using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Drives the four pet-stat bar fills from <see cref="GlobalData"/> values,
/// applies passive decay each frame, and optionally lets number keys 1-4 boost stats.
/// Attach to the <c>Bars</c> GameObject under <c>Pet Statistics / Left Box / Stats</c>.
/// </summary>
public class PetStatsBarsController : MonoBehaviour
{
    /// <summary>Atlas width for the bar strip; height is matched to laid-out bar height so corners are not vertically squashed.</summary>
    const int BarTexWidth = 128;

    static Sprite s_SharedBarSprite;
    static Texture2D s_SharedBarTexture;

    float _barFullWidth;
    float _barFullHeight;
    [Header("Bar Images (Sliced + width fill)")]
    [SerializeField] Image healthBar;
    [SerializeField] Image happinessBar;
    [SerializeField] Image energyBar;
    [SerializeField] Image hungerBar;

    [Header("Colors")]
    [SerializeField] Color healthColor = new Color(0.90f, 0.30f, 0.35f, 1f);
    [SerializeField] Color happinessColor = new Color(0.95f, 0.80f, 0.25f, 1f);
    [SerializeField] Color energyColor = new Color(0.25f, 0.60f, 0.95f, 1f);
    [SerializeField] Color hungerColor = new Color(0.45f, 0.80f, 0.40f, 1f);

    [Header("Decay")]
    [Tooltip("Points lost per second on each stat while in-game.")]
    [SerializeField] [Min(0f)] float decayRate = 1f;

    [Header("Number-Key Boost")]
    [Tooltip("When enabled, keys 1-4 add boostAmount to Health, Happiness, Energy, Hunger respectively.")]
    [SerializeField] bool numberKeyBoostEnabled = true;
    [SerializeField] [Min(0f)] float boostAmount = 10f;

    [Header("Bar shape")]
    [Tooltip("Corner roundness vs bar thickness (like a rounded square). Only the bar length stretches; 9-slice keeps corners from stretching along the width.")]
    [SerializeField] [Range(0.06f, 0.24f)] float cornerRadiusFractionOfHeight = 0.125f;

    void Awake()
    {
        ResolveBarReferences();
    }

    IEnumerator Start()
    {
        // Wait one frame so VerticalLayoutGroup / CanvasScaler have applied final rect sizes.
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();

        // Middle-center alignment re-centers each bar when width shrinks (both edges move). Left-align like a normal health bar.
        var layout = GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.childAlignment = TextAnchor.MiddleLeft;

        CacheBarDimensions();
        BuildShared9SliceBarSprite();
        ApplySharedRoundedRectSprite(healthBar);
        ApplySharedRoundedRectSprite(happinessBar);
        ApplySharedRoundedRectSprite(energyBar);
        ApplySharedRoundedRectSprite(hungerBar);
        ApplyBarColors();
        ConfigureBarSlicedMode(healthBar);
        ConfigureBarSlicedMode(happinessBar);
        ConfigureBarSlicedMode(energyBar);
        ConfigureBarSlicedMode(hungerBar);
        PrepareLeftAnchoredBarRects();
        SyncFills();
    }

    void OnDestroy()
    {
        if (!Application.isPlaying)
            return;
        if (s_SharedBarSprite != null)
        {
            Destroy(s_SharedBarSprite);
            s_SharedBarSprite = null;
        }
        if (s_SharedBarTexture != null)
        {
            Destroy(s_SharedBarTexture);
            s_SharedBarTexture = null;
        }
    }

    void CacheBarDimensions()
    {
        Image sample = healthBar ?? happinessBar ?? energyBar ?? hungerBar;
        if (sample == null)
            return;
        Rect r = sample.rectTransform.rect;
        _barFullWidth = r.width > 1f ? r.width : 297f;
        _barFullHeight = r.height > 1f ? r.height : 38f;
    }

    /// <summary>
    /// Rounded rect atlas; 9-slice keeps corner shape like a rounded square (only the flat middle stretches with bar length).
    /// Texture height follows the real bar height so the image is not vertically squashed vs width.
    /// </summary>
    void BuildShared9SliceBarSprite()
    {
        int texH = Mathf.Clamp(Mathf.RoundToInt(_barFullHeight), 24, 64);
        float radius = Mathf.Min(
            texH * 0.5f - 1f,
            Mathf.Max(3.5f, cornerRadiusFractionOfHeight * texH));
        int border = Mathf.Clamp(Mathf.RoundToInt(radius), 4, texH / 2 - 2);
        int texW = Mathf.Max(BarTexWidth, border * 2 + 24);
        EnsureSharedBarSprite(texW, texH, border);
    }

    /// <summary>
    /// LeanGUI's Round10 is a circle/capsule; filled horizontally it reads as an oval.
    /// Use a flat rounded-rectangle silhouette so the bar matches a typical UI "rounded rect" (inspector look).
    /// </summary>
    void ApplySharedRoundedRectSprite(Image img)
    {
        if (img == null || s_SharedBarSprite == null) return;
        img.sprite = s_SharedBarSprite;
    }

    void EnsureSharedBarSprite(int width, int height, int borderPixels)
    {
        if (s_SharedBarSprite != null)
            return;

        float radius = borderPixels;
        s_SharedBarTexture = CreateRoundedRectTexture(width, height, radius);
        s_SharedBarTexture.name = "PetStatBarRoundedRect9Slice";
        s_SharedBarTexture.filterMode = FilterMode.Bilinear;
        s_SharedBarTexture.wrapMode = TextureWrapMode.Clamp;

        var borders = new Vector4(borderPixels, borderPixels, borderPixels, borderPixels);
        s_SharedBarSprite = Sprite.Create(
            s_SharedBarTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            borders);
        s_SharedBarSprite.name = "PetStatBarRoundedRect9Slice";
    }

    static Texture2D CreateRoundedRectTexture(int width, int height, float radius)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        radius = Mathf.Min(radius, Mathf.Min(width, height) * 0.5f - 0.01f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = x + 0.5f;
                float cy = y + 0.5f;
                float a = RoundedRectAlpha(cx, cy, width, height, radius);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary>1 inside, 0 outside; SDF antialiases the edge.</summary>
    static float RoundedRectAlpha(float x, float y, float w, float h, float r)
    {
        float d = SignedDistanceRoundBox(x, y, w, h, r);
        return Mathf.Clamp01(0.5f - d);
    }

    /// <summary>Inigo Quilez round-box SDF; negative = inside.</summary>
    static float SignedDistanceRoundBox(float x, float y, float w, float h, float rad)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        var p = new Vector2(x - halfW, y - halfH);
        var b = new Vector2(halfW - rad, halfH - rad);
        var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
        float part = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        var qPos = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return part + qPos.magnitude - rad;
    }

    void ResolveBarReferences()
    {
        if (healthBar == null) healthBar = FindChildImage("HealthBar");
        if (happinessBar == null) happinessBar = FindChildImage("HappinessBar");
        if (energyBar == null) energyBar = FindChildImage("EnergyBar");
        if (hungerBar == null) hungerBar = FindChildImage("HungerBar");
    }

    Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        float dt = Time.deltaTime;
        ApplyDecay(dt);

        if (numberKeyBoostEnabled)
            HandleBoostInput();

        SyncFills();
    }

    void ApplyDecay(float dt)
    {
        float loss = decayRate * dt;
        GlobalData.PetHealth = Mathf.Max(0f, GlobalData.PetHealth - loss);
        GlobalData.PetHappiness = Mathf.Max(0f, GlobalData.PetHappiness - loss);
        GlobalData.PetEnergy = Mathf.Max(0f, GlobalData.PetEnergy - loss);
        GlobalData.PetHunger = Mathf.Max(0f, GlobalData.PetHunger - loss);
    }

    void HandleBoostInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[Key.Digit1].wasPressedThisFrame) GlobalData.PetHealth = Mathf.Min(100f, GlobalData.PetHealth + boostAmount);
        if (kb[Key.Digit2].wasPressedThisFrame) GlobalData.PetHappiness = Mathf.Min(100f, GlobalData.PetHappiness + boostAmount);
        if (kb[Key.Digit3].wasPressedThisFrame) GlobalData.PetEnergy = Mathf.Min(100f, GlobalData.PetEnergy + boostAmount);
        if (kb[Key.Digit4].wasPressedThisFrame) GlobalData.PetHunger = Mathf.Min(100f, GlobalData.PetHunger + boostAmount);
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) GlobalData.PetHealth = Mathf.Min(100f, GlobalData.PetHealth + boostAmount);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GlobalData.PetHappiness = Mathf.Min(100f, GlobalData.PetHappiness + boostAmount);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GlobalData.PetEnergy = Mathf.Min(100f, GlobalData.PetEnergy + boostAmount);
        if (Input.GetKeyDown(KeyCode.Alpha4)) GlobalData.PetHunger = Mathf.Min(100f, GlobalData.PetHunger + boostAmount);
#endif
    }

    void SyncFills()
    {
        SetBarFillWidth(healthBar, GlobalData.PetHealth / 100f);
        SetBarFillWidth(happinessBar, GlobalData.PetHappiness / 100f);
        SetBarFillWidth(energyBar, GlobalData.PetEnergy / 100f);
        SetBarFillWidth(hungerBar, GlobalData.PetHunger / 100f);
    }

    void SetBarFillWidth(Image img, float normalizedFill)
    {
        if (img == null || _barFullWidth <= 0.01f) return;
        float w = _barFullWidth * Mathf.Clamp01(normalizedFill);
        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
    }

    void ApplyBarColors()
    {
        if (healthBar != null) healthBar.color = healthColor;
        if (happinessBar != null) happinessBar.color = happinessColor;
        if (energyBar != null) energyBar.color = energyColor;
        if (hungerBar != null) hungerBar.color = hungerColor;
    }

    static void ConfigureBarSlicedMode(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
    }

    /// <summary>Pivot at left-center so shortening the bar keeps the left edge fixed (matches horizontal "fill").</summary>
    void PrepareLeftAnchoredBarRects()
    {
        foreach (Image img in new[] { healthBar, happinessBar, energyBar, hungerBar })
        {
            if (img == null) continue;
            RectTransform rt = img.rectTransform;
            SetPivotPreservingAnchoredPosition(rt, new Vector2(0f, 0.5f));
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _barFullHeight);
        }
    }

    static void SetPivotPreservingAnchoredPosition(RectTransform rt, Vector2 newPivot)
    {
        Vector2 oldPivot = rt.pivot;
        if (oldPivot == newPivot) return;
        Vector2 size = rt.rect.size;
        rt.pivot = newPivot;
        rt.anchoredPosition += new Vector2(
            (newPivot.x - oldPivot.x) * size.x,
            (newPivot.y - oldPivot.y) * size.y);
    }
}
