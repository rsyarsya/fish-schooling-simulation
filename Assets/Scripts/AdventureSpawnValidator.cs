using UnityEngine;

public class AdventureSpawnValidator : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnCandidate
    {
        public Vector3 position;
        public float yaw;
    }

    public SubmarinePlayerController player;
    public SpawnCandidate[] candidates;
    public Vector3 fallbackPosition = new Vector3(0f, 24f, -38f);
    public float fallbackYaw;
    public LayerMask obstacleMask;
    public float overlapRadius = 2.2f;
    public float forwardClearance = 18f;
    public float verticalClearance = 5f;

    void Start()
    {
        ApplySafeSpawn();
    }

    public void ApplySafeSpawn()
    {
        if (player == null)
        {
            return;
        }

        if (candidates != null)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                SpawnCandidate candidate = candidates[i];
                if (IsSafe(candidate.position, candidate.yaw))
                {
                    MovePlayer(candidate.position, candidate.yaw);
                    return;
                }
            }
        }

        Debug.LogWarning("[AdventureSpawnValidator] No safe spawn candidate found. Using fallback spawn.");
        MovePlayer(fallbackPosition, fallbackYaw);
    }

    bool IsSafe(Vector3 position, float yaw)
    {
        if (Physics.CheckSphere(position, overlapRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        if (Physics.SphereCast(position, overlapRadius * 0.65f, forward, out _, forwardClearance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (Physics.SphereCast(position, overlapRadius * 0.55f, Vector3.up, out _, verticalClearance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (Physics.SphereCast(position, overlapRadius * 0.55f, Vector3.down, out _, verticalClearance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    void MovePlayer(Vector3 position, float yaw)
    {
        player.TeleportTo(position, yaw);
    }
}
