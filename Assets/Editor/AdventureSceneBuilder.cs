using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdventureSceneBuilder
{
    const string ScenePath = "Assets/Scenes/BoidsAdventure.unity";
    const string AdventureSettingsPath = "Assets/Settings/AdventureSettings.asset";
    const string TerrainMaterialPath = "Assets/Materials/AdventureTerrain.mat";
    const string SubmarineMaterialPath = "Assets/Materials/AdventureSubmarine.mat";
    const string AccentMaterialPath = "Assets/Materials/AdventureCyanAccent.mat";
    const string GlassMaterialPath = "Assets/Materials/AdventureGlass.mat";
    const string AdventureDensityShaderPath = "Assets/MarchingCubes/Compute/AdventureDensity.compute";

    static readonly Vector4 SeaWorldParams = new Vector4(5.08f, 1.06f, 0.4f, -0.03f);

    [MenuItem("Boids/Build Adventure Scene")]
    public static void BuildScene()
    {
        EnsureFolders();
        BoidSettings settings = CreateAdventureSettings();
        Material terrainMaterial = CreateTerrainMaterial();
        Material submarineMaterial = CreateMaterial(SubmarineMaterialPath, new Color(0.92f, 0.42f, 0.16f, 1f));
        Material accentMaterial = CreateMaterial(AccentMaterialPath, new Color(0.05f, 0.95f, 1f, 1f));
        Material glassMaterial = CreateMaterial(GlassMaterialPath, new Color(0.25f, 0.85f, 1f, 0.55f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BoidsAdventure";

        ConfigureRenderSettings();
        CreateLighting();

        MeshGenerator meshGenerator = CreateMarchingCubesWorld(terrainMaterial);
        CreateMeshIntegrityValidator(meshGenerator);
        CreateChunkContinuityValidator(meshGenerator);
        CreateTerrainColourGenerator(terrainMaterial);
        Transform submarine = CreateSubmarine(submarineMaterial, accentMaterial, glassMaterial);
        CreateSpawnValidator(submarine);
        CreateAdventureCamera(submarine);
        CreateBoidSystem(settings);
        CreateAdventureSpawner(settings);
        CreateExplorationHud();
        CreateWorldMarkers();

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetAdventureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AdventureSceneBuilder] Created exploration scene at {ScenePath}");
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Settings");
        EnsureFolder("Assets", "Materials");
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static BoidSettings CreateAdventureSettings()
    {
        BoidSettings settings = AssetDatabase.LoadAssetAtPath<BoidSettings>(AdventureSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<BoidSettings>();
            AssetDatabase.CreateAsset(settings, AdventureSettingsPath);
        }

        settings.minSpeed = 3f;
        settings.maxSpeed = 6.2f;
        settings.perceptionRadius = 2.8f;
        settings.avoidanceRadius = 1.25f;
        settings.maxSteerForce = 8f;
        settings.alignWeight = 2f;
        settings.cohesionWeight = 1f;
        settings.seperateWeight = 2.5f;
        settings.targetWeight = 0.55f;
        settings.obstacleMask = LayerMaskFor("Obstacle", 9);
        settings.boundsRadius = 0.27f;
        settings.avoidCollisionWeight = 18f;
        settings.collisionAvoidDst = 4.5f;
        settings.aquariumCentre = new Vector3(0f, 14f, 0f);
        settings.aquariumSize = new Vector3(140f, 54f, 140f);
        settings.useRoamingTarget = true;
        settings.targetChangeInterval = 9f;
        settings.targetReachDistance = 4f;
        settings.targetCentreDeadZone = 12f;
        settings.targetWallPadding = 8f;
        settings.sigma = 0.18f;
        settings.currentInterval = 2f;
        settings.currentForce = Vector3.zero;
        settings.usePerBoidTurbulence = true;
        settings.turbulenceWeight = 2.5f;
        settings.fishCount = 50;
        settings.mobileFishCountCap = 50;

        EditorUtility.SetDirty(settings);
        return settings;
    }

    static MeshGenerator CreateMarchingCubesWorld(Material terrainMaterial)
    {
        GameObject root = new GameObject("Marching Cubes Ocean");

        GameObject densityObject = new GameObject("Underwater Adventure Density");
        densityObject.transform.SetParent(root.transform);
        AdventureDensity density = densityObject.AddComponent<AdventureDensity>();
        density.densityShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AdventureDensityShaderPath);
        ConfigureAdventureDensityDefaults(density);

        GameObject meshObject = new GameObject("Mesh Generator");
        meshObject.transform.SetParent(root.transform);
        MeshGenerator meshGenerator = meshObject.AddComponent<MeshGenerator>();
        meshGenerator.densityGenerator = density;
        meshGenerator.fixedMapSize = true;
        meshGenerator.numChunks = new Vector3Int(7, 3, 7);
        meshGenerator.viewDistance = 80f;
        meshGenerator.autoUpdateInEditor = false;
        meshGenerator.autoUpdateInGame = true;
        meshGenerator.shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/MarchingCubes/Compute/MarchingCubes.compute");
        meshGenerator.mat = terrainMaterial;
        meshGenerator.generateColliders = true;
        meshGenerator.chunkLayer = LayerIndexFor("Obstacle", 9);
        meshGenerator.isoLevel = 0f;
        meshGenerator.boundsSize = 24f;
        meshGenerator.offset = Vector3.zero;
        meshGenerator.numPointsPerAxis = 24;
        meshGenerator.showBoundsGizmo = true;
        meshGenerator.boundsGizmoCol = new Color(0.1f, 0.9f, 1f, 0.08f);
        return meshGenerator;
    }

    static void CreateMeshIntegrityValidator(MeshGenerator meshGenerator)
    {
        GameObject validatorObject = new GameObject("Mesh Integrity Validator");
        MeshIntegrityValidator validator = validatorObject.AddComponent<MeshIntegrityValidator>();
        validator.meshGenerator = meshGenerator;
        validator.logSummaryOnStart = true;
        validator.drawChunkBounds = true;
        validator.drawProblemMarkers = true;
    }

    static void CreateChunkContinuityValidator(MeshGenerator meshGenerator)
    {
        GameObject validatorObject = new GameObject("Chunk Continuity Validator");
        ChunkContinuityValidator validator = validatorObject.AddComponent<ChunkContinuityValidator>();
        validator.meshGenerator = meshGenerator;
        validator.logSummaryOnStart = true;
        validator.drawSeamGizmos = true;
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

    static void CreateTerrainColourGenerator(Material terrainMaterial)
    {
        GameObject colourObject = new GameObject("Terrain Colour Generator");
        ColourGenerator colourGenerator = colourObject.AddComponent<ColourGenerator>();
        colourGenerator.mat = terrainMaterial;
        colourGenerator.normalOffsetWeight = 0f;
        colourGenerator.seaWorldParams = SeaWorldParams;
        colourGenerator.gradient = CreateUnderwaterGradient();
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

    static Transform CreateSubmarine(Material bodyMaterial, Material accentMaterial, Material glassMaterial)
    {
        GameObject submarine = new GameObject("Player Submarine");
        submarine.transform.position = new Vector3(-52f, 14f, -54f);
        submarine.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

        SubmarinePlayerController controller = submarine.AddComponent<SubmarinePlayerController>();
        controller.obstacleMask = LayerMaskFor("Obstacle", 9);
        controller.maxSpeed = 10f;
        controller.reverseSpeed = 4f;
        controller.acceleration = 9f;
        controller.boostMultiplier = 1.7f;
        controller.collisionRadius = 1.1f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(submarine.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(1.1f, 2.2f, 1.1f);
        AssignMaterialAndRemoveCollider(body, bodyMaterial);

        GameObject cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cockpit.name = "Cockpit";
        cockpit.transform.SetParent(submarine.transform, false);
        cockpit.transform.localPosition = new Vector3(0f, 0.45f, 0.65f);
        cockpit.transform.localScale = new Vector3(0.75f, 0.45f, 0.55f);
        AssignMaterialAndRemoveCollider(cockpit, glassMaterial);

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.name = "Tail Fin";
        tail.transform.SetParent(submarine.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0.05f, -2.25f);
        tail.transform.localScale = new Vector3(0.18f, 1.2f, 0.7f);
        AssignMaterialAndRemoveCollider(tail, accentMaterial);

        GameObject leftFin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftFin.name = "Left Fin";
        leftFin.transform.SetParent(submarine.transform, false);
        leftFin.transform.localPosition = new Vector3(-1.05f, -0.05f, -0.2f);
        leftFin.transform.localScale = new Vector3(1.0f, 0.12f, 0.7f);
        AssignMaterialAndRemoveCollider(leftFin, accentMaterial);

        GameObject rightFin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightFin.name = "Right Fin";
        rightFin.transform.SetParent(submarine.transform, false);
        rightFin.transform.localPosition = new Vector3(1.05f, -0.05f, -0.2f);
        rightFin.transform.localScale = new Vector3(1.0f, 0.12f, 0.7f);
        AssignMaterialAndRemoveCollider(rightFin, accentMaterial);

        GameObject scanLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        scanLight.name = "Forward Light";
        scanLight.transform.SetParent(submarine.transform, false);
        scanLight.transform.localPosition = new Vector3(0f, -0.08f, 2.15f);
        scanLight.transform.localScale = Vector3.one * 0.25f;
        AssignMaterialAndRemoveCollider(scanLight, accentMaterial);

        Light light = scanLight.AddComponent<Light>();
        light.type = LightType.Spot;
        light.range = 32f;
        light.spotAngle = 38f;
        light.intensity = 2.8f;
        light.color = new Color(0.35f, 0.95f, 1f);

        return submarine.transform;
    }

    static void CreateSpawnValidator(Transform submarine)
    {
        GameObject validatorObject = new GameObject("Adventure Spawn Validator");
        AdventureSpawnValidator validator = validatorObject.AddComponent<AdventureSpawnValidator>();
        validator.player = submarine.GetComponent<SubmarinePlayerController>();
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

    static void CreateAdventureCamera(Transform submarine)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = submarine.position + new Vector3(0f, 4f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.07f, 0.12f);
        camera.fieldOfView = 67f;
        camera.farClipPlane = 260f;
        cameraObject.AddComponent<AudioListener>();

        AdventureCameraFollow follow = cameraObject.AddComponent<AdventureCameraFollow>();
        follow.target = submarine;
        follow.followOffset = new Vector3(0f, 2.2f, -7.2f);
        follow.lookAheadDistance = 9.5f;
        follow.smoothTime = 0.10f;
        follow.obstacleMask = LayerMaskFor("Obstacle", 9);
    }

    static BoidManager CreateBoidSystem(BoidSettings settings)
    {
        GameObject managerObject = new GameObject("Adventure Boid Manager");
        BoidManager manager = managerObject.AddComponent<BoidManager>();
        manager.settings = settings;
        manager.compute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Scripts/BoidCompute.compute");
        return manager;
    }

    static void CreateAdventureSpawner(BoidSettings settings)
    {
        Boid prefab = AssetDatabase.LoadAssetAtPath<Boid>("Assets/Prefabs/Boid.prefab");
        GameObject spawnerObject = new GameObject("Adventure Fish Spawner");
        AdventureBoidSpawner spawner = spawnerObject.AddComponent<AdventureBoidSpawner>();
        spawner.settings = settings;
        spawner.prefab = prefab;
        spawner.swimBoundsCenter = settings.aquariumCentre;
        spawner.swimBoundsSize = settings.aquariumSize;
        spawner.schoolCenter = new Vector3(20f, 16f, 18f);
        spawner.schoolSpawnRadius = 14f;
        spawner.obstacleMask = LayerMaskFor("Obstacle", 9);
        spawner.clearanceRadius = 1.25f;
        spawner.spawnAttemptsPerFish = 120;
        spawner.colour = new Color(0.95f, 0.35f, 0.25f);
    }

    static void CreateExplorationHud()
    {
        GameObject hudObject = new GameObject("Adventure Exploration HUD Bootstrap");
        hudObject.AddComponent<AdventureExplorationHUD>();
    }

    static void CreateWorldMarkers()
    {
        GameObject boundsMarker = new GameObject("Adventure Swim Bounds");
        boundsMarker.transform.position = new Vector3(0f, 14f, 0f);
    }

    static void ConfigureRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.01f, 0.08f, 0.12f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.ambientLight = new Color(0.08f, 0.18f, 0.22f);
    }

    static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Ocean Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.55f, 0.9f, 1f);
        light.intensity = 0.8f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 20f);
    }

    static Material CreateTerrainMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
        Shader shader = Shader.Find("Custom/Sea World");
        if (shader == null)
        {
            shader = FindDefaultShader();
            Debug.LogWarning("[AdventureSceneBuilder] Custom/Sea World shader not found. Falling back to default terrain shader.");
        }

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, TerrainMaterialPath);
        }

        material.shader = shader;
        material.SetVector("params", SeaWorldParams);
        material.SetFloat("_Glossiness", 0.15f);
        material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = FindDefaultShader();
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Shader FindDefaultShader()
    {
        Shader shader = Shader.Find("Standard");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Sprites/Default");
    }

    static void AssignMaterialAndRemoveCollider(GameObject go, Material material)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    static void TryGenerateTerrain(MeshGenerator meshGenerator)
    {
        try
        {
            if (SystemInfo.supportsComputeShaders)
            {
                meshGenerator.Run();
            }
            else
            {
                Debug.LogWarning("[AdventureSceneBuilder] Compute shaders are unavailable in this editor context. Terrain will generate at runtime if supported.");
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[AdventureSceneBuilder] Terrain generation skipped in editor: {exception.Message}");
        }
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

    static void SetAdventureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }
}
