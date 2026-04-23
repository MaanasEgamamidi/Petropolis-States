using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// World: Escape or right bumper (RB) toggles pause; View/Back (selectButton) also opens the gameplay-settings pause menu.
/// Settings controls on the pause prefab are bound via <see cref="PauseMenuSettingsHelper"/>.
/// </summary>
[DefaultExecutionOrder(-50)]
public class WorldPauseController : MonoBehaviour
{
    /// <summary>True while the world is paused (menu open, time scale 0). Used by player/cursor code.</summary>
    public static bool IsPaused { get; private set; }

    [Header("Timing (unscaled)")]
    [SerializeField] [Min(0.02f)] float hudFadeOutDuration = 0.18f;
    [SerializeField] [Min(0.02f)] float pauseFadeInDuration = 0.14f;
    [SerializeField] [Min(0.02f)] float pauseFadeOutDuration = 0.12f;
    [SerializeField] [Min(0.02f)] float hudFadeInDuration = 0.2f;

    [Header("Pause menu")]
    [Tooltip("If null, finds a root GameObject named PauseMenu.")]
    [SerializeField] GameObject pauseMenuRoot;
    [SerializeField] int pauseCanvasSortOrder = 400;

    [Header("Quit")]
    [SerializeField] string closingSceneName = "ClosingScene";
    [SerializeField] [Min(0.02f)] float quitFadeToClosingDuration = 0.85f;

    [Header("Help overlay")]
    [Tooltip("Additively loaded from the pause menu Help button (interactive Q&A).")]
    [FormerlySerializedAs("instructionsSceneName")]
    [SerializeField] string helpOverlaySceneName = "HelpQa";

    static readonly string[] HudChildNamesUnderUi = { "MinimapCircle", "DayNightUI", "MoneyCounter" };
    static readonly string[] HudRootNames = { "Pet Statistics", "Notifications" };

    readonly List<CanvasGroup> _hudGroups = new();
    Canvas _pauseCanvas;
    CanvasGroup _pauseGroup;
    bool _paused;
    bool _busy;
    bool _settingsBound;
    bool _helpOverlayActive;
    GameObject _prevSelectedForNavDedupe;

    void Awake()
    {
        if (pauseMenuRoot == null)
            pauseMenuRoot = FindInLoadedScenesIncludingInactive("PauseMenu");

        if (pauseMenuRoot != null)
        {
            _pauseCanvas = pauseMenuRoot.GetComponent<Canvas>();
            _pauseGroup = pauseMenuRoot.GetComponent<CanvasGroup>();
            if (_pauseGroup == null)
                _pauseGroup = pauseMenuRoot.AddComponent<CanvasGroup>();

            _pauseGroup.alpha = 0f;
            _pauseGroup.interactable = false;
            _pauseGroup.blocksRaycasts = false;
            pauseMenuRoot.SetActive(false);
        }

        BuildHudCanvasGroups();
    }

    static GameObject FindInLoadedScenesIncludingInactive(string objectName)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name != objectName || !go.scene.IsValid())
                continue;
            if (go.scene != SceneManager.GetActiveScene())
                continue;
            return go;
        }

        return null;
    }

    void OnEnable()
    {
        if (pauseMenuRoot != null && !_settingsBound)
        {
            PauseMenuSettingsHelper.Bind(pauseMenuRoot.transform);
            WireBackToGameButton();
            WireQuitButton();
            WireHelpButton();
            _settingsBound = true;
        }
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void BuildHudCanvasGroups()
    {
        _hudGroups.Clear();
        var ui = GameObject.Find("UI")?.transform;
        if (ui != null)
        {
            foreach (var name in HudChildNamesUnderUi)
            {
                var t = ui.Find(name);
                if (t != null)
                    _hudGroups.Add(GetOrAddCanvasGroup(t.gameObject));
            }
        }

        foreach (var name in HudRootNames)
        {
            var go = GameObject.Find(name);
            if (go != null)
                _hudGroups.Add(GetOrAddCanvasGroup(go));
        }
    }

    static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        return cg;
    }

    void WireBackToGameButton()
    {
        foreach (var btn in pauseMenuRoot.GetComponentsInChildren<Button>(true))
        {
            string n = btn.gameObject.name;
            if (n != "BackToGame" && n != "BackToMenu")
                continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ResumeFromButton);
            break;
        }
    }

    void WireQuitButton()
    {
        foreach (var btn in pauseMenuRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name != "Quit")
                continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(QuitFromButton);
            break;
        }
    }

    void WireHelpButton()
    {
        foreach (var btn in pauseMenuRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name != "Help")
                continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OpenHelpFromButton);
            break;
        }
    }

    void OpenHelpFromButton()
    {
        if (_busy || !_paused || _helpOverlayActive)
            return;
        StartCoroutine(OpenHelpOverlayRoutine());
    }

    void ResumeFromButton()
    {
        if (_busy || !_paused)
            return;
        StartCoroutine(ClosePauseRoutine());
    }

    void QuitFromButton()
    {
        if (_busy || !_paused)
            return;

        _busy = true;
        _paused = false;
        IsPaused = false;
        Time.timeScale = 1f;

        if (_pauseGroup != null)
        {
            _pauseGroup.interactable = false;
            _pauseGroup.blocksRaycasts = false;
        }

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        RestoreGameCursorAfterMenu();

        var panel = FadeTransitionRunner.CreateFullScreenBlackFadePanel();
        FadeTransitionRunner.RunFadeThenLoadScene(panel, quitFadeToClosingDuration, closingSceneName);
    }

    void Update()
    {
        if (_busy || pauseMenuRoot == null || _helpOverlayActive)
            return;

#if ENABLE_INPUT_SYSTEM
        if (_paused)
        {
            if (GameplayInput.GamepadCancelPressedThisFrame())
            {
                // If the player is focused on a text input, first B press just deselects the field so they
                // can navigate buttons again; a second B (now that no input is focused) closes the menu.
                if (IsTypingInUiInput())
                {
                    FocusFirstPauseMenuSelectable();
                    return;
                }

                StartCoroutine(ClosePauseRoutine());
                return;
            }

            HandleGamepadPauseNavigation();
        }
#endif

        if (!PauseMenuTogglePressedThisFrame())
            return;

        if (IsTypingInUiInput())
            return;

        // Don't open pause when Escape just closed (or is about to close) an NPC chat window.
        if (!_paused && (NpcProximityChatOpener.IsWorldNpcChatOpen || NpcProximityChatOpener.LastCloseFrame == Time.frameCount))
            return;

        if (_paused)
            StartCoroutine(ClosePauseRoutine());
        else
            StartCoroutine(OpenPauseRoutine());
    }

    static bool PauseMenuTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return GameplayInput.PausePressedThisFrame()
               || GameplayInput.GameplaySettingsMenuPressedThisFrame();
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    /// <summary>True when a TMP or legacy InputField has focus (typing), so gameplay shortcuts should not fire.</summary>
    public static bool IsFocusedOnTextInput()
    {
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (sel == null)
            return false;
        return sel.GetComponent<TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null;
    }

    static bool IsTypingInUiInput() => IsFocusedOnTextInput();

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Explicit gamepad UI routing for the pause menu. Unity's <c>InputSystemUIInputModule</c> is supposed to
    /// do this, but in this project the module's actions can end up silent (no gamepad UI control scheme is
    /// enabled, the PlayerInput on the player owns the action asset, etc.), leaving controllers dead in the
    /// pause menu. Driving navigation and submit directly off <see cref="GameplayInput"/> makes the pause
    /// menu usable with a pad regardless of the UI module state.
    /// </summary>
    void HandleGamepadPauseNavigation()
    {
        var es = EventSystem.current;
        if (es == null)
            return;

        // Keep a valid selection pinned so D-pad navigation always has somewhere to move from.
        GameObject selected = es.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy ||
            (pauseMenuRoot != null && !selected.transform.IsChildOf(pauseMenuRoot.transform)))
        {
            FocusFirstPauseMenuSelectable();
            selected = es.currentSelectedGameObject;
        }

        if (selected == null)
            return;

        var pad = GameplayInput.GetUsableGamepad();
        const float dpadSliderStep = 0.05f;
        const float rightStickScale = 2.2f;

        // Sliders: D-pad left/right and right stick adjust the value. Horizontal "move" would leave the
        // control without ever changing the track (A only fires Button — Sliders are not buttons).
        var slider = selected.GetComponent<Slider>();
        if (slider != null && slider.interactable)
        {
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame)
                    slider.value = Mathf.Clamp01(slider.value - dpadSliderStep);
                else if (pad.dpad.right.wasPressedThisFrame)
                    slider.value = Mathf.Clamp01(slider.value + dpadSliderStep);

                float rx = pad.rightStick.ReadValue().x;
                if (Mathf.Abs(rx) > 0.12f)
                    slider.value = Mathf.Clamp01(slider.value + rx * rightStickScale * Time.unscaledDeltaTime);
            }
        }

        // Length of day: D-pad ±1, triggers ±5 (Loans and other world listeners skip while paused).
        if (selected.name == "LengthOfDayInput" && pauseMenuRoot != null && pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame)
                PauseMenuSettingsHelper.NudgeLengthOfDayMinutes(pauseMenuRoot.transform, -1);
            else if (pad.dpad.right.wasPressedThisFrame)
                PauseMenuSettingsHelper.NudgeLengthOfDayMinutes(pauseMenuRoot.transform, 1);
            if (pad.leftTrigger.wasPressedThisFrame)
                PauseMenuSettingsHelper.NudgeLengthOfDayMinutes(pauseMenuRoot.transform, -5);
            if (pad.rightTrigger.wasPressedThisFrame)
                PauseMenuSettingsHelper.NudgeLengthOfDayMinutes(pauseMenuRoot.transform, 5);
        }

        Vector2Int step = GameplayInput.GamepadNavigateStepThisFrame();
        if (slider != null)
            step.x = 0;
        else if (selected.name == "LengthOfDayInput")
            step.x = 0;

        // Skip if the UI input module already moved the selection this frame in response to the same input —
        // otherwise the D-pad tap would step twice (once from the module, once from here).
        bool uiModuleAlreadyMovedSelection = selected != _prevSelectedForNavDedupe && _prevSelectedForNavDedupe != null;
        if (step.sqrMagnitude > 0 && !uiModuleAlreadyMovedSelection)
        {
            Selectable sel = selected.GetComponent<Selectable>();
            Selectable next = null;
            if (sel != null)
            {
                if (step.y > 0) next = sel.FindSelectableOnUp();
                else if (step.y < 0) next = sel.FindSelectableOnDown();
                else if (step.x > 0) next = sel.FindSelectableOnRight();
                else if (step.x < 0) next = sel.FindSelectableOnLeft();
            }

            if (next != null && next.interactable && next.isActiveAndEnabled)
            {
                es.SetSelectedGameObject(next.gameObject);
                selected = next.gameObject;
            }
        }

        _prevSelectedForNavDedupe = selected;

        if (!GameplayInput.GamepadSubmitPressedThisFrame() || selected == null)
            return;

        if (IsTypingInUiInput())
        {
            // Keep submit from firing a mis-focused button; LengthOfDay and sliders use D-pad / sticks above.
            return;
        }

        var drop = selected.GetComponent<TMP_Dropdown>();
        if (drop != null)
        {
            if (!drop.IsExpanded)
                drop.Show();
            return;
        }

        var btn = selected.GetComponent<Button>();
        if (btn != null && btn.interactable && btn.IsActive())
            btn.onClick.Invoke();
    }
#endif

    void FocusFirstPauseMenuSelectable()
    {
        if (pauseMenuRoot == null || EventSystem.current == null)
            return;

        foreach (var btn in pauseMenuRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn == null || !btn.isActiveAndEnabled)
                continue;
            string n = btn.gameObject.name;
            if (n == "BackToGame" || n == "BackToMenu")
            {
                EventSystem.current.firstSelectedGameObject = btn.gameObject;
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
                return;
            }
        }

        var fallback = pauseMenuRoot.GetComponentInChildren<Button>(true);
        if (fallback != null)
        {
            EventSystem.current.firstSelectedGameObject = fallback.gameObject;
            EventSystem.current.SetSelectedGameObject(fallback.gameObject);
        }
    }

    IEnumerator OpenPauseRoutine()
    {
        _busy = true;
        IsPaused = true;
        Time.timeScale = 0f;
        UnlockGameCursorForMenu();
        if (!_settingsBound)
        {
            PauseMenuSettingsHelper.Bind(pauseMenuRoot.transform);
            WireBackToGameButton();
            WireQuitButton();
            WireHelpButton();
            _settingsBound = true;
        }

        pauseMenuRoot.SetActive(true);
        if (_pauseCanvas != null)
            _pauseCanvas.sortingOrder = pauseCanvasSortOrder;

        _pauseGroup.alpha = 0f;
        _pauseGroup.interactable = false;
        _pauseGroup.blocksRaycasts = false;

        yield return FadeCanvasGroups(_hudGroups, 1f, 0f, hudFadeOutDuration);

        float t = 0f;
        while (t < pauseFadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            _pauseGroup.alpha = Mathf.Clamp01(t / pauseFadeInDuration);
            yield return null;
        }

        _pauseGroup.alpha = 1f;
        _pauseGroup.interactable = true;
        _pauseGroup.blocksRaycasts = true;
        _paused = true;
        _busy = false;
        FocusFirstPauseMenuSelectable();
    }

    IEnumerator ClosePauseRoutine()
    {
        _busy = true;
        _prevSelectedForNavDedupe = null;
        _pauseGroup.interactable = false;
        _pauseGroup.blocksRaycasts = false;

        float t = 0f;
        while (t < pauseFadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            _pauseGroup.alpha = Mathf.Clamp01(1f - t / pauseFadeOutDuration);
            yield return null;
        }

        _pauseGroup.alpha = 0f;
        pauseMenuRoot.SetActive(false);

        Time.timeScale = 1f;
        _paused = false;
        IsPaused = false;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        RestoreGameCursorAfterMenu();

        yield return FadeCanvasGroups(_hudGroups, 0f, 1f, hudFadeInDuration);

        _busy = false;
    }

    static void UnlockGameCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#if ENABLE_INPUT_SYSTEM
        foreach (var sai in Object.FindObjectsByType<StarterAssets.StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(true);
#endif
    }

    static void RestoreGameCursorAfterMenu()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (var sai in Object.FindObjectsByType<StarterAssets.StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(false);
#endif
    }

    IEnumerator OpenHelpOverlayRoutine()
    {
        _busy = true;
        _helpOverlayActive = true;

        if (_pauseGroup != null)
        {
            _pauseGroup.interactable = false;
            _pauseGroup.blocksRaycasts = false;

            float t = 0f;
            while (t < pauseFadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                _pauseGroup.alpha = Mathf.Clamp01(1f - t / pauseFadeOutDuration);
                yield return null;
            }
            _pauseGroup.alpha = 0f;
        }

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        HelpQaOverlayController.ReturnToPauseOnClose = true;

        SuspendWorldSceneSingletons();

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        var op = SceneManager.LoadSceneAsync(helpOverlaySceneName, LoadSceneMode.Additive);
        if (op != null)
        {
            while (!op.isDone)
                yield return null;
        }

        // If the additively-loaded help scene doesn't provide its own EventSystem or
        // AudioListener, revive one from the world so we don't end up with zero.
        HelpQaOverlayLoadUtility.EnsureAtLeastOneActiveEventSystem();
        HelpQaOverlayLoadUtility.EnsureAtLeastOneActiveAudioListener();

        _busy = false;
    }

    /// <summary>
    /// Disable every currently-active EventSystem and AudioListener in all loaded
    /// scenes before additively loading the help overlay, so the incoming scene's
    /// copies become the sole active singletons. Avoids the "more than one"
    /// warnings Unity logs on OnEnable/Update when duplicates coexist.
    /// </summary>
    void SuspendWorldSceneSingletons()
    {
        HelpQaOverlayLoadUtility.SuspendEventSystemsAndAudioListeners();
    }

    void RestoreWorldSceneSingletons()
    {
        HelpQaOverlayLoadUtility.RestoreSuspendedSingletons();
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (!_helpOverlayActive || scene.name != helpOverlaySceneName)
            return;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        RestoreWorldSceneSingletons();
        StartCoroutine(ReopenPauseAfterHelpRoutine());
    }

    IEnumerator ReopenPauseAfterHelpRoutine()
    {
        _busy = true;

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);
            if (_pauseCanvas != null)
                _pauseCanvas.sortingOrder = pauseCanvasSortOrder;
        }

        if (_pauseGroup != null)
        {
            _pauseGroup.alpha = 0f;
            _pauseGroup.interactable = false;
            _pauseGroup.blocksRaycasts = false;

            float t = 0f;
            while (t < pauseFadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                _pauseGroup.alpha = Mathf.Clamp01(t / pauseFadeInDuration);
                yield return null;
            }

            _pauseGroup.alpha = 1f;
            _pauseGroup.interactable = true;
            _pauseGroup.blocksRaycasts = true;
        }

        UnlockGameCursorForMenu();
        _helpOverlayActive = false;
        _busy = false;
        FocusFirstPauseMenuSelectable();
    }

    static IEnumerator FadeCanvasGroups(List<CanvasGroup> groups, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            foreach (var cg in groups)
            {
                if (cg != null)
                    cg.alpha = a;
            }

            yield return null;
        }

        foreach (var cg in groups)
        {
            if (cg != null)
                cg.alpha = to;
        }
    }
}
