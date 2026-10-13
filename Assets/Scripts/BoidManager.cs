using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour {

    const int threadGroupSize = 1024;

    public BoidSettings settings;
    public ComputeShader compute;
    Boid[] boids;
    BoidData[] boidData;
    ComputeBuffer boidBuffer;
    public IReadOnlyList<Boid> Boids => boids;
    public BoidSettings Settings => settings;
    Transform roamingTarget;
    float nextTargetChangeTime;

    IEnumerator Start () {
        yield return null;

        boids = FindObjectsByType<Boid> ();
        boidData = new BoidData[boids.Length];
        if (settings == null) {
            Debug.LogError ("[BoidManager] BoidSettings is missing!");
            enabled = false;
            yield break;
        }

        if (settings.useRoamingTarget) {
            GameObject targetObject = new GameObject ("Boid Roaming Target");
            roamingTarget = targetObject.transform;
            PickNewRoamingTarget ();
        }

        foreach (Boid b in boids) {
            b.Initialize (settings, roamingTarget);
        }

    }

    void Update () {
        if (boids != null && settings != null) {

            int numBoids = boids.Length;
            if (numBoids == 0) {
                return;
            }

            UpdateRoamingTarget ();

            for (int i = 0; i < boids.Length; i++) {
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
                boidData[i].flockHeading = Vector3.zero;
                boidData[i].flockCentre = Vector3.zero;
                boidData[i].avoidanceHeading = Vector3.zero;
                boidData[i].numFlockmates = 0;
            }

            if (CanUseComputeShader ()) {
                UpdateFlockDataWithCompute (boidData, numBoids);
            } else {
                UpdateFlockDataOnCpu (boidData, numBoids);
            }

            ApplyBoidData (boidData);
        }
    }

    void UpdateRoamingTarget () {
        if (roamingTarget == null || !settings.useRoamingTarget) {
            return;
        }

        Vector3 flockCentre = Vector3.zero;
        for (int i = 0; i < boids.Length; i++) {
            flockCentre += boids[i].position;
        }

        flockCentre /= boids.Length;
        float dstToTarget = Vector3.Distance (flockCentre, roamingTarget.position);
        if (dstToTarget <= settings.targetReachDistance || Time.time >= nextTargetChangeTime) {
            PickNewRoamingTarget ();
        }
    }

    void PickNewRoamingTarget () {
        if (settings == null || roamingTarget == null) {
            return;
        }

        roamingTarget.position = GetRandomRoamingPoint ();
        nextTargetChangeTime = Time.time + settings.targetChangeInterval;
    }

    Vector3 GetRandomRoamingPoint () {
        Vector3 halfSize = settings.aquariumSize * 0.5f - Vector3.one * settings.targetWallPadding;
        halfSize.x = Mathf.Max (halfSize.x, 1);
        halfSize.y = Mathf.Max (halfSize.y, 1);
        halfSize.z = Mathf.Max (halfSize.z, 1);

        Vector3 point = settings.aquariumCentre;
        for (int i = 0; i < 16; i++) {
            point = settings.aquariumCentre + new Vector3 (
                Random.Range (-halfSize.x, halfSize.x),
                Random.Range (-halfSize.y, halfSize.y),
                Random.Range (-halfSize.z, halfSize.z)
            );

            if (Vector3.Distance (point, settings.aquariumCentre) >= settings.targetCentreDeadZone) {
                return point;
            }
        }

        return point;
    }

    bool CanUseComputeShader () {
        if (compute == null || !SystemInfo.supportsComputeShaders) {
            return false;
        }

#if UNITY_WEBGL
        return false;
#else
        return Application.platform != RuntimePlatform.WebGLPlayer;
#endif
    }

    void UpdateFlockDataWithCompute (BoidData[] boidData, int numBoids) {
        EnsureComputeBuffer (numBoids);
        boidBuffer.SetData (boidData);

        compute.SetBuffer (0, "boids", boidBuffer);
        compute.SetInt ("numBoids", boids.Length);
        compute.SetFloat ("viewRadius", settings.perceptionRadius);
        compute.SetFloat ("avoidRadius", settings.avoidanceRadius);

        int threadGroups = Mathf.CeilToInt (numBoids / (float) threadGroupSize);
        compute.Dispatch (0, threadGroups, 1, 1);

        boidBuffer.GetData (boidData);
    }

    void EnsureComputeBuffer (int numBoids) {
        if (boidBuffer != null && boidBuffer.count == numBoids) {
            return;
        }

        ReleaseComputeBuffer ();
        boidBuffer = new ComputeBuffer (numBoids, BoidData.Size);
    }

    void UpdateFlockDataOnCpu (BoidData[] boidData, int numBoids) {
        float viewRadiusSqr = settings.perceptionRadius * settings.perceptionRadius;
        float avoidRadiusSqr = settings.avoidanceRadius * settings.avoidanceRadius;

        for (int i = 0; i < numBoids; i++) {
            Vector3 position = boidData[i].position;

            for (int j = 0; j < numBoids; j++) {
                if (i == j) {
                    continue;
                }

                Vector3 offset = boidData[j].position - position;
                float sqrDst = offset.sqrMagnitude;

                if (sqrDst < viewRadiusSqr) {
                    boidData[i].numFlockmates += 1;
                    boidData[i].flockHeading += boidData[j].direction;
                    boidData[i].flockCentre += boidData[j].position;

                    if (sqrDst > Mathf.Epsilon && sqrDst < avoidRadiusSqr) {
                        boidData[i].avoidanceHeading -= offset / sqrDst;
                    }
                }
            }
        }
    }

    void ApplyBoidData (BoidData[] boidData) {
        for (int i = 0; i < boids.Length; i++) {
            boids[i].avgFlockHeading = boidData[i].flockHeading;
            boids[i].centreOfFlockmates = boidData[i].flockCentre;
            boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
            boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

            boids[i].UpdateBoid ();
        }
    }

    void OnDrawGizmos () {
        if (settings == null) {
            return;
        }

        Gizmos.color = new Color (0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube (settings.aquariumCentre, settings.aquariumSize);
        Gizmos.color = new Color (0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube (settings.aquariumCentre, settings.aquariumSize);

        if (settings.useRoamingTarget && Application.isPlaying && roamingTarget != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere (roamingTarget.position, settings.targetReachDistance);
        }
    }

    void OnDestroy () {
        ReleaseComputeBuffer ();
    }

    void ReleaseComputeBuffer () {
        if (boidBuffer == null) {
            return;
        }

        boidBuffer.Release ();
        boidBuffer = null;
    }

    public struct BoidData {
        public Vector3 position;
        public Vector3 direction;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public static int Size {
            get {
                return sizeof (float) * 3 * 5 + sizeof (int);
            }
        }
    }
}
