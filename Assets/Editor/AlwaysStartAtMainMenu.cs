using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// When enabled, entering Play mode always loads the Main Menu scene regardless of which scene is open.
/// Toggle via Edit > Always Start at Main Menu.
/// </summary>
[InitializeOnLoad]
internal static class AlwaysStartAtMainMenu
{
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string PrefsKey = "Petropolis_AlwaysStartAtMainMenu";

    static AlwaysStartAtMainMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>Whether the "always start at main menu" behavior is enabled. Persisted in EditorPrefs.</summary>
    public static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefsKey, true);
        set => EditorPrefs.SetBool(PrefsKey, value);
    }

    [MenuItem("Edit/Always Start at Main Menu", false, 300)]
    private static void ToggleAlwaysStartAtMainMenu()
    {
        IsEnabled = !IsEnabled;
    }

    [MenuItem("Edit/Always Start at Main Menu", true)]
    private static bool ToggleAlwaysStartAtMainMenuValidate()
    {
        Menu.SetChecked("Edit/Always Start at Main Menu", IsEnabled);
        return true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;
        if (!IsEnabled)
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
