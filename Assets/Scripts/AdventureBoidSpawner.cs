using UnityEngine;

[DefaultExecutionOrder(100)]
public class AdventureBoidSpawner : MonoBehaviour
{
    const int MinFishCount = 1;
    const int MaxFishCount = 500;

    public BoidSettings settings;
    public Boid prefab;
    public Vector3 swimBoundsCenter = Vector3.zero;
    public Vector3 swimBoundsSize = new Vector3(42f, 24f, 42f);
    public Vector3 schoolCenter = new Vector3(0f, 0f, 4f);
    public float schoolSpawnRadius = 8f;
    public LayerMask obstacleMask;
    public float clearanceRadius = 1.2f;
    public int spawnAttemptsPerFish = 80;
    public Color colour = new Color(0.95f, 0.35f, 0.25f);

    Transform spawnedRoot;

    void Start()
    {
        if (settings == null || prefab == null)
        {
            Debug.LogError("[AdventureBoidSpawner] Missing settings or boid prefab.");
            enabled = false;
            return;
        }

        spawnedRoot = new GameObject("Adventure Fish School").transform;
        int count = Mathf.Clamp(settings.fishCount, MinFishCount, MaxFishCount);
        for (int i = 0; i < count; i++)
        {
            SpawnBoid(i);
        }
    }

    void SpawnBoid(int index)
    {
        Vector3 position = GetSpawnPosition(index);
        Quaternion rotation = Quaternion.LookRotation(Random.onUnitSphere, Vector3.up);
        Boid boid = Instantiate(prefab, position, rotation, spawnedRoot);
        boid.SetColour(colour);
    }

    Vector3 GetSpawnPosition(int index)
    {
        Bounds swimBounds = new Bounds(swimBoundsCenter, swimBoundsSize);
        for (int i = 0; i < spawnAttemptsPerFish; i++)
        {
            Vector3 candidate = schoolCenter + Random.insideUnitSphere * schoolSpawnRadius;
            if (IsValidSpawnPoint(candidate, swimBounds))
            {
                return candidate;
            }
        }

        for (int i = 0; i < spawnAttemptsPerFish; i++)
        {
            Vector3 candidate = RandomPointInBounds(swimBounds);
            if (IsValidSpawnPoint(candidate, swimBounds))
            {
                return candidate;
            }
        }

        Debug.LogWarning($"[AdventureBoidSpawner] Falling back to school centre for fish {index}. Check cave density/spawn bounds.");
        return swimBounds.ClosestPoint(schoolCenter);
    }

    bool IsValidSpawnPoint(Vector3 point, Bounds swimBounds)
    {
        if (!swimBounds.Contains(point))
        {
            return false;
        }

        return !Physics.CheckSphere(point, clearanceRadius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    static Vector3 RandomPointInBounds(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        return new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            Random.Range(min.z, max.z)
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.2f);
        Gizmos.DrawCube(swimBoundsCenter, swimBoundsSize);
        Gizmos.color = new Color(colour.r, colour.g, colour.b, 0.3f);
        Gizmos.DrawSphere(schoolCenter, schoolSpawnRadius);
    }
}
