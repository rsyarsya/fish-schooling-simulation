using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Checks scene imports and serialized script references in batch mode.</summary>
public static class RepositoryValidation
{
    public static void Validate()
    {
        try
        {
            foreach (string path in new[] {
                "Assets/Scenes/Obstacles.unity",
                "Assets/Scenes/BoidsAdventure.unity"
            })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                            throw new InvalidOperationException($"Missing script in {path}: {child.name}");
                    }
                }
                var manager = UnityEngine.Object.FindAnyObjectByType<BoidManager>();
                if (manager == null || manager.settings == null)
                    throw new InvalidOperationException($"Missing manager/settings: {path}");
                Debug.Log($"[RepositoryValidation] PASS: {path}");
            }
            Debug.Log("[RepositoryValidation] PASS: Unity import and scene references. Gameplay not tested.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
