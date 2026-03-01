using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// When entering Play mode, always loads the Main Menu scene regardless of which scene is currently open.
/// </summary>
[InitializeOnLoad]
internal static class AlwaysStartAtMainMenu
{
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";

    static AlwaysStartAtMainMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (string.Equals(activeScene.path, MainMenuScenePath, System.StringComparison.OrdinalIgnoreCase))
            return;

        // Save any unsaved changes before switching scenes
        if (EditorSceneManager.GetActiveScene().isDirty)
            EditorSceneManager.SaveOpenScenes();

        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            Debug.LogWarning($"[AlwaysStartAtMainMenu] Failed to load Main Menu scene at '{MainMenuScenePath}'. Make sure the scene exists and the path is correct.");
    }
}
