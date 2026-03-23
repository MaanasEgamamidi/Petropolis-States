using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shows/hides the PetStats UI in the World scene per PetInfoSettings (Q toggle / hold).
/// Hidden until a pet is confirmed (<see cref="GlobalData.SavePetChoice"/>); Q does nothing before that.
/// After the first pet choice this run, shows once (toggle opens / hold mode peek), then settings apply normally.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PetStatsPanelController : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Seconds to fade pet stats in/out (same timing as minimap). Default 0.1s.")]
    [SerializeField] [Min(0.01f)] float fadeDuration = 0.1f;

    [Header("After first pet choice")]
    [Tooltip("Hold-to-show only: brief automatic reveal so stats are seen once before hold is required.")]
    [SerializeField] [Min(0f)] float holdModeIntroPeekDuration = 2f;

    CanvasGroup _canvasGroup;
    PetInfoDisplayMode _lastMode;
    bool _toggleOpen;
    float _presentationAlpha;
    float _holdIntroPeekRemaining;

    /// <summary>True after we applied the one-time post-choice presentation for this play session.</summary>
    static bool s_PostPetChoiceIntroDone;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        _lastMode = PetInfoSettings.Mode;
        if (_lastMode == PetInfoDisplayMode.Toggle)
            _toggleOpen = false;

        _presentationAlpha = 0f;
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        bool petChosen = GlobalData.StoredPetSkin > 0;
        if (!petChosen)
        {
            s_PostPetChoiceIntroDone = false;
            _holdIntroPeekRemaining = 0f;
            _toggleOpen = false;
            FadeTowardsShown(false);
            return;
        }

        PetInfoDisplayMode mode = PetInfoSettings.Mode;
        if (mode != _lastMode)
        {
            _lastMode = mode;
            if (mode == PetInfoDisplayMode.Toggle)
                _toggleOpen = false;
        }

        if (!s_PostPetChoiceIntroDone)
        {
            ApplyFirstPetChoiceIntro(mode);
            s_PostPetChoiceIntroDone = true;
        }

        if (_holdIntroPeekRemaining > 0f)
            _holdIntroPeekRemaining -= Time.deltaTime;

#if ENABLE_INPUT_SYSTEM
        bool qPressed = Keyboard.current != null && Keyboard.current[Key.Q].wasPressedThisFrame;
        bool qHeld = Keyboard.current != null && Keyboard.current[Key.Q].isPressed;
#else
        bool qPressed = Input.GetKeyDown(KeyCode.Q);
        bool qHeld = Input.GetKey(KeyCode.Q);
#endif

        if (mode == PetInfoDisplayMode.Toggle && qPressed)
            _toggleOpen = !_toggleOpen;

        bool wantShown = _holdIntroPeekRemaining > 0f || ComputeShownForMode(mode, qHeld);
        FadeTowardsShown(wantShown);
    }

    void ApplyFirstPetChoiceIntro(PetInfoDisplayMode mode)
    {
        switch (mode)
        {
            case PetInfoDisplayMode.Toggle:
                _toggleOpen = true;
                break;
            case PetInfoDisplayMode.HoldToShow:
                if (holdModeIntroPeekDuration > 0f)
                    _holdIntroPeekRemaining = holdModeIntroPeekDuration;
                break;
            case PetInfoDisplayMode.AlwaysOn:
            default:
                break;
        }
    }

    void FadeTowardsShown(bool shown)
    {
        float target = shown ? 1f : 0f;
        float step = Time.deltaTime / Mathf.Max(0.01f, fadeDuration);
        _presentationAlpha = Mathf.MoveTowards(_presentationAlpha, target, step);
        _canvasGroup.alpha = _presentationAlpha;
        bool canInteract = _presentationAlpha > 0.01f;
        _canvasGroup.blocksRaycasts = canInteract;
        _canvasGroup.interactable = canInteract;
    }

    bool ComputeShownForMode(PetInfoDisplayMode mode, bool qHeld)
    {
        switch (mode)
        {
            case PetInfoDisplayMode.AlwaysOn:
                return true;
            case PetInfoDisplayMode.Toggle:
                return _toggleOpen;
            case PetInfoDisplayMode.HoldToShow:
                return qHeld;
            default:
                return true;
        }
    }
}
