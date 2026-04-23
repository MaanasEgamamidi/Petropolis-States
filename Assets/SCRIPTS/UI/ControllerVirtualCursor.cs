#if ENABLE_INPUT_SYSTEM
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Software cursor driven by the left stick. Does NOT touch the OS mouse cursor: the player can use mouse and gamepad
/// at the same time. A press dispatches real UGUI pointer events at the cursor position so any Selectable
/// (Button, Toggle, Slider, Dropdown, InputField, custom IPointerClickHandler, etc.) activates exactly like a real click.
/// Hover swaps to a second sprite when the pointer is over an interactable.
/// The sprite pointer stays hidden until the first gamepad UI input in each menu context, then hides again after controller idle.
/// Mouse/keyboard do not wake the sprite or reset controller idle; they only briefly allow the OS cursor to show.
/// Active when a gamepad is connected and the scene is a menu, or in World while paused / NPC chat is open.
/// PetChoose can be loaded additively while World stays the <see cref="SceneManager.GetActiveScene"/> — that case is detected explicitly.
/// </summary>
[DefaultExecutionOrder(-400)]
public sealed class ControllerVirtualCursor : MonoBehaviour
{
    public static ControllerVirtualCursor Instance { get; private set; }

    const string PetChooseSceneName = "PetChoose";
    const int DefaultOverlaySortOrder = 32767;

    const string ResourcesDefaultCursor = "Sprites/cursor";
    const string ResourcesHoverCursor = "Sprites/cursor_hover";

    [Header("Sprites (auto-loaded from Resources/Sprites if unset)")]
    [SerializeField] Sprite defaultCursorSprite;
    [SerializeField] Sprite hoverCursorSprite;

    [Header("Movement")]
    [SerializeField] float pointerSpeedPixelsPerSecond = 1500f;
    [SerializeField] float stickDeadZone = 0.15f;

    [Header("Visual")]
    [Tooltip("Width of the cursor graphic in (reference 1080p) pixels. Height is computed from the sprite's aspect.")]
    [SerializeField] float cursorWidth = 48f;
    [Tooltip("Hide the on-screen pointer after this many seconds without left-stick input or confirm (A). Uses unscaled time.")]
    [SerializeField] float hideCursorAfterIdleSeconds = 2f;

    Canvas _canvas;
    RectTransform _canvasRect;
    RectTransform _cursorRect;
    Image _cursorImage;

    Vector2 _screenPosition;
    readonly List<RaycastResult> _raycastBuffer = new List<RaycastResult>(16);
    GameObject _lastHoveredForSprite;
    PointerEventData _hoverPointer;

    /// <summary>Extra input ticks when a menu expects a gamepad but none is listed yet (Windows XInput timing).</summary>
    int _menuGamepadPollRemaining = 120;

    /// <summary>True while the pointer graphic lives under PetChoose's menu <see cref="Canvas"/> so it draws above Back/Next (same overlay sort order no longer fights sibling canvases).</summary>
    bool _cursorReparentedUnderPetChooseMenu;

    float _gamepadCursorIdleTimer;
    int _menuCursorContextId = int.MinValue;
    bool _awaitingFirstGamepadPointerMotion = true;
    bool _hadGamepadForMenuLastFrame;

    /// <summary>
    /// Most-recent-input tracking for OS cursor visibility in menu contexts. Physically moving or clicking the
    /// mouse sets this to <see cref="float.PositiveInfinity"/> (OS cursor shown). Any gamepad activity pins it
    /// to <see cref="float.NegativeInfinity"/> (OS cursor hidden). Independent of the gamepad idle-hide timer.
    /// </summary>
    float _osCursorGrantedByMouseKeyboardUntilUnscaled = float.NegativeInfinity;

    static int BuildMenuCursorContextId()
    {
        unchecked
        {
            int h = 17;
            Scene active = SceneManager.GetActiveScene();
            h = h * 29 + active.buildIndex;
            h = h * 29 + (active.name != null ? active.name.GetHashCode() : 0);
            h = h * 29 + (WorldPauseController.IsPaused ? 1 : 0);
            h = h * 29 + (NpcProximityChatOpener.IsWorldNpcChatOpen ? 1 : 0);
            h = h * 29 + (InventoryPanelController.IsOpen ? 1 : 0);
            h = h * 29 + (TeleportMenuController.IsOpen ? 1 : 0);
            h = h * 29 + (IsPetChooseSceneLoaded() ? 1 : 0);
            return h;
        }
    }

    static Vector2 ApplyRadialDeadzone(Vector2 v, float deadzone)
    {
        float m = v.magnitude;
        if (m < deadzone || m < 1e-6f)
            return Vector2.zero;
        float scalar = (m - deadzone) / Mathf.Max(1e-6f, 1f - deadzone);
        return v * (scalar / m);
    }

    /// <summary>True if the player is using the gamepad for UI this frame (sticks, d-pad, triggers, or any usual button).</summary>
    bool ConsumeGamepadUiActivity(Gamepad pad, out Vector2 processedLeftStick)
    {
        processedLeftStick = ApplyRadialDeadzone(pad.leftStick.ReadValue(), stickDeadZone);
        Vector2 rightProc = ApplyRadialDeadzone(pad.rightStick.ReadValue(), stickDeadZone);

        if (processedLeftStick.sqrMagnitude > 1e-8f)
            return true;
        if (rightProc.sqrMagnitude > 1e-8f)
            return true;
        if (pad.dpad.ReadValue().sqrMagnitude > 1e-8f)
            return true;
        if (pad.leftTrigger.ReadValue() > 0.25f || pad.rightTrigger.ReadValue() > 0.25f)
            return true;

        if (pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
            pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)
            return true;
        if (pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)
            return true;
        if (pad.selectButton.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)
            return true;
        if (pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame)
            return true;
        if (pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame ||
            pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame)
            return true;

        return false;
    }

    /// <summary>True when the user physically moved or clicked the mouse this frame. Keyboard is ignored on
    /// purpose: typing on the keyboard doesn't mean the player wants the OS mouse cursor to appear.</summary>
    static bool MouseMenuActivityThisFrame()
    {
        if (Mouse.current == null)
            return false;

        var m = Mouse.current;
        if (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame ||
            m.middleButton.wasPressedThisFrame)
            return true;
        if (m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed)
            return true;
        if (m.delta.ReadValue().sqrMagnitude > 0.0001f)
            return true;
        if (m.scroll.ReadValue().sqrMagnitude > 0.0001f)
            return true;
        return false;
    }

    static bool IsPetChooseSceneLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name == PetChooseSceneName)
                return true;
        }

        return false;
    }

    static bool ShouldShowControllerCursorForScene()
    {
        // Additive PetChoose: active scene often remains "World" until explicitly switched.
        if (IsPetChooseSceneLoaded())
            return true;

        string scene = SceneManager.GetActiveScene().name;
        if (scene != "World")
            return true;
        if (WorldPauseController.IsPaused)
            return true;
        if (InventoryPanelController.IsOpen)
            return true;
        if (TeleportMenuController.IsOpen)
            return true;
        return NpcProximityChatOpener.IsWorldNpcChatOpen;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Application.isPlaying || Instance != null)
            return;
        var go = new GameObject(nameof(ControllerVirtualCursor));
        DontDestroyOnLoad(go);
        go.AddComponent<ControllerVirtualCursor>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (defaultCursorSprite == null)
            defaultCursorSprite = Resources.Load<Sprite>(ResourcesDefaultCursor);
        if (hoverCursorSprite == null)
            hoverCursorSprite = Resources.Load<Sprite>(ResourcesHoverCursor);
        if (hoverCursorSprite == null)
            hoverCursorSprite = defaultCursorSprite;

        _screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var root = gameObject;
        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = DefaultOverlaySortOrder;
        var scaler = root.AddComponent<CanvasScaler>();
        // ConstantPixelSize so the cursor visual is exactly at the screen pixel we click at — no scaling drift between visual and raycast.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        _canvasRect = root.GetComponent<RectTransform>();

        var ptrGo = new GameObject("CursorGraphic");
        ptrGo.transform.SetParent(transform, false);
        _cursorRect = ptrGo.AddComponent<RectTransform>();
        _cursorRect.anchorMin = _cursorRect.anchorMax = new Vector2(0f, 0f);
        _cursorRect.pivot = new Vector2(0f, 1f);
        _cursorImage = ptrGo.AddComponent<Image>();
        _cursorImage.raycastTarget = false;
        ApplySprite(defaultCursorSprite);

        if (defaultCursorSprite == null)
            Debug.LogWarning("[ControllerVirtualCursor] Resources/Sprites/cursor.png not found or not imported as a Sprite.");

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void Start()
    {
        // First menu frame: gamepads sometimes are not in Gamepad.all until the input loop processes once more.
        InputSystem.Update();
        // Initial scene can be PetChoose before sceneLoaded runs relative to this instance.
        if (IsPetChooseSceneLoaded() && !_cursorReparentedUnderPetChooseMenu)
        {
            var s = SceneManager.GetSceneByName(PetChooseSceneName);
            if (s.isLoaded && TryReparentCursorUnderPetChooseMenu(s))
                return;
            RaiseCanvasAboveOtherOverlayUi();
        }
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        InputSystem.onDeviceChange -= OnDeviceChange;
        if (Instance == this)
            Instance = null;
        CursorVisibilityKeeper.SuppressMenuHardwareCursor = false;
    }

    static void OnActiveSceneChanged(Scene _, Scene __)
    {
        InputSystem.Update();
        if (Instance != null)
            Instance._menuGamepadPollRemaining = 120;
    }

    static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added && device is Gamepad g)
            g.MakeCurrent();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != PetChooseSceneName)
            return;
        InputSystem.Update();
        _menuGamepadPollRemaining = 120;
        if (!TryReparentCursorUnderPetChooseMenu(scene))
            RaiseCanvasAboveOtherOverlayUi();
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != PetChooseSceneName || _canvas == null)
            return;
        if (_cursorReparentedUnderPetChooseMenu)
            RestoreCursorAfterPetChooseMenu();
        else
            _canvas.sortingOrder = DefaultOverlaySortOrder;
    }

    static Canvas FindPetChooseMenuCanvas(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "Canvas")
                continue;
            var c = root.GetComponent<Canvas>();
            if (c != null)
                return c;
        }

        Canvas fallback = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            {
                if (c.gameObject.name == "FadeCanvas")
                    continue;
                if (fallback == null)
                    fallback = c;
            }
        }

        return fallback;
    }

    bool TryReparentCursorUnderPetChooseMenu(Scene scene)
    {
        if (_cursorRect == null || _canvas == null)
            return false;

        var menuCanvas = FindPetChooseMenuCanvas(scene);
        if (menuCanvas == null || menuCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return false;

        _cursorRect.SetParent(menuCanvas.transform, worldPositionStays: true);
        _cursorRect.SetAsLastSibling();
        _canvas.enabled = false;
        _cursorReparentedUnderPetChooseMenu = true;
        return true;
    }

    void RestoreCursorAfterPetChooseMenu()
    {
        if (!_cursorReparentedUnderPetChooseMenu || _cursorRect == null || _canvas == null)
            return;

        _cursorRect.SetParent(transform, worldPositionStays: true);
        _cursorRect.SetAsLastSibling();
        _canvas.enabled = true;
        _canvas.sortingOrder = DefaultOverlaySortOrder;
        _cursorReparentedUnderPetChooseMenu = false;
    }

    /// <summary>
    /// PetChoose (especially additive) may add overlay canvases; keep the pointer drawable above all of them.
    /// </summary>
    void RaiseCanvasAboveOtherOverlayUi()
    {
        if (_canvas == null)
            return;
        int maxOther = 0;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null || c == _canvas)
                continue;
            if (c.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;
            maxOther = Mathf.Max(maxOther, c.sortingOrder);
        }

        _canvas.sortingOrder = Mathf.Max(DefaultOverlaySortOrder, maxOther + 200);
    }

    void Update()
    {
        var sceneWantsCursor = ShouldShowControllerCursorForScene();
        var pad = GameplayInput.GetUsableGamepad();
        if (sceneWantsCursor && pad == null && _menuGamepadPollRemaining > 0)
        {
            _menuGamepadPollRemaining--;
            InputSystem.Update();
            pad = GameplayInput.GetUsableGamepad();
        }

        // Keyboard/mouse: hardware cursor may stay hidden in fullscreen / after FPS lock; mirror the mouse with
        // the same sprite used for gamepad so pause menus always show a pointer.
        bool mousePauseOverlay = WorldPauseController.IsPaused && sceneWantsCursor && pad == null &&
                                 Mouse.current != null;
        bool useGamepad = pad != null && sceneWantsCursor;

        // Same as inventory/teleport: the pause menu has sliders and fields that need a pointer
        // without forcing an extra stick wiggle to reveal the gamepad cursor.
        bool worldOverlayWantsImmediateCursor = SceneManager.GetActiveScene().name == "World"
            && (WorldPauseController.IsPaused
                || InventoryPanelController.IsOpen
                || TeleportMenuController.IsOpen);

        int menuCtx = BuildMenuCursorContextId();
        if (menuCtx != _menuCursorContextId)
        {
            _menuCursorContextId = menuCtx;
            if (sceneWantsCursor)
            {
                _awaitingFirstGamepadPointerMotion = !worldOverlayWantsImmediateCursor;
                _gamepadCursorIdleTimer = 0f;
                _screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
        }

        if (useGamepad && !_hadGamepadForMenuLastFrame)
        {
            _awaitingFirstGamepadPointerMotion = !worldOverlayWantsImmediateCursor;
            _gamepadCursorIdleTimer = 0f;
            _screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        Vector2 processedLeftStick = Vector2.zero;
        bool suppressSouthClickWhilePointerOff = useGamepad && _awaitingFirstGamepadPointerMotion;
        if (useGamepad)
        {
            bool padOnly = ConsumeGamepadUiActivity(pad, out processedLeftStick);

            // Most-recent-input wins. Gamepad activity pins the OS cursor "hidden"; mouse activity pins it
            // "shown". Mouse is applied after gamepad so a physical mouse move during the same frame as a
            // gamepad input still reveals the OS cursor (the user clearly just reached for the mouse).
            if (padOnly)
                _osCursorGrantedByMouseKeyboardUntilUnscaled = float.NegativeInfinity;
            if (MouseMenuActivityThisFrame())
                _osCursorGrantedByMouseKeyboardUntilUnscaled = float.PositiveInfinity;

            // Virtual controller pointer: only gamepad activity wakes it, idles it, or clears "awaiting first stick".
            if (padOnly)
                _awaitingFirstGamepadPointerMotion = false;

            if (!_awaitingFirstGamepadPointerMotion)
            {
                if (padOnly)
                    _gamepadCursorIdleTimer = 0f;
                else
                    _gamepadCursorIdleTimer += Time.unscaledDeltaTime;
            }
            else
                _gamepadCursorIdleTimer = 0f;
        }
        else
        {
            _gamepadCursorIdleTimer = 0f;
            _osCursorGrantedByMouseKeyboardUntilUnscaled = float.NegativeInfinity;
        }

        _hadGamepadForMenuLastFrame = useGamepad;

        bool hideAfterIdleEnabled = hideCursorAfterIdleSeconds > 0f;
        bool gamepadIdleTimedOut = hideAfterIdleEnabled && _gamepadCursorIdleTimer >= hideCursorAfterIdleSeconds;
        bool gamepadCursorGraphicOn = useGamepad && !_awaitingFirstGamepadPointerMotion && !gamepadIdleTimedOut;
        bool showGraphic = gamepadCursorGraphicOn || mousePauseOverlay;

        // OS mouse cursor suppression is now driven purely by which input was most recent: gamepad activity
        // pinned the grant to -infinity (hide), mouse activity pinned it to +infinity (show). This replaces
        // the old rule that only hid the OS cursor while the sprite itself was hidden — players expect the
        // physical mouse to disappear the instant they touch the controller, not wait for sprite idle-out.
        bool hardwareHiddenByGamepadPointer = useGamepad && sceneWantsCursor &&
                                              Time.unscaledTime >= _osCursorGrantedByMouseKeyboardUntilUnscaled;
        CursorVisibilityKeeper.SuppressMenuHardwareCursor = hardwareHiddenByGamepadPointer;

        if (_cursorImage != null)
            _cursorImage.enabled = showGraphic;

        if (mousePauseOverlay)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            _screenPosition = Mouse.current.position.ReadValue();
            _screenPosition.x = Mathf.Clamp(_screenPosition.x, 0f, Screen.width - 1f);
            _screenPosition.y = Mathf.Clamp(_screenPosition.y, 0f, Screen.height - 1f);
            ApplyCursorTransform();
            UpdateHoverSprite();
            return;
        }

        if (!useGamepad)
        {
            _lastHoveredForSprite = null;
            return;
        }

        if (!_awaitingFirstGamepadPointerMotion && processedLeftStick.sqrMagnitude > 1e-8f)
            _screenPosition += processedLeftStick * (pointerSpeedPixelsPerSecond * Time.unscaledDeltaTime);

        _screenPosition.x = Mathf.Clamp(_screenPosition.x, 0f, Screen.width - 1f);
        _screenPosition.y = Mathf.Clamp(_screenPosition.y, 0f, Screen.height - 1f);

        ApplyCursorTransform();
        if (showGraphic)
            UpdateHoverSprite();

        if (pad.buttonSouth.wasPressedThisFrame && !suppressSouthClickWhilePointerOff)
            DispatchPointerClick();
    }

    void LateUpdate()
    {
        if (CursorVisibilityKeeper.SuppressMenuHardwareCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            return;
        }

        if (ShouldShowControllerCursorForScene() && Mouse.current != null &&
            Time.unscaledTime < _osCursorGrantedByMouseKeyboardUntilUnscaled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void ApplyCursorTransform()
    {
        if (_cursorRect == null)
            return;

        // Screen-space overlay: same world/screen pixel mapping for DDOL canvas and PetChoose menu canvas (scale 1).
        _cursorRect.position = new Vector3(_screenPosition.x, _screenPosition.y, 0f);
    }

    void ApplySprite(Sprite sprite)
    {
        if (_cursorImage == null || sprite == null)
            return;
        _cursorImage.sprite = sprite;
        float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
        float h = aspect > 0f ? cursorWidth / aspect : cursorWidth;
        _cursorRect.sizeDelta = new Vector2(cursorWidth, h);
    }

    void UpdateHoverSprite()
    {
        if (_cursorImage == null || defaultCursorSprite == null)
            return;

        bool overInteractable = PointerOverInteractable(_screenPosition, out var hoveredGo);
        if (hoveredGo != _lastHoveredForSprite)
        {
            _lastHoveredForSprite = hoveredGo;
            ApplySprite(overInteractable && hoverCursorSprite != null ? hoverCursorSprite : defaultCursorSprite);
        }
    }

    bool PointerOverInteractable(Vector2 screenPos, out GameObject hovered)
    {
        hovered = null;
        var es = EventSystem.current;
        if (es == null)
            return false;

        if (_hoverPointer == null)
            _hoverPointer = new PointerEventData(es);
        _hoverPointer.position = screenPos;

        _raycastBuffer.Clear();
        es.RaycastAll(_hoverPointer, _raycastBuffer);
        foreach (var hit in _raycastBuffer)
        {
            if (hit.gameObject == _cursorImage.gameObject)
                continue;
            var sel = hit.gameObject.GetComponentInParent<Selectable>();
            if (sel != null && sel.interactable && sel.isActiveAndEnabled)
            {
                hovered = sel.gameObject;
                return true;
            }
            if (hit.gameObject.GetComponentInParent<IPointerClickHandler>() != null)
            {
                hovered = hit.gameObject;
                return true;
            }
        }

        return false;
    }

    void DispatchPointerClick()
    {
        var es = EventSystem.current;
        if (es == null)
            return;

        var ped = new PointerEventData(es)
        {
            position = _screenPosition,
            button = PointerEventData.InputButton.Left,
            clickCount = 1,
            clickTime = Time.unscaledTime,
            eligibleForClick = true,
            pressPosition = _screenPosition,
        };

        _raycastBuffer.Clear();
        es.RaycastAll(ped, _raycastBuffer);

        RaycastResult firstHit = default;
        foreach (var hit in _raycastBuffer)
        {
            firstHit = hit;
            break;
        }

        if (firstHit.gameObject == null)
            return;

        ped.pointerCurrentRaycast = firstHit;
        ped.pointerPressRaycast = firstHit;

        GameObject hitGo = firstHit.gameObject;
        GameObject downTarget = ExecuteEvents.ExecuteHierarchy(hitGo, ped, ExecuteEvents.pointerDownHandler);
        GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitGo);
        if (downTarget == null)
            downTarget = clickHandler;
        ped.pointerPress = downTarget;
        ped.rawPointerPress = hitGo;

        if (downTarget != null)
            ExecuteEvents.Execute(downTarget, ped, ExecuteEvents.pointerUpHandler);
        if (clickHandler != null)
            ExecuteEvents.Execute(clickHandler, ped, ExecuteEvents.pointerClickHandler);

        var sel = hitGo.GetComponentInParent<Selectable>();
        if (sel != null && sel.interactable && sel.isActiveAndEnabled)
            es.SetSelectedGameObject(sel.gameObject);
    }
}
#endif
