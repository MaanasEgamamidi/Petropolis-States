using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Handles additive PetChoose loading: disables World's EventSystem before load (avoids "2 EventSystems" warning),
/// freezes entire World after PetChoose loads, restores and locks cursor when PetChoose unloads.
/// </summary>
public class AdditiveSceneHelper : MonoBehaviour
{
    const string PetChooseScene = "PetChoose";
    const string WorldScene = "World";

    /// <summary>Offset applied to PetChoose content so it spawns far from World (avoids overlap in editor and prevents bleed-through).</summary>
    static readonly Vector3 PetChooseOffset = new Vector3(0, 0, 25000f);

    static readonly List<GameObject> _disabledWorldRoots = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (FindFirstObjectByType<AdditiveSceneHelper>() != null) return;

        var go = new GameObject("AdditiveSceneHelper");
        DontDestroyOnLoad(go);
        go.AddComponent<AdditiveSceneHelper>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != PetChooseScene || mode != LoadSceneMode.Additive) return;

        // Freeze entire World scene: deactivate all root GameObjects (nothing runs, no cameras, no EventSystem, no scripts)
        var worldScene = SceneManager.GetSceneByName(WorldScene);
        if (worldScene.isLoaded)
            FreezeScene(worldScene);

        // Move PetChoose content far from World so scenes don't overlap (25km along Z)
        foreach (var root in scene.GetRootGameObjects())
        {
            root.transform.position += PetChooseOffset;
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != PetChooseScene) return;

        // Unfreeze World scene
        var worldScene = SceneManager.GetSceneByName(WorldScene);
        if (worldScene.isLoaded)
            UnfreezeScene();

        // Restore cursor lock for World (look-around mode)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Call before loading PetChoose additively. Disables only World's EventSystem to avoid "2 EventSystems" warning.
    /// Keeps camera and AudioListener active during the fade; full freeze happens after PetChoose loads.</summary>
    public static void FreezeWorldBeforePetChooseLoad()
    {
        _disabledWorldRoots.Clear();
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (es.gameObject.scene.name == WorldScene && es.gameObject.activeSelf)
            {
                es.gameObject.SetActive(false);
                _disabledWorldRoots.Add(es.gameObject);
                break;
            }
        }
    }

    static void FreezeScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.activeSelf)
            {
                root.SetActive(false);
                if (!_disabledWorldRoots.Contains(root))
                    _disabledWorldRoots.Add(root);
            }
        }
    }

    static void UnfreezeScene()
    {
        foreach (var root in _disabledWorldRoots)
        {
            if (root != null)
                root.SetActive(true);
        }
        _disabledWorldRoots.Clear();
    }
}
