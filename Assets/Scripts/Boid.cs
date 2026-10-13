using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour {

    const float CollisionSkinWidth = 0.05f;
    const int MaxPenetrationChecks = 8;
    const float MinStochasticInterval = 0.05f;

    BoidSettings settings;

    // State
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward;
    Vector3 velocity;
    Vector3 stochasticForce;
    float stochasticTimer;
    public Vector3 Velocity => velocity;
    public Vector3 StochasticForce => stochasticForce;

    // To update:
    Vector3 acceleration;
    [HideInInspector]
    public Vector3 avgFlockHeading;
    [HideInInspector]
    public Vector3 avgAvoidanceHeading;
    [HideInInspector]
    public Vector3 centreOfFlockmates;
    [HideInInspector]
    public int numPerceivedFlockmates;

    // Cached
    Material material;
    Transform cachedTransform;
    Transform target;
    Collider[] penetrationHits = new Collider[MaxPenetrationChecks];

    void Awake () {
        material = transform.GetComponentInChildren<MeshRenderer> ().material;
        cachedTransform = transform;
    }

    public void Initialize (BoidSettings settings, Transform target) {
        this.target = target;
        this.settings = settings;

        position = cachedTransform.position;
        forward = cachedTransform.forward;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;
        stochasticTimer = Random.Range (0, Mathf.Max (settings.currentInterval, MinStochasticInterval));
        RefreshStochasticForce ();
    }

    public void SetColour (Color col) {
        if (material != null) {
            material.color = col;
        }
    }

    public void UpdateBoid () {
        Vector3 acceleration = Vector3.zero;

        if (target != null) {
            Vector3 offsetToTarget = (target.position - position);
            acceleration = SteerTowards (offsetToTarget) * settings.targetWeight;
        }

        if (numPerceivedFlockmates != 0) {
            centreOfFlockmates /= numPerceivedFlockmates;

            Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);

            var alignmentForce = SteerTowards (avgFlockHeading) * settings.alignWeight;
            var cohesionForce = SteerTowards (offsetToFlockmatesCentre) * settings.cohesionWeight;
            var seperationForce = SteerTowards (avgAvoidanceHeading) * settings.seperateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }

        if (IsHeadingForCollision ()) {
            Vector3 collisionAvoidDir = ObstacleRays ();
            Vector3 collisionAvoidForce = SteerTowards (collisionAvoidDir) * settings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        UpdateStochasticForce ();
        acceleration += settings.currentForce + stochasticForce;

        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector3 dir = speed > Mathf.Epsilon ? velocity / speed : forward;
        speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
        velocity = dir * speed;

        MoveWithObstacleCollision (velocity * Time.deltaTime);
        KeepInsideAquarium ();

        speed = velocity.magnitude;
        dir = speed > Mathf.Epsilon ? velocity / speed : forward;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;
    }

    bool IsHeadingForCollision () {
        RaycastHit hit;
        if (Physics.SphereCast (position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask)) {
            return true;
        }

        return false;
    }

    Vector3 ObstacleRays () {
        Vector3[] rayDirections = BoidHelper.directions;

        for (int i = 0; i < rayDirections.Length; i++) {
            Vector3 dir = cachedTransform.TransformDirection (rayDirections[i]);
            Ray ray = new Ray (position, dir);
            if (!Physics.SphereCast (ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask)) {
                return dir;
            }
        }

        return forward;
    }

    Vector3 SteerTowards (Vector3 vector) {
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude (v, settings.maxSteerForce);
    }

    void UpdateStochasticForce () {
        if (settings == null || !settings.usePerBoidTurbulence || settings.sigma <= Mathf.Epsilon) {
            stochasticForce = Vector3.zero;
            stochasticTimer = 0;
            return;
        }

        float interval = Mathf.Max (settings.currentInterval, MinStochasticInterval);
        stochasticTimer += Time.deltaTime;
        if (stochasticTimer < interval) {
            return;
        }

        stochasticTimer %= interval;
        RefreshStochasticForce ();
    }

    void RefreshStochasticForce () {
        if (settings == null || !settings.usePerBoidTurbulence || settings.sigma <= Mathf.Epsilon) {
            stochasticForce = Vector3.zero;
            return;
        }

        float activeSigma = settings.sigma;
        float u1 = Mathf.Max (Random.value, 0.0001f);
        float u2 = Mathf.Max (Random.value, 0.0001f);
        float radius = Mathf.Sqrt (-2f * Mathf.Log (u1));
        float angle = 2f * Mathf.PI * u2;

        float z1 = radius * Mathf.Cos (angle);
        float z2 = radius * Mathf.Sin (angle);

        float u3 = Mathf.Max (Random.value, 0.0001f);
        float u4 = Mathf.Max (Random.value, 0.0001f);
        float z3 = Mathf.Sqrt (-2f * Mathf.Log (u3)) * Mathf.Cos (2f * Mathf.PI * u4);

        stochasticForce = new Vector3 (
            activeSigma * z1,
            activeSigma * z2 * 0.3f,
            activeSigma * z3
        ) * settings.turbulenceWeight;
    }

    void MoveWithObstacleCollision (Vector3 displacement) {
        float distance = displacement.magnitude;
        if (distance <= Mathf.Epsilon) {
            return;
        }

        Vector3 direction = displacement / distance;
        RaycastHit hit;
        if (Physics.SphereCast (position, settings.boundsRadius, direction, out hit, distance + CollisionSkinWidth, settings.obstacleMask)) {
            float safeDistance = Mathf.Max (0, hit.distance - CollisionSkinWidth);
            cachedTransform.position = position + direction * safeDistance;

            Vector3 slideVelocity = Vector3.ProjectOnPlane (velocity, hit.normal);
            Vector3 awayFromObstacle = hit.normal * settings.minSpeed;
            velocity = Vector3.ClampMagnitude (slideVelocity + awayFromObstacle, settings.maxSpeed);
            ResolveObstacleOverlap ();
            return;
        }

        cachedTransform.position = position + displacement;
        ResolveObstacleOverlap ();
    }

    void ResolveObstacleOverlap () {
        int hitCount = Physics.OverlapSphereNonAlloc (cachedTransform.position, settings.boundsRadius, penetrationHits, settings.obstacleMask);
        for (int i = 0; i < hitCount; i++) {
            Collider obstacle = penetrationHits[i];
            if (obstacle == null) {
                continue;
            }

            Vector3 closestPoint = GetSafeClosestPoint (obstacle, cachedTransform.position);
            Vector3 awayFromObstacle = cachedTransform.position - closestPoint;
            if (awayFromObstacle.sqrMagnitude < 0.0001f) {
                awayFromObstacle = cachedTransform.position - obstacle.bounds.center;
            }

            if (awayFromObstacle.sqrMagnitude < 0.0001f) {
                awayFromObstacle = -forward;
            }

            Vector3 normal = awayFromObstacle.normalized;
            cachedTransform.position = closestPoint + normal * (settings.boundsRadius + CollisionSkinWidth);
            velocity = Vector3.ProjectOnPlane (velocity, -normal) + normal * settings.minSpeed;
            velocity = Vector3.ClampMagnitude (velocity, settings.maxSpeed);
        }
    }

    Vector3 GetSafeClosestPoint (Collider obstacle, Vector3 point) {
        MeshCollider meshCollider = obstacle as MeshCollider;
        if (meshCollider != null && !meshCollider.convex) {
            return obstacle.bounds.ClosestPoint (point);
        }

        return obstacle.ClosestPoint (point);
    }

    void KeepInsideAquarium () {
        Vector3 halfSize = settings.aquariumSize * 0.5f;
        Vector3 min = settings.aquariumCentre - halfSize + Vector3.one * settings.boundsRadius;
        Vector3 max = settings.aquariumCentre + halfSize - Vector3.one * settings.boundsRadius;
        Vector3 currentPosition = cachedTransform.position;
        Vector3 clampedPosition = new Vector3 (
            Mathf.Clamp (currentPosition.x, min.x, max.x),
            Mathf.Clamp (currentPosition.y, min.y, max.y),
            Mathf.Clamp (currentPosition.z, min.z, max.z)
        );

        if (clampedPosition == currentPosition) {
            return;
        }

        Vector3 inwardDirection = (clampedPosition - currentPosition).normalized;
        if (inwardDirection != Vector3.zero) {
            velocity = Vector3.ProjectOnPlane (velocity, -inwardDirection);
            velocity += inwardDirection * settings.minSpeed;
            velocity = Vector3.ClampMagnitude (velocity, settings.maxSpeed);
        }

        cachedTransform.position = clampedPosition;
    }

}
