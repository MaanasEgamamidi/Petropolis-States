using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shows/hides the PetStats UI in the World scene per PetInfoSettings (Q toggle / hold).
/// Keeps this GameObject active and uses CanvasGroup alpha so input still runs when hidden.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PetStatsPanelController : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Seconds to fade pet stats in/out (same timing as minimap). Default 0.1s.")]
    [SerializeField] [Min(0.01f)] float fadeDuration = 0.1f;

    CanvasGroup _canvasGroup;
    PetInfoDisplayMode _lastMode;
    bool _toggleOpen;
    float _presentationAlpha;

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

        PetInfoDisplayMode mode = PetInfoSettings.Mode;
        if (mode != _lastMode)
        {
            _lastMode = mode;
            if (mode == PetInfoDisplayMode.Toggle)
                _toggleOpen = false;
        }

#if ENABLE_INPUT_SYSTEM
        bool qPressed = Keyboard.current != null && Keyboard.current[Key.Q].wasPressedThisFrame;
        bool qHeld = Keyboard.current != null && Keyboard.current[Key.Q].isPressed;
#else
        bool qPressed = Input.GetKeyDown(KeyCode.Q);
        bool qHeld = Input.GetKey(KeyCode.Q);
#endif

        if (mode == PetInfoDisplayMode.Toggle && qPressed)
            _toggleOpen = !_toggleOpen;

        float target = ComputeShownForMode(mode, qHeld) ? 1f : 0f;
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
