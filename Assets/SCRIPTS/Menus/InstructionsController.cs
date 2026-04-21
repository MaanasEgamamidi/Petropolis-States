using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the Instructions scene. Play button normally loads CharacterChoose,
/// but when the scene was opened additively from the pause menu's Help button
/// (<see cref="ReturnToPauseOnPlay"/> = true), Play instead unloads this scene
/// and returns to the underlying paused world.
/// </summary>
public class InstructionsController : MonoBehaviour
{
    /// <summary>
    /// Set to true by <see cref="WorldPauseController"/> before additively loading
    /// the Instructions scene from the pause menu's Help button. Instructs this
    /// controller's Play handler to unload the scene instead of loading CharacterChoose.
    /// Consumed (reset to false) on the next Play click.
    /// </summary>
    public static bool ReturnToPauseOnPlay;

    [Tooltip("Optional full-screen black image for fade transition. Must have CanvasGroup.")]
    [SerializeField] GameObject fadeToBlackPanel;

    /// <summary>
    /// Called by the Play button. Loads CharacterChoose scene, unless this Instructions
    /// scene was opened as a help overlay from the pause menu, in which case it unloads
    /// itself and returns the player to the pause menu.
    /// </summary>
    public void Play()
    {
        if (ReturnToPauseOnPlay)
        {
            ReturnToPauseOnPlay = false;
            var scene = gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
            return;
        }

        GameObject panel = fadeToBlackPanel;
        if (panel == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                panel = canvas.transform.Find("FadePanel")?.gameObject;
        }
        if (panel != null && panel.GetComponent<CanvasGroup>() != null)
        {
            FadeTransitionRunner.RunFadeThenLoadScene(panel, 0.85f, "CharacterChoose");
            return;
        }
        SceneManager.LoadScene("CharacterChoose");
    }
}
