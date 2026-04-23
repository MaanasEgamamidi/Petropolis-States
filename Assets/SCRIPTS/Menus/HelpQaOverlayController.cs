using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Additively loaded Help Q&amp;A scene. When <see cref="ReturnToPauseOnClose"/> is set by
/// <see cref="WorldPauseController"/> before the scene loads, <see cref="Close"/> unloads
/// and returns to the pause menu. Optionally draws a full-screen <see cref="helpBackgroundSprite"/>
/// behind the chat. Creates a "HelpBack" button on the main Canvas if none exists.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class HelpQaOverlayController : MonoBehaviour
{
    const string BackdropName = "HelpBackdrop";

    [Tooltip("Full-screen image behind the chat (e.g. brown_bg / \"background\" sprite).")]
    [SerializeField] Sprite helpBackgroundSprite;

    /// <summary>
    /// Set to true by <c>WorldPauseController</c> before additively loading this scene
    /// from the pause menu Help action.
    /// </summary>
    public static bool ReturnToPauseOnClose;

    void Awake()
    {
        if (helpBackgroundSprite != null)
            EnsureFullscreenBackdrop(helpBackgroundSprite);
        if (!TryWireHelpBackButton())
            CreateHelpBackButton();
    }

    void EnsureFullscreenBackdrop(Sprite sprite)
    {
        if (transform.Find(BackdropName) != null)
            return;

        var go = new GameObject(
            BackdropName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = Color.white;
    }

    void Update()
    {
        if (WorldPauseController.IsFocusedOnTextInput())
            return;
        if (CancelPressedThisFrame())
            Close();
    }

    static bool CancelPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        return GameplayInput.GamepadCancelPressedThisFrame();
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void Close()
    {
        if (ReturnToPauseOnClose)
            ReturnToPauseOnClose = false;

        var scene = gameObject.scene;
        if (scene.IsValid() && scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
    }

    bool TryWireHelpBackButton()
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name != "HelpBack")
                continue;
            btn.onClick.AddListener(Close);
            return true;
        }

        return false;
    }

    void CreateHelpBackButton()
    {
        var go = new GameObject(
            "HelpBack",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        go.transform.SetAsLastSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(32f, -32f);
        rt.sizeDelta = new Vector2(200f, 56f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
        var colors = go.GetComponent<Button>().colors;
        colors.highlightedColor = new Color(0.32f, 0.4f, 0.5f, 1f);
        colors.pressedColor = new Color(0.16f, 0.2f, 0.28f, 1f);
        go.GetComponent<Button>().colors = colors;
        go.GetComponent<Button>().onClick.AddListener(Close);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
        text.text = "Back";
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
    }
}
