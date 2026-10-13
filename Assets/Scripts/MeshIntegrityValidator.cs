using System.Collections;
using UnityEngine;

public class MeshIntegrityValidator : MonoBehaviour
{
    public MeshGenerator meshGenerator;
    public bool logSummaryOnStart = true;
    public bool drawChunkBounds = true;
    public bool drawProblemMarkers = true;
    public float markerSize = 1.2f;
    public Color healthyBoundsColor = new Color(0.1f, 0.8f, 1f, 0.12f);
    public Color problemBoundsColor = new Color(1f, 0.25f, 0.1f, 0.42f);
    public Color invalidMarkerColor = new Color(1f, 0f, 0f, 0.9f);
    public Color degenerateMarkerColor = new Color(1f, 0.72f, 0f, 0.9f);
    public Color colliderMarkerColor = new Color(1f, 1f, 0f, 0.9f);

    IEnumerator Start()
    {
        EnsureMeshGenerator();

        if (!logSummaryOnStart)
        {
            yield break;
        }

        yield return null;
        yield return null;
        LogSummary();
    }

    public void LogSummary()
    {
        EnsureMeshGenerator();
        if (meshGenerator == null)
        {
            Debug.LogWarning("[MeshIntegrityValidator] No MeshGenerator found.");
            return;
        }

        MeshSummary summary = BuildSummary();
        Debug.Log(
            "[MeshIntegrityValidator] " +
            $"chunks={summary.chunkCount}, " +
            $"triangles={summary.validTriangles}/{summary.rawTriangles}, " +
            $"invalid={summary.invalidTriangles}, " +
            $"degenerate={summary.degenerateTriangles}, " +
            $"spike={summary.spikeTriangles}, " +
            $"normalIssues={summary.normalIssues}, " +
            $"colliderDisabled={summary.colliderDisabledCount}, " +
            $"colliderMismatch={summary.colliderMismatchCount}"
        );
    }

    MeshSummary BuildSummary()
    {
        MeshSummary summary = new MeshSummary();
        foreach (Chunk chunk in meshGenerator.ActiveChunks)
        {
            if (chunk == null)
            {
                continue;
            }

            summary.chunkCount++;
            if (!meshGenerator.TryGetDiagnostics(chunk, out MeshGenerator.MeshDiagnostics diagnostics))
            {
                continue;
            }

            summary.rawTriangles += diagnostics.rawTriangleCount;
            summary.validTriangles += diagnostics.validTriangleCount;
            summary.invalidTriangles += diagnostics.invalidTriangleCount;
            summary.degenerateTriangles += diagnostics.degenerateTriangleCount;
            summary.spikeTriangles += diagnostics.spikeTriangleCount;
            summary.normalIssues += diagnostics.nonFiniteNormalCount;
            if (diagnostics.colliderExpected && !diagnostics.colliderEnabled)
            {
                summary.colliderDisabledCount++;
            }

            if (diagnostics.colliderExpected && diagnostics.colliderEnabled && !diagnostics.colliderMatchesMesh)
            {
                summary.colliderMismatchCount++;
            }
        }

        return summary;
    }

    void OnDrawGizmos()
    {
        EnsureMeshGenerator();
        if (meshGenerator == null)
        {
            return;
        }

        foreach (Chunk chunk in meshGenerator.ActiveChunks)
        {
            if (chunk == null || chunk.SharedMesh == null)
            {
                continue;
            }

            meshGenerator.TryGetDiagnostics(chunk, out MeshGenerator.MeshDiagnostics diagnostics);
            bool hasDiagnostics = diagnostics.rawTriangleCount > 0 || diagnostics.validTriangleCount > 0;
            bool hasProblem = hasDiagnostics && diagnostics.HasProblems;

            if (drawChunkBounds)
            {
                DrawChunkBounds(chunk, hasProblem);
            }

            if (drawProblemMarkers && hasDiagnostics)
            {
                DrawProblemMarkers(chunk, diagnostics);
            }
        }
    }

    void DrawChunkBounds(Chunk chunk, bool hasProblem)
    {
        Bounds bounds = chunk.SharedMesh.bounds;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = chunk.transform.localToWorldMatrix;
        Gizmos.color = hasProblem ? problemBoundsColor : healthyBoundsColor;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        Gizmos.matrix = previousMatrix;
    }

    void DrawProblemMarkers(Chunk chunk, MeshGenerator.MeshDiagnostics diagnostics)
    {
        if (diagnostics.invalidTriangleCount > 0)
        {
            Gizmos.color = invalidMarkerColor;
            Gizmos.DrawSphere(diagnostics.firstInvalidPoint, markerSize);
        }

        if (diagnostics.degenerateTriangleCount > 0)
        {
            Gizmos.color = degenerateMarkerColor;
            Gizmos.DrawSphere(diagnostics.firstDegeneratePoint, markerSize * 0.85f);
        }

        if (diagnostics.spikeTriangleCount > 0)
        {
            Gizmos.color = invalidMarkerColor;
            Gizmos.DrawWireSphere(diagnostics.firstSpikePoint, markerSize * 1.25f);
        }

        if (diagnostics.nonFiniteNormalCount > 0)
        {
            Gizmos.color = degenerateMarkerColor;
            Gizmos.DrawWireCube(diagnostics.firstNormalIssuePoint, Vector3.one * markerSize);
        }

        if (diagnostics.colliderExpected && (!diagnostics.colliderEnabled || !diagnostics.colliderMatchesMesh))
        {
            Gizmos.color = colliderMarkerColor;
            Bounds bounds = chunk.SharedMesh.bounds;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = chunk.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size * 1.05f);
            Gizmos.matrix = previousMatrix;
        }
    }

    void EnsureMeshGenerator()
    {
        if (meshGenerator == null)
        {
            meshGenerator = FindFirstObjectByType<MeshGenerator>();
        }
    }

    struct MeshSummary
    {
        public int chunkCount;
        public int rawTriangles;
        public int validTriangles;
        public int invalidTriangles;
        public int degenerateTriangles;
        public int spikeTriangles;
        public int normalIssues;
        public int colliderDisabledCount;
        public int colliderMismatchCount;
    }
}
