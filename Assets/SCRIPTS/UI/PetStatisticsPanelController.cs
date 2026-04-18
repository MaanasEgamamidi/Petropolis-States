using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Drives the Pet Statistics screen-space canvas: keep <see cref="petStatisticsRoot"/> active in the scene for
/// editing; on play, Awake hides it briefly before <see cref="Update"/> applies pet-confirmed visibility and slides
/// <see cref="leftBox"/> via <see cref="PetInfoSettings"/> and Q (hold / toggle / always open).
/// </summary>
public class PetStatisticsPanelController : MonoBehaviour
{
    [SerializeField] GameObject petStatisticsRoot;
    [SerializeField] RectTransform leftBox;

    [Tooltip("Fades the drag-handle bar and \"Pet Statistics\" label with the panel: full when collapsed (left), invisible when open (right). Auto-added under Left Box / Drag Handle if unset.")]
    [SerializeField] CanvasGroup dragHandleChrome;

    [Tooltip("Inverse of drag handle: invisible when collapsed (left), full opacity when open (right). Auto-added under Left Box / Stats if unset. Covers all Stats descendants (bars, label rows, FeelingText), so they all fade in together when sliding right and out together when sliding left.")]
    [SerializeField] CanvasGroup statsChrome;

    [Tooltip("Anchored X when closed (toggle off / not holding). Match Left Box X in the scene so edit mode and play mode look the same.")]
    [SerializeField] float closedAnchoredX = 0f;

    [Tooltip("Anchored X when the panel is fully shown.")]
    [SerializeField] float openAnchoredX = 560f;

    [Tooltip("How fast the panel moves between closed and open (units per second, anchored space).")]
    [SerializeField] [Min(1f)] float slideSpeed = 2000f;

    [Tooltip("Hold-to-show only: brief automatic open after first pet choice this session.")]
    [SerializeField] [Min(0f)] float holdModeIntroPeekDuration = 2f;

    [Tooltip("\"Pet Statistics\" / drag handle opacity = (1−t)^n where t is slide progress (0 = closed left, 1 = open right). n > 1: fades out quickly at first when opening, then slowly at the end; when closing (right→left) the opposite.")]
    [SerializeField] [Min(1f)] float dragHandleFadeExponent = 3f;

    [Tooltip("Health / happiness / energy / hunger chrome opacity = t^n. n > 1: stays faint for most of the slide when opening, then ramps up quickly at the end; when closing, fades most of the way early and eases out at the end.")]
    [SerializeField] [Min(1f)] float statsFadeExponent = 3f;

    PetInfoDisplayMode _lastMode;
    bool _toggleExpanded;
    float _holdIntroPeekRemaining;
    float _currentAnchoredX;

    static bool s_PostPetChoiceIntroDone;

    void Awake()
    {
        if (leftBox != null)
            _currentAnchoredX = leftBox.anchoredPosition.x;
        EnsureDragHandleChrome();
        if (Application.isPlaying && petStatisticsRoot != null)
            petStatisticsRoot.SetActive(false);
    }

    void EnsureDragHandleChrome()
    {
        if (leftBox == null)
            return;

        if (dragHandleChrome == null)
        {
            Transform handle = leftBox.Find("Drag Handle");
            if (handle != null)
            {
                dragHandleChrome = handle.GetComponent<CanvasGroup>();
                if (dragHandleChrome == null)
                    dragHandleChrome = handle.gameObject.AddComponent<CanvasGroup>();
                dragHandleChrome.blocksRaycasts = true;
                dragHandleChrome.interactable = true;
            }
        }

        if (statsChrome == null)
        {
            Transform stats = leftBox.Find("Stats");
            if (stats != null)
            {
                statsChrome = stats.GetComponent<CanvasGroup>();
                if (statsChrome == null)
                    statsChrome = stats.gameObject.AddComponent<CanvasGroup>();
                statsChrome.blocksRaycasts = true;
                statsChrome.interactable = true;
            }
        }
    }

    void ApplyDragHandleFade()
    {
        if (leftBox == null)
            return;
        float x = leftBox.anchoredPosition.x;
        float t = Mathf.Clamp01(Mathf.InverseLerp(closedAnchoredX, openAnchoredX, x));

        if (dragHandleChrome != null)
            dragHandleChrome.alpha = Mathf.Pow(1f - t, dragHandleFadeExponent);

        if (statsChrome != null)
            statsChrome.alpha = Mathf.Pow(t, statsFadeExponent);
    }

    void OnEnable()
    {
        _lastMode = PetInfoSettings.Mode;
        if (_lastMode == PetInfoDisplayMode.Toggle)
            _toggleExpanded = false;
    }

    void Update()
    {
        if (!Application.isPlaying || petStatisticsRoot == null || leftBox == null)
            return;

        bool petChosen = GlobalData.StoredPetSkin > 0;
        if (!petChosen)
        {
            s_PostPetChoiceIntroDone = false;
            _holdIntroPeekRemaining = 0f;
            _toggleExpanded = false;
            petStatisticsRoot.SetActive(false);
            return;
        }

        petStatisticsRoot.SetActive(true);

        PetInfoDisplayMode mode = PetInfoSettings.Mode;
        if (mode != _lastMode)
        {
            _lastMode = mode;
            if (mode == PetInfoDisplayMode.Toggle)
                _toggleExpanded = false;
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
            _toggleExpanded = !_toggleExpanded;

        bool wantOpen = _holdIntroPeekRemaining > 0f || ComputeOpenForMode(mode, qHeld);
        if (NpcProximityChatOpener.IsWorldNpcChatOpen)
            wantOpen = false;
        float targetX = wantOpen ? openAnchoredX : closedAnchoredX;
        float step = slideSpeed * Time.deltaTime;
        _currentAnchoredX = Mathf.MoveTowards(_currentAnchoredX, targetX, step);

        Vector2 p = leftBox.anchoredPosition;
        p.x = _currentAnchoredX;
        leftBox.anchoredPosition = p;

        ApplyDragHandleFade();
    }

    void ApplyFirstPetChoiceIntro(PetInfoDisplayMode mode)
    {
        switch (mode)
        {
            case PetInfoDisplayMode.Toggle:
                _toggleExpanded = true;
                _currentAnchoredX = openAnchoredX;
                break;
            case PetInfoDisplayMode.HoldToShow:
                if (holdModeIntroPeekDuration > 0f)
                    _holdIntroPeekRemaining = holdModeIntroPeekDuration;
                _currentAnchoredX = openAnchoredX;
                break;
            case PetInfoDisplayMode.AlwaysOn:
            default:
                _currentAnchoredX = openAnchoredX;
                break;
        }
    }

    bool ComputeOpenForMode(PetInfoDisplayMode mode, bool qHeld)
    {
        switch (mode)
        {
            case PetInfoDisplayMode.AlwaysOn:
                return true;
            case PetInfoDisplayMode.Toggle:
                return _toggleExpanded;
            case PetInfoDisplayMode.HoldToShow:
                return qHeld;
            default:
                return true;
        }
    }
}
