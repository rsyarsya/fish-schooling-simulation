using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(-200)]
public class MeshGenerator : MonoBehaviour {

    const int threadGroupSize = 8;

    [Header ("General Settings")]
    public DensityGenerator densityGenerator;

    public bool fixedMapSize;
    [ConditionalHide (nameof (fixedMapSize), true)]
    public Vector3Int numChunks = Vector3Int.one;
    [ConditionalHide (nameof (fixedMapSize), false)]
    public Transform viewer;
    [ConditionalHide (nameof (fixedMapSize), false)]
    public float viewDistance = 30;

    [Space ()]
    public bool autoUpdateInEditor = true;
    public bool autoUpdateInGame = true;
    public ComputeShader shader;
    public Material mat;
    public bool generateColliders;
    public int chunkLayer = 9;

    [Header ("Voxel Settings")]
    public float isoLevel;
    public float boundsSize = 1;
    public Vector3 offset = Vector3.zero;

    [Range (2, 100)]
    public int numPointsPerAxis = 30;

    [Header ("Gizmos")]
    public bool showBoundsGizmo = true;
    public Color boundsGizmoCol = Color.white;

    [Header ("Mesh Integrity")]
    public float maxTriangleEdgeMultiplier = 2.5f;

    GameObject chunkHolder;
    const string chunkHolderName = "Chunks Holder";
    List<Chunk> chunks;
    static readonly List<Chunk> emptyChunks = new List<Chunk> ();
    Dictionary<Vector3Int, Chunk> existingChunks;
    Queue<Chunk> recycleableChunks;
    readonly Dictionary<Chunk, MeshDiagnostics> diagnosticsByChunk = new Dictionary<Chunk, MeshDiagnostics> ();

    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;

    bool settingsUpdated;

    public IReadOnlyList<Chunk> ActiveChunks => chunks ?? emptyChunks;
    public float PointSpacing => numPointsPerAxis > 1 ? boundsSize / (numPointsPerAxis - 1) : boundsSize;

    public bool TryGetDiagnostics (Chunk chunk, out MeshDiagnostics diagnostics) {
        if (chunk == null) {
            diagnostics = default;
            return false;
        }

        return diagnosticsByChunk.TryGetValue (chunk, out diagnostics);
    }

    public Bounds GetChunkWorldBounds (Chunk chunk) {
        return chunk != null
            ? new Bounds (CentreFromCoord (chunk.coord), Vector3.one * boundsSize)
            : new Bounds (Vector3.zero, Vector3.zero);
    }

    public bool TryGetChunk (Vector3Int coord, out Chunk chunk) {
        if (chunks != null) {
            foreach (Chunk activeChunk in chunks) {
                if (activeChunk != null && activeChunk.coord == coord) {
                    chunk = activeChunk;
                    return true;
                }
            }
        }

        chunk = null;
        return false;
    }

    void Awake () {
        if (Application.isPlaying && !fixedMapSize) {
            InitVariableChunkStructures ();

            var oldChunks = FindObjectsOfType<Chunk> ();
            for (int i = oldChunks.Length - 1; i >= 0; i--) {
                Destroy (oldChunks[i].gameObject);
            }
        }
    }

    void Start () {
        if (Application.isPlaying && fixedMapSize && autoUpdateInGame) {
            Run ();
        }
    }

    void Update () {
        // Update endless terrain
        if ((Application.isPlaying && !fixedMapSize)) {
            Run ();
        }

        if (settingsUpdated) {
            RequestMeshUpdate ();
            settingsUpdated = false;
        }
    }

    public void Run () {
        CreateBuffers ();

        if (fixedMapSize) {
            InitChunks ();
            UpdateAllChunks ();

        } else {
            if (Application.isPlaying) {
                InitVisibleChunks ();
            }
        }

        // Release buffers immediately in editor
        if (!Application.isPlaying) {
            ReleaseBuffers ();
        }

    }

    public void RequestMeshUpdate () {
        if ((Application.isPlaying && autoUpdateInGame) || (!Application.isPlaying && autoUpdateInEditor)) {
            Run ();
        }
    }

    void InitVariableChunkStructures () {
        recycleableChunks = new Queue<Chunk> ();
        chunks = new List<Chunk> ();
        existingChunks = new Dictionary<Vector3Int, Chunk> ();
    }

    void InitVisibleChunks () {
        if (chunks==null) {
            return;
        }
        CreateChunkHolder ();

        Vector3 p = viewer.position;
        Vector3 ps = p / boundsSize;
        Vector3Int viewerCoord = new Vector3Int (Mathf.RoundToInt (ps.x), Mathf.RoundToInt (ps.y), Mathf.RoundToInt (ps.z));

        int maxChunksInView = Mathf.CeilToInt (viewDistance / boundsSize);
        float sqrViewDistance = viewDistance * viewDistance;

        // Go through all existing chunks and flag for recyling if outside of max view dst
        for (int i = chunks.Count - 1; i >= 0; i--) {
            Chunk chunk = chunks[i];
            Vector3 centre = CentreFromCoord (chunk.coord);
            Vector3 viewerOffset = p - centre;
            Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
            float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;
            if (sqrDst > sqrViewDistance) {
                existingChunks.Remove (chunk.coord);
                recycleableChunks.Enqueue (chunk);
                chunks.RemoveAt (i);
            }
        }

        for (int x = -maxChunksInView; x <= maxChunksInView; x++) {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++) {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z) + viewerCoord;

                    if (existingChunks.ContainsKey (coord)) {
                        continue;
                    }

                    Vector3 centre = CentreFromCoord (coord);
                    Vector3 viewerOffset = p - centre;
                    Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
                    float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance) {

                        Bounds bounds = new Bounds (CentreFromCoord (coord), Vector3.one * boundsSize);
                        if (IsVisibleFrom (bounds, Camera.main)) {
                            if (recycleableChunks.Count > 0) {
                                Chunk chunk = recycleableChunks.Dequeue ();
                                chunk.coord = coord;
                                chunk.gameObject.layer = chunkLayer;
                                existingChunks.Add (coord, chunk);
                                chunks.Add (chunk);
                                UpdateChunkMesh (chunk);
                            } else {
                                Chunk chunk = CreateChunk (coord);
                                chunk.coord = coord;
                                chunk.SetUp (mat, generateColliders);
                                existingChunks.Add (coord, chunk);
                                chunks.Add (chunk);
                                UpdateChunkMesh (chunk);
                            }
                        }
                    }

                }
            }
        }
    }

    public bool IsVisibleFrom (Bounds bounds, Camera camera) {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes (camera);
        return GeometryUtility.TestPlanesAABB (planes, bounds);
    }

    public void UpdateChunkMesh (Chunk chunk) {
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt (numVoxelsPerAxis / (float) threadGroupSize);
        float pointSpacing = boundsSize / (numPointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord (coord);

        Vector3 worldBounds = new Vector3 (numChunks.x, numChunks.y, numChunks.z) * boundsSize;

        densityGenerator.Generate (pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, pointSpacing);

        triangleBuffer.SetCounterValue (0);
        shader.SetBuffer (0, "points", pointsBuffer);
        shader.SetBuffer (0, "triangles", triangleBuffer);
        shader.SetInt ("numPointsPerAxis", numPointsPerAxis);
        shader.SetFloat ("isoLevel", isoLevel);

        shader.Dispatch (0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount (triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData (triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData (tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear ();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];
        int validVertexCount = 0;
        float maxEdgeLength = Mathf.Max (pointSpacing * maxTriangleEdgeMultiplier, pointSpacing);
        float maxEdgeLengthSqr = maxEdgeLength * maxEdgeLength;
        float minAreaSqr = pointSpacing * pointSpacing * pointSpacing * pointSpacing * 0.000001f;
        MeshDiagnostics diagnostics = new MeshDiagnostics {
            coord = coord,
            rawTriangleCount = numTris
        };

        for (int i = 0; i < numTris; i++) {
            Triangle tri = tris[i];
            Vector3 triCentre = TriangleCentre (tri, centre);

            if (!TriangleIsFinite (tri)) {
                diagnostics.invalidTriangleCount++;
                diagnostics.firstInvalidPoint = diagnostics.invalidTriangleCount == 1 ? triCentre : diagnostics.firstInvalidPoint;
                continue;
            }

            Vector3 normalCross = TriangleNormalCross (tri);
            if (!VectorIsFinite (normalCross)) {
                diagnostics.nonFiniteNormalCount++;
                diagnostics.firstNormalIssuePoint = diagnostics.nonFiniteNormalCount == 1 ? triCentre : diagnostics.firstNormalIssuePoint;
                continue;
            }

            if (normalCross.sqrMagnitude <= minAreaSqr) {
                diagnostics.degenerateTriangleCount++;
                diagnostics.firstDegeneratePoint = diagnostics.degenerateTriangleCount == 1 ? triCentre : diagnostics.firstDegeneratePoint;
                continue;
            }

            if (TriangleHasSpikeEdge (tri, maxEdgeLengthSqr)) {
                diagnostics.spikeTriangleCount++;
                diagnostics.firstSpikePoint = diagnostics.spikeTriangleCount == 1 ? triCentre : diagnostics.firstSpikePoint;
                continue;
            }

            meshTriangles[validVertexCount] = validVertexCount;
            vertices[validVertexCount++] = tri.a;
            meshTriangles[validVertexCount] = validVertexCount;
            vertices[validVertexCount++] = tri.b;
            meshTriangles[validVertexCount] = validVertexCount;
            vertices[validVertexCount++] = tri.c;
        }

        if (validVertexCount != vertices.Length) {
            System.Array.Resize (ref vertices, validVertexCount);
            System.Array.Resize (ref meshTriangles, validVertexCount);
        }

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals ();
        mesh.RecalculateBounds ();
        chunk.RefreshCollider ();

        diagnostics.validTriangleCount = validVertexCount / 3;
        diagnostics.colliderExpected = generateColliders;
        diagnostics.colliderEnabled = chunk.SharedMeshCollider != null && chunk.SharedMeshCollider.enabled;
        diagnostics.colliderMatchesMesh = !generateColliders || (chunk.SharedMeshCollider != null && chunk.SharedMeshCollider.sharedMesh == mesh);
        diagnosticsByChunk[chunk] = diagnostics;
    }

    public void UpdateAllChunks () {

        // Create mesh for each chunk
        foreach (Chunk chunk in chunks) {
            UpdateChunkMesh (chunk);
        }

    }

    void OnDestroy () {
        if (Application.isPlaying) {
            ReleaseBuffers ();
        }
    }

    void CreateBuffers () {
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        if (!Application.isPlaying || (pointsBuffer == null || numPoints != pointsBuffer.count)) {
            if (Application.isPlaying) {
                ReleaseBuffers ();
            }
            triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
            pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
            triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);

        }
    }

    void ReleaseBuffers () {
        if (triangleBuffer != null) {
            triangleBuffer.Release ();
            pointsBuffer.Release ();
            triCountBuffer.Release ();
        }
    }

    Vector3 CentreFromCoord (Vector3Int coord) {
        // Centre entire map at origin
        if (fixedMapSize) {
            Vector3 totalBounds = (Vector3) numChunks * boundsSize;
            return -totalBounds / 2 + (Vector3) coord * boundsSize + Vector3.one * boundsSize / 2;
        }

        return new Vector3 (coord.x, coord.y, coord.z) * boundsSize;
    }

    void CreateChunkHolder () {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null) {
            if (GameObject.Find (chunkHolderName)) {
                chunkHolder = GameObject.Find (chunkHolderName);
            } else {
                chunkHolder = new GameObject (chunkHolderName);
            }
        }
    }

    // Create/get references to all chunks
    void InitChunks () {
        CreateChunkHolder ();
        chunks = new List<Chunk> ();
        List<Chunk> oldChunks = new List<Chunk> (FindObjectsOfType<Chunk> ());

        // Go through all coords and create a chunk there if one doesn't already exist
        for (int x = 0; x < numChunks.x; x++) {
            for (int y = 0; y < numChunks.y; y++) {
                for (int z = 0; z < numChunks.z; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z);
                    bool chunkAlreadyExists = false;

                    // If chunk already exists, add it to the chunks list, and remove from the old list.
                    for (int i = 0; i < oldChunks.Count; i++) {
                        if (oldChunks[i].coord == coord) {
                            chunks.Add (oldChunks[i]);
                            oldChunks.RemoveAt (i);
                            chunkAlreadyExists = true;
                            break;
                        }
                    }

                    // Create new chunk
                    if (!chunkAlreadyExists) {
                        var newChunk = CreateChunk (coord);
                        chunks.Add (newChunk);
                    }

                    chunks[chunks.Count - 1].gameObject.layer = chunkLayer;
                    chunks[chunks.Count - 1].SetUp (mat, generateColliders);
                }
            }
        }

        // Delete all unused chunks
        for (int i = 0; i < oldChunks.Count; i++) {
            oldChunks[i].DestroyOrDisable ();
        }
    }

    Chunk CreateChunk (Vector3Int coord) {
        GameObject chunk = new GameObject ($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.layer = chunkLayer;
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk> ();
        newChunk.coord = coord;
        return newChunk;
    }

    void OnValidate() {
        settingsUpdated = true;
    }

    public struct MeshDiagnostics {
        public Vector3Int coord;
        public int rawTriangleCount;
        public int validTriangleCount;
        public int invalidTriangleCount;
        public int degenerateTriangleCount;
        public int spikeTriangleCount;
        public int nonFiniteNormalCount;
        public bool colliderExpected;
        public bool colliderEnabled;
        public bool colliderMatchesMesh;
        public Vector3 firstInvalidPoint;
        public Vector3 firstDegeneratePoint;
        public Vector3 firstSpikePoint;
        public Vector3 firstNormalIssuePoint;

        public int RejectedTriangleCount => invalidTriangleCount + degenerateTriangleCount + spikeTriangleCount + nonFiniteNormalCount;
        public bool HasProblems => RejectedTriangleCount > 0 || (colliderExpected && (!colliderEnabled || !colliderMatchesMesh));
    }

    static bool TriangleIsFinite (Triangle tri) {
        return VectorIsFinite (tri.a) && VectorIsFinite (tri.b) && VectorIsFinite (tri.c);
    }

    static bool VectorIsFinite (Vector3 value) {
        return FloatIsFinite (value.x) && FloatIsFinite (value.y) && FloatIsFinite (value.z);
    }

    static bool FloatIsFinite (float value) {
        return !float.IsNaN (value) && !float.IsInfinity (value);
    }

    static Vector3 TriangleNormalCross (Triangle tri) {
        return Vector3.Cross (tri.b - tri.a, tri.c - tri.a);
    }

    static bool TriangleHasSpikeEdge (Triangle tri, float maxEdgeLengthSqr) {
        return (tri.a - tri.b).sqrMagnitude > maxEdgeLengthSqr
            || (tri.b - tri.c).sqrMagnitude > maxEdgeLengthSqr
            || (tri.c - tri.a).sqrMagnitude > maxEdgeLengthSqr;
    }

    static Vector3 TriangleCentre (Triangle tri, Vector3 fallback) {
        if (!TriangleIsFinite (tri)) {
            return fallback;
        }

        return (tri.a + tri.b + tri.c) / 3f;
    }

    struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    void OnDrawGizmos () {
        if (showBoundsGizmo) {
            Gizmos.color = boundsGizmoCol;

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk> (FindObjectsOfType<Chunk> ()) : this.chunks;
            foreach (var chunk in chunks) {
                Bounds bounds = new Bounds (CentreFromCoord (chunk.coord), Vector3.one * boundsSize);
                Gizmos.color = boundsGizmoCol;
                Gizmos.DrawWireCube (CentreFromCoord (chunk.coord), Vector3.one * boundsSize);
            }
        }
    }

}
