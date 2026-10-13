using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BoidSettings : ScriptableObject {
    // Settings
    public float minSpeed = 5;
    public float maxSpeed = 8;
    public float perceptionRadius = 2.8f;
    public float avoidanceRadius = 1.25f;
    public float maxSteerForce = 8;

    public float alignWeight = 2;
    public float cohesionWeight = 1;
    public float seperateWeight = 2.5f;

    public float targetWeight = 0.8f;

    [Header ("Collisions")]
    public LayerMask obstacleMask;
    public float boundsRadius = .27f;
    public float avoidCollisionWeight = 14;
    public float collisionAvoidDst = 3.5f;

    [Header("=== AQUARIUM BOUNDS ===")]
    [Tooltip("Center of the safe swimming volume.")]
    public Vector3 aquariumCentre = new Vector3(0, 16.6f, 0);

    [Tooltip("Size of the safe swimming volume. Keep this slightly inside the visible walls.")]
    public Vector3 aquariumSize = new Vector3(76, 30, 76);

    [Header("=== ROAMING TARGET ===")]
    [Tooltip("Moves a hidden target around the aquarium so the flock explores instead of staying near spawn.")]
    public bool useRoamingTarget = true;

    [Tooltip("How long before the hidden target picks a new destination, even if the flock has not reached it.")]
    public float targetChangeInterval = 8;

    [Tooltip("When the flock centre gets this close to the hidden target, a new destination is chosen.")]
    public float targetReachDistance = 5;

    [Tooltip("Keeps random destinations away from the exact centre so fish are encouraged to visit outer areas.")]
    public float targetCentreDeadZone = 14;

    [Tooltip("Keeps random destinations away from glass walls.")]
    public float targetWallPadding = 6;

    [Header("=== STOCHASTIC OCEAN CURRENT ===")]
    [Tooltip("Standard deviation sigma of ocean turbulence. Range: 0.0-0.8 realistic, up to 2.0 extreme.")]
    public float sigma = 0.3f;

    [Tooltip("How often (seconds) a new current vector is generated via Box-Muller.")]
    public float currentInterval = 2.0f;

    [Tooltip("Current force vector, updated by StochasticCurrent.cs. Read-only at runtime.")]
    [HideInInspector] public Vector3 currentForce = Vector3.zero;

    [Tooltip("Applies independent stochastic perturbation to each boid, so sigma can affect polarization and spread.")]
    public bool usePerBoidTurbulence = true;

    [Tooltip("Multiplier for each boid's independent stochastic perturbation. This is tuning, not a direct measured turbulence value.")]
    public float turbulenceWeight = 6f;

    [Header("=== UI CONTROL ===")]
    [Tooltip("Number of fish to spawn. Clamped 10-500 at runtime.")]
    public int fishCount = 50;

    [Tooltip("Maximum fish spawned on handheld/mobile platforms. Keeps WebGL/mobile demos responsive.")]
    public int mobileFishCountCap = 100;

}
