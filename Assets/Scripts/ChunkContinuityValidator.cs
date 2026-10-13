using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkContinuityValidator : MonoBehaviour
{
    public MeshGenerator meshGenerator;
    public bool logSummaryOnStart = true;
    public bool drawSeamGizmos = true;
    public float seamTolerance = 0.12f;
    public float boundaryEpsilonMultiplier = 0.18f;
    public Color seamWarningColor = new Color(1f, 0.85f, 0.05f, 0.85f);
    public Color seamErrorColor = new Color(1f, 0.05f, 0.02f, 0.95f);

    readonly List<SeamDiagnostic> seamDiagnostics = new List<SeamDiagnostic>();
    ContinuitySummary lastSummary;

    IEnumerator Start()
    {
        EnsureMeshGenerator();

        if (!logSummaryOnStart)
        {
            yield break;
        }

        yield return null;
        yield return null;
        yield return null;
        RefreshDiagnostics();
        LogSummary();
    }

    public void RefreshDiagnostics()
    {
        EnsureMeshGenerator();
        seamDiagnostics.Clear();
        lastSummary = new ContinuitySummary();

        if (meshGenerator == null)
        {
            return;
        }

        Dictionary<Vector3Int, Chunk> chunkByCoord = BuildChunkMap();
        HashSet<Material> materials = new HashSet<Material>();
        foreach (Chunk chunk in meshGenerator.ActiveChunks)
        {
            if (chunk != null && chunk.SharedMaterial != null)
            {
                materials.Add(chunk.SharedMaterial);
            }
        }

        lastSummary.materialInstanceCount = materials.Count;
        ValidateAxis(chunkByCoord, Vector3Int.right, 0);
        ValidateAxis(chunkByCoord, Vector3Int.up, 1);
        ValidateAxis(chunkByCoord, new Vector3Int(0, 0, 1), 2);
    }

    public void LogSummary()
    {
        if (meshGenerator == null)
        {
            Debug.LogWarning("[ChunkContinuityValidator] No MeshGenerator found.");
            return;
        }

        Debug.Log(
            "[ChunkContinuityValidator] " +
            $"pairs={lastSummary.neighborPairsChecked}, " +
            $"seamWarnings={lastSummary.seamWarningCount}, " +
            $"missingNeighbors={lastSummary.missingNeighborCount}, " +
            $"emptyBoundaryPairs={lastSummary.emptyBoundaryPairCount}, " +
            $"maxGap={lastSummary.maxGap:F4}, " +
            $"materialInstances={lastSummary.materialInstanceCount}"
        );
    }

    Dictionary<Vector3Int, Chunk> BuildChunkMap()
    {
        Dictionary<Vector3Int, Chunk> chunkByCoord = new Dictionary<Vector3Int, Chunk>();
        foreach (Chunk chunk in meshGenerator.ActiveChunks)
        {
            if (chunk != null && !chunkByCoord.ContainsKey(chunk.coord))
            {
                chunkByCoord.Add(chunk.coord, chunk);
            }
        }

        return chunkByCoord;
    }

    void ValidateAxis(Dictionary<Vector3Int, Chunk> chunkByCoord, Vector3Int direction, int axis)
    {
        foreach (KeyValuePair<Vector3Int, Chunk> entry in chunkByCoord)
        {
            Vector3Int neighborCoord = entry.Key + direction;
            if (!CoordInsideFixedMap(neighborCoord))
            {
                continue;
            }

            if (!chunkByCoord.TryGetValue(neighborCoord, out Chunk neighbor))
            {
                lastSummary.missingNeighborCount++;
                SeamDiagnostic missingDiagnostic = CreateBaseDiagnostic(entry.Value, null, axis);
                missingDiagnostic.missingNeighbor = true;
                missingDiagnostic.hasWarning = true;
                seamDiagnostics.Add(missingDiagnostic);
                continue;
            }

            lastSummary.neighborPairsChecked++;
            SeamDiagnostic diagnostic = CompareChunkPair(entry.Value, neighbor, axis);
            if (diagnostic.hasWarning)
            {
                lastSummary.seamWarningCount++;
                if (diagnostic.emptyBoundary)
                {
                    lastSummary.emptyBoundaryPairCount++;
                }

                seamDiagnostics.Add(diagnostic);
            }

            lastSummary.maxGap = Mathf.Max(lastSummary.maxGap, diagnostic.maxGap);
        }
    }

    bool CoordInsideFixedMap(Vector3Int coord)
    {
        if (!meshGenerator.fixedMapSize)
        {
            return true;
        }

        return coord.x >= 0 && coord.y >= 0 && coord.z >= 0
            && coord.x < meshGenerator.numChunks.x
            && coord.y < meshGenerator.numChunks.y
            && coord.z < meshGenerator.numChunks.z;
    }

    SeamDiagnostic CompareChunkPair(Chunk a, Chunk b, int axis)
    {
        SeamDiagnostic diagnostic = CreateBaseDiagnostic(a, b, axis);
        if (a.SharedMesh == null || b.SharedMesh == null)
        {
            diagnostic.hasWarning = true;
            return diagnostic;
        }

        float plane = SharedPlane(a, axis);
        float boundaryEpsilon = Mathf.Max(meshGenerator.PointSpacing * boundaryEpsilonMultiplier, 0.001f);
        List<Vector3> aVertices = CollectBoundaryVertices(a, axis, plane, boundaryEpsilon);
        List<Vector3> bVertices = CollectBoundaryVertices(b, axis, plane, boundaryEpsilon);
        diagnostic.boundaryVerticesA = aVertices.Count;
        diagnostic.boundaryVerticesB = bVertices.Count;

        if (aVertices.Count == 0 && bVertices.Count == 0)
        {
            return diagnostic;
        }

        if (aVertices.Count == 0 || bVertices.Count == 0)
        {
            diagnostic.hasWarning = true;
            diagnostic.emptyBoundary = true;
            diagnostic.maxGap = seamTolerance * 2f;
            return diagnostic;
        }

        HashSet<Vector3Int> bHashes = BuildQuantizedSet(bVertices);
        int unmatched = CountUnmatched(aVertices, bHashes);
        HashSet<Vector3Int> aHashes = BuildQuantizedSet(aVertices);
        unmatched += CountUnmatched(bVertices, aHashes);

        diagnostic.unmatchedVertices = unmatched;
        diagnostic.maxGap = EstimateMaxGap(aVertices, bVertices);
        diagnostic.hasWarning = unmatched > 0 || diagnostic.maxGap > seamTolerance;
        return diagnostic;
    }

    SeamDiagnostic CreateBaseDiagnostic(Chunk a, Chunk b, int axis)
    {
        Bounds bounds = meshGenerator.GetChunkWorldBounds(a);
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        if (b != null)
        {
            Bounds neighborBounds = meshGenerator.GetChunkWorldBounds(b);
            center = (bounds.center + neighborBounds.center) * 0.5f;
            size = bounds.size;
        }

        if (axis == 0)
        {
            center.x = SharedPlane(a, axis);
            size.x = 0.12f;
        }
        else if (axis == 1)
        {
            center.y = SharedPlane(a, axis);
            size.y = 0.12f;
        }
        else
        {
            center.z = SharedPlane(a, axis);
            size.z = 0.12f;
        }

        return new SeamDiagnostic
        {
            axis = axis,
            center = center,
            size = size,
            maxGap = 0f
        };
    }

    float SharedPlane(Chunk chunk, int axis)
    {
        Bounds bounds = meshGenerator.GetChunkWorldBounds(chunk);
        if (axis == 0)
        {
            return bounds.center.x + bounds.extents.x;
        }

        if (axis == 1)
        {
            return bounds.center.y + bounds.extents.y;
        }

        return bounds.center.z + bounds.extents.z;
    }

    List<Vector3> CollectBoundaryVertices(Chunk chunk, int axis, float plane, float epsilon)
    {
        List<Vector3> vertices = new List<Vector3>();
        Vector3[] meshVertices = chunk.SharedMesh.vertices;
        for (int i = 0; i < meshVertices.Length; i++)
        {
            Vector3 world = chunk.transform.TransformPoint(meshVertices[i]);
            if (Mathf.Abs(AxisValue(world, axis) - plane) <= epsilon)
            {
                vertices.Add(world);
            }
        }

        return vertices;
    }

    HashSet<Vector3Int> BuildQuantizedSet(List<Vector3> vertices)
    {
        HashSet<Vector3Int> set = new HashSet<Vector3Int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            set.Add(Quantize(vertices[i]));
        }

        return set;
    }

    int CountUnmatched(List<Vector3> vertices, HashSet<Vector3Int> reference)
    {
        int unmatched = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!HasNearbyHash(reference, Quantize(vertices[i])))
            {
                unmatched++;
            }
        }

        return unmatched;
    }

    bool HasNearbyHash(HashSet<Vector3Int> reference, Vector3Int hash)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (reference.Contains(hash + new Vector3Int(x, y, z)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    Vector3Int Quantize(Vector3 value)
    {
        float scale = Mathf.Max(seamTolerance, 0.0001f);
        return new Vector3Int(
            Mathf.RoundToInt(value.x / scale),
            Mathf.RoundToInt(value.y / scale),
            Mathf.RoundToInt(value.z / scale)
        );
    }

    float EstimateMaxGap(List<Vector3> aVertices, List<Vector3> bVertices)
    {
        float maxGapSqr = 0f;
        for (int i = 0; i < aVertices.Count; i++)
        {
            float nearest = NearestDistanceSqr(aVertices[i], bVertices);
            maxGapSqr = Mathf.Max(maxGapSqr, nearest);
        }

        for (int i = 0; i < bVertices.Count; i++)
        {
            float nearest = NearestDistanceSqr(bVertices[i], aVertices);
            maxGapSqr = Mathf.Max(maxGapSqr, nearest);
        }

        return Mathf.Sqrt(maxGapSqr);
    }

    float NearestDistanceSqr(Vector3 point, List<Vector3> candidates)
    {
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            nearest = Mathf.Min(nearest, (point - candidates[i]).sqrMagnitude);
        }

        return nearest;
    }

    float AxisValue(Vector3 value, int axis)
    {
        if (axis == 0)
        {
            return value.x;
        }

        if (axis == 1)
        {
            return value.y;
        }

        return value.z;
    }

    void OnDrawGizmos()
    {
        if (!drawSeamGizmos || seamDiagnostics.Count == 0)
        {
            return;
        }

        for (int i = 0; i < seamDiagnostics.Count; i++)
        {
            SeamDiagnostic diagnostic = seamDiagnostics[i];
            Gizmos.color = diagnostic.missingNeighbor ? seamErrorColor : seamWarningColor;
            Gizmos.DrawWireCube(diagnostic.center, diagnostic.size);
        }
    }

    void EnsureMeshGenerator()
    {
        if (meshGenerator == null)
        {
            meshGenerator = FindFirstObjectByType<MeshGenerator>();
        }
    }

    struct ContinuitySummary
    {
        public int neighborPairsChecked;
        public int seamWarningCount;
        public int missingNeighborCount;
        public int emptyBoundaryPairCount;
        public float maxGap;
        public int materialInstanceCount;
    }

    struct SeamDiagnostic
    {
        public int axis;
        public Vector3 center;
        public Vector3 size;
        public int boundaryVerticesA;
        public int boundaryVerticesB;
        public int unmatchedVertices;
        public float maxGap;
        public bool hasWarning;
        public bool missingNeighbor;
        public bool emptyBoundary;
    }
}
