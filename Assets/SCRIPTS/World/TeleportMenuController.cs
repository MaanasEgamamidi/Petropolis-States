using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// World: press <b>T</b> to toggle a menu listing every child of <see cref="teleportLocationsRoot"/>
/// as a button. Clicking a button teleports the player to that child's world position/rotation
/// and deducts <see cref="teleportCost"/> from <see cref="GlobalData.PlayerMoney"/>. Buttons are
/// disabled (dimmed) when the player can't afford the trip. The UI is built at runtime — just
/// drop this component on any GameObject (typically the <c>TeleportLocations</c> parent itself)
/// and the menu wires itself up.
/// </summary>
public class TeleportMenuController : MonoBehaviour
{
    const string WorldSceneName = "World";
    const string DefaultTeleportRootName = "TeleportLocations";

    [Header("Setup")]
    [Tooltip("Parent whose direct children are teleport destinations. When null, searches for a root GameObject named 'TeleportLocations'. Defaults to this transform if this component is placed on the parent itself.")]
    [SerializeField] Transform teleportLocationsRoot;

    [Tooltip("Tag used to identify the player root for teleporting.")]
    [SerializeField] string playerTag = "Player";

    [Header("Cost")]
    [Tooltip("Amount subtracted from GlobalData.PlayerMoney each time a teleport button is clicked.")]
    [Min(0f)]
    [SerializeField] float teleportCost = 25f;

    [Header("UI styling")]
    [SerializeField] Color panelColor = new Color(0.09f, 0.1f, 0.13f, 0.94f);
    [SerializeField] Color buttonColor = new Color(0.22f, 0.27f, 0.36f, 1f);
    [SerializeField] Color buttonDisabledColor = new Color(0.22f, 0.27f, 0.36f, 0.45f);
    [SerializeField] Color titleColor = Color.white;
    [SerializeField] Color buttonTextColor = Color.white;

    /// <summary>True while the teleport menu is on screen. Other systems may check this to gate input.</summary>
    public static bool IsOpen { get; private set; }

    readonly List<Button> _buttons = new List<Button>();
    readonly List<Transform> _targets = new List<Transform>();

    GameObject _canvasGo;
    GameObject _panelGo;
    Canvas _canvas;
    CanvasGroup _panelGroup;
    TMP_FontAsset _font;

    void Awake()
    {
        if (teleportLocationsRoot == null)
        {
            Transform t = transform;
            if (t.childCount > 0 && string.Equals(t.name, DefaultTeleportRootName, System.StringComparison.OrdinalIgnoreCase))
                teleportLocationsRoot = t;
        }

        if (teleportLocationsRoot == null)
            teleportLocationsRoot = FindRootByNameInActiveScene(DefaultTeleportRootName);

        _font = TMP_Settings.defaultFontAsset;
        BuildUi();
        SetMenuVisible(false);
    }

    void OnEnable()
    {
        GlobalData.OnMoneyChanged += HandleMoneyChanged;
    }

    void OnDisable()
    {
        GlobalData.OnMoneyChanged -= HandleMoneyChanged;
        if (IsOpen)
        {
            RestoreCursorAfterMenu();
            IsOpen = false;
        }
    }

    void OnDestroy()
    {
        if (_canvasGo != null)
            Destroy(_canvasGo);
    }

    static Transform FindRootByNameInActiveScene(string rootName)
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid())
            return null;

        foreach (GameObject root in active.GetRootGameObjects())
        {
            if (root != null && root.name == rootName)
                return root.transform;
        }
        return null;
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (SceneManager.GetActiveScene().name != WorldSceneName) return;
        if (WorldPauseController.IsPaused) return;

        if (NpcProximityChatOpener.IsWorldNpcChatOpen || InventoryPanelController.IsOpen)
        {
            if (IsOpen) Close();
            return;
        }

        if (IsTypingInFocusedInput()) return;

        if (IsOpen && CloseKeyPressedThisFrame())
        {
            Close();
            return;
        }

        if (!TogglePressedThisFrame()) return;

        if (IsOpen) Close();
        else Open();
    }

    void LateUpdate()
    {
        if (IsOpen)
            UnlockCursorForMenu();
    }

    // ─── Open / close ────────────────────────────────────────────────────────

    void Open()
    {
        if (teleportLocationsRoot == null || _targets.Count == 0)
        {
            Debug.LogWarning($"[{nameof(TeleportMenuController)}] No teleport locations found; cannot open menu.");
            return;
        }

        RefreshAffordability();
        SetMenuVisible(true);
        UnlockCursorForMenu();
        IsOpen = true;

        if (EventSystem.current != null && _buttons.Count > 0)
        {
            Button first = FindFirstInteractableButton();
            if (first != null)
            {
                EventSystem.current.firstSelectedGameObject = first.gameObject;
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }
    }

    void Close()
    {
        SetMenuVisible(false);
        RestoreCursorAfterMenu();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        IsOpen = false;
    }

    Button FindFirstInteractableButton()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null && _buttons[i].interactable)
                return _buttons[i];
        }
        return _buttons.Count > 0 ? _buttons[0] : null;
    }

    void HandleMoneyChanged(float _)
    {
        if (IsOpen)
            RefreshAffordability();
    }

    void RefreshAffordability()
    {
        bool canAfford = GlobalData.PlayerMoney >= teleportCost;
        for (int i = 0; i < _buttons.Count; i++)
        {
            Button b = _buttons[i];
            if (b == null) continue;
            b.interactable = canAfford;

            Image img = b.GetComponent<Image>();
            if (img != null)
                img.color = canAfford ? buttonColor : buttonDisabledColor;
        }
    }

    // ─── Teleport ────────────────────────────────────────────────────────────

    void OnTeleportButtonClicked(int index)
    {
        if (index < 0 || index >= _targets.Count) return;
        Transform target = _targets[index];
        if (target == null) return;

        if (GlobalData.PlayerMoney < teleportCost)
        {
            RefreshAffordability();
            return;
        }

        GameObject playerRoot = GameObject.FindGameObjectWithTag(playerTag);
        if (playerRoot == null)
        {
            Debug.LogWarning($"[{nameof(TeleportMenuController)}] Could not find an object tagged '{playerTag}' to teleport.");
            return;
        }

        TeleportPlayerTo(playerRoot.transform, target);
        GlobalData.PlayerMoney -= teleportCost;
        Close();
    }

    static void TeleportPlayerTo(Transform playerRoot, Transform target)
    {
        CharacterController cc = playerRoot.GetComponentInChildren<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.transform.SetPositionAndRotation(target.position, target.rotation);
            cc.enabled = true;
        }
        else
        {
            playerRoot.SetPositionAndRotation(target.position, target.rotation);
        }
    }

    // ─── Input helpers ───────────────────────────────────────────────────────

    static bool TogglePressedThisFrame()
    {
        return GameplayInput.TeleportMenuTogglePressedThisFrame();
    }

    static bool CloseKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    static bool IsTypingInFocusedInput()
    {
        if (EventSystem.current == null) return false;
        GameObject sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return false;
        TMP_InputField field = sel.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }

    static void UnlockCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#if ENABLE_INPUT_SYSTEM
        foreach (StarterAssetsInputs sai in Object.FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(true);
#endif
    }

    static void RestoreCursorAfterMenu()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (StarterAssetsInputs sai in Object.FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(false);
#endif
    }

    // ─── Runtime UI build ────────────────────────────────────────────────────

    void BuildUi()
    {
        _targets.Clear();
        _buttons.Clear();

        _canvasGo = new GameObject($"{nameof(TeleportMenuController)} Canvas");
        _canvas = _canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 350;
        _canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScaler scaler = _canvasGo.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasGo.AddComponent<GraphicRaycaster>();

        _panelGo = new GameObject("Panel");
        _panelGo.transform.SetParent(_canvasGo.transform, false);
        RectTransform panelRect = _panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 40f);
        Image panelImage = _panelGo.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = true;
        _panelGroup = _panelGo.AddComponent<CanvasGroup>();

        VerticalLayoutGroup vlg = _panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 24, 24);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter fitter = _panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildTitleLabel(_panelGo.transform);

        if (teleportLocationsRoot != null)
        {
            int count = teleportLocationsRoot.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = teleportLocationsRoot.GetChild(i);
                if (child == null) continue;
                _targets.Add(child);
                int capturedIndex = _targets.Count - 1;
                Button btn = BuildButton(_panelGo.transform, child.name, capturedIndex);
                _buttons.Add(btn);
            }
        }

        BuildHintLabel(_panelGo.transform);
    }

    void BuildTitleLabel(Transform parent)
    {
        GameObject go = new GameObject("Title");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = $"Teleport  (−${teleportCost:0})";
        tmp.fontSize = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = titleColor;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
    }

    void BuildHintLabel(Transform parent)
    {
        GameObject go = new GameObject("Hint");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = "Press T or Esc to close";
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.65f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 32f;
    }

    Button BuildButton(Transform parent, string label, int index)
    {
        GameObject go = new GameObject($"Button - {label}");
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = buttonColor;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.12f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
        cb.selectedColor = new Color(1.15f, 1.15f, 1.2f, 1f);
        cb.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;
        btn.onClick.AddListener(() => OnTeleportButtonClicked(index));

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        le.minHeight = 48f;

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 6f);
        textRect.offsetMax = new Vector2(-16f, -6f);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = label;
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = buttonTextColor;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return btn;
    }

    void SetMenuVisible(bool visible)
    {
        if (_canvasGo != null)
            _canvasGo.SetActive(visible);
        if (_panelGroup != null)
        {
            _panelGroup.alpha = visible ? 1f : 0f;
            _panelGroup.interactable = visible;
            _panelGroup.blocksRaycasts = visible;
        }
    }
}
