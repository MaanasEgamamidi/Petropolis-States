using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Tooltip("Must match <see cref=\"WorldPauseController.helpOverlaySceneName\"/> and Build Settings.")]
    [SerializeField] string helpQaSceneName = "HelpQa";

    [Tooltip("Pixels to offset the \"Ask the guide\" button above the Play button (created at runtime).")]
    [SerializeField] float interactiveHelpButtonOffsetY = 150f;

    bool _helpQaLoadInProgress;

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnHelpQaSceneUnloaded;
    }

    void Start()
    {
        TryAddInteractiveHelpButton();
    }

    void TryAddInteractiveHelpButton()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;
        if (canvas.transform.Find("HelpQa") != null)
            return;

        var playTf = canvas.transform.Find("Play");
        if (playTf == null)
            return;

        var helpGo = Instantiate(playTf.gameObject, playTf.parent);
        helpGo.name = "HelpQa";
        var rt = helpGo.GetComponent<RectTransform>();
        var playRt = playTf.GetComponent<RectTransform>();
        if (rt != null && playRt != null)
        {
            rt.sizeDelta = playRt.sizeDelta;
            var pos = playRt.anchoredPosition;
            pos.y += interactiveHelpButtonOffsetY;
            rt.anchoredPosition = pos;
        }

        helpGo.transform.SetSiblingIndex(playTf.GetSiblingIndex() + 1);

        var helpBtn = helpGo.GetComponent<Button>();
        if (helpBtn != null)
        {
            helpBtn.onClick.RemoveAllListeners();
            helpBtn.onClick.AddListener(OpenInteractiveHelp);
        }

        var tmp = helpGo.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
            tmp.text = "Ask the guide";
    }

    /// <summary>
    /// Opens the interactive Help Q&amp;A additively on top of this scene (main menu → How to play).
    /// </summary>
    public void OpenInteractiveHelp()
    {
        if (_helpQaLoadInProgress)
            return;
        StartCoroutine(OpenInteractiveHelpRoutine());
    }

    IEnumerator OpenInteractiveHelpRoutine()
    {
        _helpQaLoadInProgress = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        HelpQaOverlayController.ReturnToPauseOnClose = false;
        HelpQaOverlayLoadUtility.SuspendEventSystemsAndAudioListeners();

        SceneManager.sceneUnloaded -= OnHelpQaSceneUnloaded;
        SceneManager.sceneUnloaded += OnHelpQaSceneUnloaded;

        var op = SceneManager.LoadSceneAsync(helpQaSceneName, LoadSceneMode.Additive);
        if (op != null)
        {
            while (!op.isDone)
                yield return null;
        }

        HelpQaOverlayLoadUtility.EnsureAtLeastOneActiveEventSystem();
        HelpQaOverlayLoadUtility.EnsureAtLeastOneActiveAudioListener();

        _helpQaLoadInProgress = false;
    }

    void OnHelpQaSceneUnloaded(Scene scene)
    {
        if (scene.name != helpQaSceneName)
            return;
        SceneManager.sceneUnloaded -= OnHelpQaSceneUnloaded;
        HelpQaOverlayLoadUtility.RestoreSuspendedSingletons();
    }

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
