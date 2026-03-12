using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the Instructions scene. Play button loads CharacterChoose.
/// </summary>
public class InstructionsController : MonoBehaviour
{
    [Tooltip("Optional full-screen black image for fade transition. Must have CanvasGroup.")]
    [SerializeField] GameObject fadeToBlackPanel;

    /// <summary>
    /// Called by the Play button. Loads CharacterChoose scene.
    /// </summary>
    public void Play()
    {
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
