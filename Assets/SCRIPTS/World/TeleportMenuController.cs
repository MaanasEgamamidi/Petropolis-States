using System.Collections.Generic;
using System.Text;
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
/// World: press <b>T</b> to toggle a compact grid menu listing every child of
/// <see cref="teleportLocationsRoot"/> as a button. Clicking a button teleports the player to
/// that child's world position and deducts <see cref="teleportCost"/> from
/// <see cref="GlobalData.PlayerMoney"/>. Buttons are disabled (dimmed) when the player can't
/// afford the trip. The UI is built at runtime with a light (white) theme that matches the
/// inventory panel — just drop this component on any GameObject (typically the
/// <c>TeleportLocations</c> parent itself) and the menu wires itself up.
/// </summary>
public class TeleportMenuController : MonoBehaviour
{
    const string WorldSceneName = "World";
    const string DefaultTeleportRootName = "TeleportLocations";

    const string FontRegularName = "AlteHaasGroteskRegular SDF";
    const string FontBoldName = "AlteHaasGroteskBold SDF";

    [Header("Setup")]
    [Tooltip("Parent whose direct children are teleport destinations. When null, searches for a root GameObject named 'TeleportLocations'. Defaults to this transform if this component is placed on the parent itself.")]
    [SerializeField] Transform teleportLocationsRoot;

    [Tooltip("Tag used to identify the player root for teleporting.")]
    [SerializeField] string playerTag = "Player";

    [Header("Cost")]
    [Tooltip("Amount subtracted from GlobalData.PlayerMoney each time a teleport button is clicked.")]
    [Min(0f)]
    [SerializeField] float teleportCost = 25f;

    [Header("Labels")]
    [Tooltip("When true, strips a trailing 'Teleport' suffix from each destination's GameObject name and inserts spaces into CamelCase so 'PetShopTeleport' becomes 'Pet Shop'.")]
    [SerializeField] bool prettifyButtonLabels = true;

    [Header("Layout")]
    [Tooltip("Number of buttons per row in the grid.")]
    [Min(1)]
    [SerializeField] int columns = 2;
    [Tooltip("Pixel width of each button cell.")]
    [SerializeField] float cellWidth = 200f;
    [Tooltip("Pixel height of each button cell.")]
    [SerializeField] float cellHeight = 52f;
    [Tooltip("Horizontal + vertical gap between cells.")]
    [SerializeField] Vector2 cellSpacing = new Vector2(10f, 10f);

    [Header("Fonts (optional)")]
    [Tooltip("Regular-weight TMP font used for button labels. Leave null to auto-locate AlteHaasGroteskRegular SDF at runtime and fall back to the TMP default.")]
    [SerializeField] TMP_FontAsset fontRegular;
    [Tooltip("Bold-weight TMP font (currently unused by the button labels, reserved for future title/header styling).")]
    [SerializeField] TMP_FontAsset fontBold;

    // Light theme palette — intentionally not serialized so stale dark values in existing scene
    // YAML (from the old blue theme) are orphaned and these defaults take effect automatically.
    static readonly Color PanelBgColor = new Color(0.98f, 0.98f, 0.99f, 1f);
    static readonly Color ButtonBgColor = new Color(0.94f, 0.95f, 0.97f, 1f);
    static readonly Color ButtonHoverColor = new Color(0.88f, 0.91f, 0.96f, 1f);
    static readonly Color ButtonPressColor = new Color(0.82f, 0.86f, 0.92f, 1f);
    static readonly Color ButtonSelectedColor = new Color(0.88f, 0.91f, 0.96f, 1f);
    static readonly Color ButtonBgDisabledColor = new Color(0.92f, 0.93f, 0.95f, 1f);
    static readonly Color LabelColor = new Color(0.12f, 0.13f, 0.16f, 1f);
    static readonly Color LabelDisabledColor = new Color(0.55f, 0.57f, 0.62f, 1f);

    /// <summary>True while the teleport menu is on screen. Other systems may check this to gate input.</summary>
    public static bool IsOpen { get; private set; }

    readonly List<Button> _buttons = new List<Button>();
    readonly List<Image> _buttonImages = new List<Image>();
    readonly List<TextMeshProUGUI> _buttonLabels = new List<TextMeshProUGUI>();
    readonly List<Transform> _targets = new List<Transform>();

    GameObject _canvasGo;
    GameObject _panelGo;
    Canvas _canvas;
    CanvasGroup _panelGroup;

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

        ResolveFonts();
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

    void ResolveFonts()
    {
        if (fontRegular == null) fontRegular = FindFontByName(FontRegularName);
        if (fontBold == null) fontBold = FindFontByName(FontBoldName);
        if (fontRegular == null) fontRegular = TMP_Settings.defaultFontAsset;
        if (fontBold == null) fontBold = fontRegular;
    }

    static TMP_FontAsset FindFontByName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;
        TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < all.Length; i++)
        {
            TMP_FontAsset f = all[i];
            if (f != null && f.name == assetName)
                return f;
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

            if (i < _buttonImages.Count && _buttonImages[i] != null)
                _buttonImages[i].color = canAfford ? ButtonBgColor : ButtonBgDisabledColor;
            if (i < _buttonLabels.Count && _buttonLabels[i] != null)
                _buttonLabels[i].color = canAfford ? LabelColor : LabelDisabledColor;
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
        // Shift the tagged root by the offset needed so the CharacterController ends at the marker.
        // Setting root.position = target fails when CC is on a child: capsule and minimap/camera follow
        // different points than the root transform.
        Vector3 targetPos = target.position;
        CharacterController cc = playerRoot.GetComponentInChildren<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerRoot.position += targetPos - cc.transform.position;
            cc.enabled = true;
        }
        else
        {
            playerRoot.position = targetPos;
        }

        Physics.SyncTransforms();
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
        _buttonImages.Clear();
        _buttonLabels.Clear();

        _canvasGo = new GameObject($"{nameof(TeleportMenuController)} Canvas");
        // Disable immediately so no UI, raycaster, or full-screen image can steal clicks
        // (or interfere with the player camera cursor lock) before SetMenuVisible(false) runs.
        _canvasGo.SetActive(false);
        _canvas = _canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 350;
        CanvasScaler scaler = _canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasGo.AddComponent<GraphicRaycaster>();

        _panelGo = new GameObject("Panel");
        _panelGo.transform.SetParent(_canvasGo.transform, false);
        RectTransform panelRect = _panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        Image panelImage = _panelGo.AddComponent<Image>();
        panelImage.color = PanelBgColor;
        panelImage.raycastTarget = true;
        _panelGroup = _panelGo.AddComponent<CanvasGroup>();

        const int padX = 18;
        const int padY = 18;
        const float titleHeight = 58f;
        const float titleGridGap = 12f;

        int cols = Mathf.Max(1, columns);
        int childCount = teleportLocationsRoot != null ? teleportLocationsRoot.childCount : 0;
        int rows = Mathf.CeilToInt(childCount / (float)cols);
        float contentWidth = cols * cellWidth + (cols - 1) * cellSpacing.x;
        float gridHeight = rows * cellHeight + Mathf.Max(0, rows - 1) * cellSpacing.y;
        float titleBlock = titleHeight + titleGridGap;
        float panelWidth = contentWidth + padX * 2;
        float panelHeight = padY + titleBlock + gridHeight + padY;
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(_panelGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -padY);
        titleRect.sizeDelta = new Vector2(contentWidth, titleHeight);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        if (fontBold != null) titleTmp.font = fontBold;
        else if (fontRegular != null) titleTmp.font = fontRegular;
        titleTmp.text = $"Choose a destination below.\nEach trip teleports you there for ${teleportCost:0}.";
        titleTmp.fontSize = 18f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = LabelColor;
        titleTmp.textWrappingMode = TextWrappingModes.Normal;
        titleTmp.overflowMode = TextOverflowModes.Overflow;
        titleTmp.raycastTarget = false;

        GameObject gridGo = new GameObject("Grid");
        gridGo.transform.SetParent(_panelGo.transform, false);
        RectTransform gridRect = gridGo.AddComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = new Vector2(0f, -(padY + titleBlock));
        gridRect.sizeDelta = new Vector2(contentWidth, gridHeight);

        GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.spacing = cellSpacing;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;

        if (teleportLocationsRoot != null)
        {
            int count = teleportLocationsRoot.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = teleportLocationsRoot.GetChild(i);
                if (child == null) continue;
                _targets.Add(child);
                int capturedIndex = _targets.Count - 1;
                string label = prettifyButtonLabels ? PrettifyLocationName(child.name) : child.name;
                BuildButton(gridGo.transform, label, capturedIndex);
            }
        }
    }

    void BuildButton(Transform parent, string label, int index)
    {
        GameObject go = new GameObject($"Button - {label}");
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = ButtonBgColor;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = MultiplyToward(ButtonBgColor, ButtonHoverColor);
        cb.pressedColor = MultiplyToward(ButtonBgColor, ButtonPressColor);
        cb.selectedColor = MultiplyToward(ButtonBgColor, ButtonSelectedColor);
        cb.disabledColor = new Color(1f, 1f, 1f, 0.85f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.onClick.AddListener(() => OnTeleportButtonClicked(index));

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (fontRegular != null) tmp.font = fontRegular;
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = LabelColor;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;

        _buttons.Add(btn);
        _buttonImages.Add(img);
        _buttonLabels.Add(tmp);
    }

    /// <summary>
    /// Per-state color is multiplied by <see cref="Button.targetGraphic"/>.color at runtime, so to get
    /// an exact target look from a known base we derive a multiplier that (component-wise) converts
    /// <paramref name="baseColor"/> into <paramref name="targetColor"/> — falling back to white for
    /// zero-valued base channels.
    /// </summary>
    static Color MultiplyToward(Color baseColor, Color targetColor)
    {
        float r = baseColor.r > 0.001f ? targetColor.r / baseColor.r : 1f;
        float g = baseColor.g > 0.001f ? targetColor.g / baseColor.g : 1f;
        float b = baseColor.b > 0.001f ? targetColor.b / baseColor.b : 1f;
        float a = baseColor.a > 0.001f ? targetColor.a / baseColor.a : 1f;
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Turns a raw GameObject name like "PetShopTeleport" or "BankTeleport" into a human-friendly
    /// label ("Pet Shop", "Bank"): drops a trailing "Teleport" suffix and inserts spaces at
    /// CamelCase / letter-to-digit boundaries. Names that already contain spaces are left alone.
    /// </summary>
    static string PrettifyLocationName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        string s = raw.Trim();
        const string suffix = "Teleport";
        if (s.Length > suffix.Length && s.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
            s = s.Substring(0, s.Length - suffix.Length).TrimEnd();

        if (s.IndexOf(' ') >= 0) return s;

        StringBuilder sb = new StringBuilder(s.Length + 4);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (i > 0)
            {
                char prev = s[i - 1];
                bool boundary =
                    (char.IsUpper(c) && (char.IsLower(prev) || char.IsDigit(prev))) ||
                    (char.IsDigit(c) != char.IsDigit(prev) && prev != ' ') ||
                    (i + 1 < s.Length && char.IsUpper(prev) && char.IsUpper(c) && char.IsLower(s[i + 1]));
                if (boundary) sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
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
