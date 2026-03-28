using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimap shape (square / circle) and optional border are driven by shader
/// <c>Custom/UI/MinimapRawImage</c> on <see cref="uiMaterialTemplate"/> (MinimapRawImageUI.mat).
/// </summary>
public enum MinimapDisplayShape
{
    Square,
    Circle
}

public enum MinimapBorderStyle
{
    None,
    Border
}

[ExecuteAlways]
public class MinimapDisplayController : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] MinimapDisplayShape displayShape = MinimapDisplayShape.Circle;

    [Header("Border")]
    [Tooltip("None = no frame. Border = anti-aliased rim (works for both Square and Circle).")]
    [SerializeField] MinimapBorderStyle borderStyle = MinimapBorderStyle.None;

    [Tooltip("Border thickness in UV space (shared by square and circle). Try 0.02–0.05.")]
    [SerializeField] [Range(0.005f, 0.12f)] float borderWidthUv = 0.03f;

    [SerializeField] Color borderColor = new Color(0.08f, 0.08f, 0.12f, 1f);

    [Header("References")]
    [Tooltip("Defaults to RawImage on a child if not set.")]
    [SerializeField] RawImage minimapRawImage;

    [Tooltip("Material using shader Custom/UI/MinimapRawImage (project asset MinimapRawImageUI.mat).")]
    [SerializeField] Material uiMaterialTemplate;

    [Header("Quality")]
    [Tooltip("Higher = softer shape edge fade. Typical 1.5–3.")]
    [SerializeField] float edgeAa = 2f;

    [Header("Fade")]
    [Tooltip("Seconds to fade minimap in/out when toggled or on load. Default 0.1s.")]
    [SerializeField] [Min(0.01f)] float fadeDuration = 0.1f;

    Material _materialInstance;
    CanvasGroup _canvasGroup;
    float _presentationAlpha;

    void Awake()
    {
        if (Application.isPlaying)
            return;
        EnsureMaterialInstance();
        ApplyMaterialSettings();
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        ApplyFromPlayerSettings();
    }

    void Update()
    {
        if (!Application.isPlaying || MinimapSettings.Mode == MinimapMode.None)
            return;

        if (minimapRawImage == null)
            minimapRawImage = GetComponentInChildren<RawImage>(true);

        if (minimapRawImage == null)
            return;

        CanvasGroup cg = EnsureCanvasGroup();
        float target = MinimapSettings.IsVisible ? 1f : 0f;
        float step = Time.deltaTime / Mathf.Max(0.01f, fadeDuration);
        _presentationAlpha = Mathf.MoveTowards(_presentationAlpha, target, step);
        cg.alpha = _presentationAlpha;
        cg.blocksRaycasts = _presentationAlpha > 0.01f;
        MinimapSettings.PresentationAlpha = _presentationAlpha;
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureMaterialInstance();
            ApplyMaterialSettings();
        }
    }

    void OnValidate()
    {
        if (minimapRawImage == null)
            minimapRawImage = GetComponentInChildren<RawImage>(true);

        if (!Application.isPlaying)
        {
            EnsureMaterialInstance();
            ApplyMaterialSettings();
        }
    }

    void ApplyFromPlayerSettings()
    {
        MinimapSettings.RuntimeSuppressed = false;

        switch (MinimapSettings.Mode)
        {
            case MinimapMode.None:
                MinimapSettings.PresentationAlpha = 0f;
                gameObject.SetActive(false);
                return;
            case MinimapMode.Circle:
                displayShape = MinimapDisplayShape.Circle;
                break;
            case MinimapMode.Square:
                displayShape = MinimapDisplayShape.Square;
                break;
        }

        gameObject.SetActive(true);
        if (minimapRawImage == null)
            minimapRawImage = GetComponentInChildren<RawImage>(true);
        if (minimapRawImage != null)
            minimapRawImage.gameObject.SetActive(true);

        _presentationAlpha = 0f;
        MinimapSettings.PresentationAlpha = 0f;
        CanvasGroup cg = EnsureCanvasGroup();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        EnsureMaterialInstance();
        ApplyMaterialSettings();
    }

    CanvasGroup EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        return _canvasGroup;
    }

    void OnDestroy()
    {
        if (_materialInstance != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_materialInstance);
            else
#endif
            Destroy(_materialInstance);
            _materialInstance = null;
        }
    }

    void EnsureMaterialInstance()
    {
        if (uiMaterialTemplate == null || minimapRawImage == null)
            return;

        if (_materialInstance == null)
            _materialInstance = Instantiate(uiMaterialTemplate);
    }

    void ApplyMaterialSettings()
    {
        if (minimapRawImage == null)
            minimapRawImage = GetComponentInChildren<RawImage>(true);

        if (minimapRawImage == null)
            return;

        if (uiMaterialTemplate == null)
        {
            minimapRawImage.material = null;
            return;
        }

        EnsureMaterialInstance();

        bool circle = displayShape == MinimapDisplayShape.Circle;
        bool border = borderStyle == MinimapBorderStyle.Border;

        _materialInstance.SetFloat("_UseCircularClip", circle ? 1f : 0f);
        _materialInstance.SetFloat("_EdgeAA", Mathf.Max(0.5f, edgeAa));
        _materialInstance.SetFloat("_UseBorder", border ? 1f : 0f);
        _materialInstance.SetFloat("_BorderWidthUV", borderWidthUv);
        _materialInstance.SetColor("_BorderColor", borderColor);
        minimapRawImage.material = _materialInstance;
    }
}
