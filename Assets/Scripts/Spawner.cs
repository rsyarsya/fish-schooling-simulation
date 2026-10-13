using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    public enum GizmoType { Never, SelectedOnly, Always }
    const int MinFishCount = 10;
    const int MaxFishCount = 500;

    public BoidSettings settings;
    public Boid prefab;
    public float spawnRadius = 10;
    public int spawnCount = 10;
    public Color colour;
    public GizmoType showSpawnRegion;

    void Start () {
        int count = settings != null ? GetRuntimeFishCount () : spawnCount;

        for (int i = 0; i < count; i++) {
            Vector3 pos = GetSpawnPosition ();
            Boid boid = Instantiate (prefab);
            boid.transform.position = pos;
            boid.transform.forward = Random.insideUnitSphere;

            boid.SetColour (colour);
        }
    }

    int GetRuntimeFishCount () {
        int count = Mathf.Clamp (settings.fishCount, MinFishCount, MaxFishCount);
        if (RuntimeViewMode.IsMobileLike) {
            int mobileCap = Mathf.Clamp (settings.mobileFishCountCap, MinFishCount, MaxFishCount);
            count = Mathf.Min (count, mobileCap);
        }

        return count;
    }

    Vector3 GetSpawnPosition () {
        return transform.position + Random.insideUnitSphere * spawnRadius;
    }

    private void OnDrawGizmos () {
        if (showSpawnRegion == GizmoType.Always) {
            DrawGizmos ();
        }
    }

    void OnDrawGizmosSelected () {
        if (showSpawnRegion == GizmoType.SelectedOnly) {
            DrawGizmos ();
        }
    }

    void DrawGizmos () {

        Gizmos.color = new Color (colour.r, colour.g, colour.b, 0.3f);
        Gizmos.DrawSphere (transform.position, spawnRadius);
    }

}
