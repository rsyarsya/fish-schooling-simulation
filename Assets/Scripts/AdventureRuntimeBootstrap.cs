using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AdventureRuntimeBootstrap
{
    const string AdventureSceneName = "BoidsAdventure";
    const string AdventureDensityShaderPath = "Assets/MarchingCubes/Compute/AdventureDensity.compute";
    static readonly Vector4 SeaWorldParams = new Vector4(5.08f, 1.06f, 0.4f, -0.03f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ConfigureAdventureScene()
    {
        if (SceneManager.GetActiveScene().name != AdventureSceneName)
        {
            return;
        }

        RemoveSimulationUi();
        ConfigureWorld();
        ConfigureBoidSettings();
        EnsureMeshIntegrityValidator();
        EnsureChunkContinuityValidator();
        ConfigureFishSpawner();
        ConfigurePlayerSpawn();
        ConfigureAdventureCamera();
        EnsureExplorationHud();
    }

    static void RemoveSimulationUi()
    {
        foreach (BoidMetrics metrics in Object.FindObjectsByType<BoidMetrics>(FindObjectsSortMode.None))
        {
            Object.Destroy(metrics.gameObject);
        }

        foreach (FishSchoolScanner scanner in Object.FindObjectsByType<FishSchoolScanner>(FindObjectsSortMode.None))
        {
            Object.Destroy(scanner.gameObject);
        }
    }

    static void ConfigureWorld()
    {
        Material terrainMaterial = null;
        foreach (MeshGenerator generator in Object.FindObjectsByType<MeshGenerator>(FindObjectsSortMode.None))
        {
            generator.fixedMapSize = true;
            generator.numChunks = new Vector3Int(7, 3, 7);
            generator.viewDistance = 80f;
            generator.autoUpdateInEditor = false;
            generator.autoUpdateInGame = true;
            generator.generateColliders = true;
            generator.chunkLayer = LayerIndexFor("Obstacle", 9);
            generator.isoLevel = 0f;
            generator.boundsSize = 24f;
            generator.offset = Vector3.zero;
            generator.numPointsPerAxis = 24;
            generator.densityGenerator = EnsureAdventureDensity(generator);

            if (terrainMaterial == null && generator.mat != null)
            {
                terrainMaterial = generator.mat;
            }
        }

        EnsureTerrainColourGenerator(terrainMaterial);
    }

    static AdventureDensity EnsureAdventureDensity(MeshGenerator generator)
    {
        AdventureDensity density = null;
        if (generator.densityGenerator != null)
        {
            density = generator.densityGenerator.GetComponent<AdventureDensity>();
        }

        if (density == null)
        {
            Transform parent = generator.densityGenerator != null
                ? generator.densityGenerator.transform
                : generator.transform;
            density = parent.GetComponent<AdventureDensity>();
            if (density == null)
            {
                density = parent.gameObject.AddComponent<AdventureDensity>();
            }
        }

        ConfigureAdventureDensityDefaults(density);

#if UNITY_EDITOR
        if (density.densityShader == null)
        {
            density.densityShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AdventureDensityShaderPath);
        }
#endif

        return density;
    }

    static void ConfigureAdventureDensityDefaults(AdventureDensity density)
    {
        density.seed = 12;
        density.floorBase = -13f;
        density.floorAmplitude = 4.5f;
        density.ceilingBase = 30f;
        density.ceilingAmplitude = 7f;
        density.canyonWidth = 22f;
        density.canyonMeander = 22f;
        density.edgeWallPadding = 12f;
        density.surfaceRoughness = 2.4f;
        density.floorNoiseScale = 0.035f;
        density.wallNoiseScale = 0.055f;
        density.detailNoiseScale = 0.16f;
    }

    static void ConfigureBoidSettings()
    {
        BoidSettings configuredSettings = null;
        foreach (BoidManager manager in Object.FindObjectsByType<BoidManager>(FindObjectsSortMode.None))
        {
            if (manager.settings == null)
            {
                continue;
            }

            configuredSettings = Object.Instantiate(manager.settings);
            ApplyAdventureSettings(configuredSettings);
            manager.settings = configuredSettings;
        }

        foreach (AdventureBoidSpawner spawner in Object.FindObjectsByType<AdventureBoidSpawner>(FindObjectsSortMode.None))
        {
            if (spawner.settings == null)
            {
                continue;
            }

            if (configuredSettings == null)
            {
                configuredSettings = Object.Instantiate(spawner.settings);
                ApplyAdventureSettings(configuredSettings);
            }

            spawner.settings = configuredSettings;
        }
    }

    static void ApplyAdventureSettings(BoidSettings settings)
    {
        settings.aquariumCentre = new Vector3(0f, 14f, 0f);
        settings.aquariumSize = new Vector3(140f, 54f, 140f);
        settings.targetWallPadding = 12f;
        settings.targetCentreDeadZone = 16f;
        settings.targetReachDistance = 5f;
        settings.targetChangeInterval = 10f;
    }

    static void EnsureMeshIntegrityValidator()
    {
        MeshGenerator generator = Object.FindFirstObjectByType<MeshGenerator>();
        if (generator == null)
        {
            return;
        }

        MeshIntegrityValidator validator = Object.FindFirstObjectByType<MeshIntegrityValidator>();
        if (validator == null)
        {
            GameObject validatorObject = new GameObject("Mesh Integrity Validator");
            validator = validatorObject.AddComponent<MeshIntegrityValidator>();
        }

        validator.meshGenerator = generator;
        validator.logSummaryOnStart = true;
        validator.drawChunkBounds = true;
        validator.drawProblemMarkers = true;
    }

    static void EnsureChunkContinuityValidator()
    {
        MeshGenerator generator = Object.FindFirstObjectByType<MeshGenerator>();
        if (generator == null)
        {
            return;
        }

        ChunkContinuityValidator validator = Object.FindFirstObjectByType<ChunkContinuityValidator>();
        if (validator == null)
        {
            GameObject validatorObject = new GameObject("Chunk Continuity Validator");
            validator = validatorObject.AddComponent<ChunkContinuityValidator>();
        }

        validator.meshGenerator = generator;
        validator.logSummaryOnStart = true;
        validator.drawSeamGizmos = true;
    }

    static void EnsureTerrainColourGenerator(Material terrainMaterial)
    {
        ColourGenerator colourGenerator = Object.FindFirstObjectByType<ColourGenerator>();
        if (colourGenerator == null)
        {
            GameObject colourObject = new GameObject("Terrain Colour Generator");
            colourGenerator = colourObject.AddComponent<ColourGenerator>();
        }

        if (terrainMaterial != null)
        {
            Shader seaWorldShader = Shader.Find("Custom/Sea World");
            if (seaWorldShader != null)
            {
                terrainMaterial.shader = seaWorldShader;
            }

            terrainMaterial.SetVector("params", SeaWorldParams);
        }

        if (terrainMaterial != null)
        {
            colourGenerator.mat = terrainMaterial;
        }

        colourGenerator.normalOffsetWeight = 0f;
        colourGenerator.seaWorldParams = SeaWorldParams;
        colourGenerator.gradient = CreateUnderwaterGradient();
    }

    static void ConfigureFishSpawner()
    {
        foreach (AdventureBoidSpawner spawner in Object.FindObjectsByType<AdventureBoidSpawner>(FindObjectsSortMode.None))
        {
            spawner.swimBoundsCenter = new Vector3(0f, 14f, 0f);
            spawner.swimBoundsSize = new Vector3(140f, 54f, 140f);
            spawner.schoolCenter = new Vector3(20f, 16f, 18f);
            spawner.schoolSpawnRadius = 14f;
            spawner.obstacleMask = LayerMaskFor("Obstacle", 9);
            spawner.clearanceRadius = 1.25f;
            spawner.spawnAttemptsPerFish = 120;
        }
    }

    static void ConfigurePlayerSpawn()
    {
        SubmarinePlayerController player = Object.FindFirstObjectByType<SubmarinePlayerController>();
        if (player == null)
        {
            return;
        }

        AdventureSpawnValidator validator = Object.FindFirstObjectByType<AdventureSpawnValidator>();
        if (validator == null)
        {
            GameObject validatorObject = new GameObject("Adventure Spawn Validator");
            validator = validatorObject.AddComponent<AdventureSpawnValidator>();
        }

        validator.player = player;
        validator.obstacleMask = LayerMaskFor("Obstacle", 9);
        validator.overlapRadius = 2.2f;
        validator.forwardClearance = 18f;
        validator.verticalClearance = 5f;
        validator.fallbackPosition = new Vector3(-52f, 20f, -54f);
        validator.fallbackYaw = 35f;
        validator.candidates = new[]
        {
            new AdventureSpawnValidator.SpawnCandidate { position = new Vector3(-52f, 14f, -54f), yaw = 35f },
            new AdventureSpawnValidator.SpawnCandidate { position = new Vector3(-32f, 16f, -32f), yaw = 25f },
            new AdventureSpawnValidator.SpawnCandidate { position = new Vector3(20f, 16f, 10f), yaw = -20f },
            new AdventureSpawnValidator.SpawnCandidate { position = new Vector3(-8f, 20f, 48f), yaw = 180f }
        };
    }

    static void ConfigureAdventureCamera()
    {
        AdventureCameraFollow follow = Object.FindFirstObjectByType<AdventureCameraFollow>();
        if (follow != null)
        {
            follow.followOffset = new Vector3(0f, 2.2f, -7.2f);
            follow.lookAheadDistance = 9.5f;
            follow.smoothTime = 0.10f;
            follow.rotationSmoothSpeed = 6.5f;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.fieldOfView = 64f;
            camera.farClipPlane = 240f;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.01f, 0.08f, 0.12f);
        RenderSettings.fogDensity = 0.018f;
    }

    static void EnsureExplorationHud()
    {
        if (Object.FindFirstObjectByType<AdventureExplorationHUD>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("Adventure Exploration HUD Bootstrap");
        hudObject.AddComponent<AdventureExplorationHUD>();
    }

    static Gradient CreateUnderwaterGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.14f, 0.08f, 0.23f), 0f),
                new GradientColorKey(new Color(0.02f, 0.23f, 0.47f), 0.25f),
                new GradientColorKey(new Color(0.03f, 0.49f, 0.80f), 0.50f),
                new GradientColorKey(new Color(0.28f, 0.78f, 0.66f), 0.75f),
                new GradientColorKey(new Color(0.85f, 0.71f, 0.42f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return gradient;
    }

    static LayerMask LayerMaskFor(string layerName, int fallbackLayer)
    {
        return 1 << LayerIndexFor(layerName, fallbackLayer);
    }

    static int LayerIndexFor(string layerName, int fallbackLayer)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : fallbackLayer;
    }
}
