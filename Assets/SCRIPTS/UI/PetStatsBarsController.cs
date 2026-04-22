using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Drives the four pet-stat bar fills from <see cref="GlobalData"/> values,
/// and optionally lets number keys 2-4 boost happiness/energy/hunger. Passive decay runs in
/// <see cref="PetNeedsSimulator"/> (not here) so stats always tick. Health is a derived stat:
/// <c>(PetEnergy + PetHunger) / 2</c>. When at least two care stats hit zero, the world fades to the
/// closing scene.
/// Bar widths can ease toward live stats (<see cref="smoothBarChanges"/>) so jumps from zones or keys animate smoothly.
/// Each bar has a full-width background track (dark border, dimmed) and a fill Image whose
/// border and right-edge curve follow the fill as it decays.
/// A short feeling word (e.g. "Happy", "Sick") derived from the four stats is written into <see cref="feelingText"/>.
/// Attach to the <c>Bars</c> GameObject under <c>Pet Statistics / Left Box / Stats</c>.
/// </summary>
public class PetStatsBarsController : MonoBehaviour
{
    const int BarTexWidth = 128;

    // Fill bar sprite: colored fill with dark border ring baked in.
    static Sprite s_SharedFillSprite;
    static Texture2D s_SharedFillTexture;

    // Track sprite: semi-transparent background with dark border.
    static Sprite s_SharedTrackSprite;
    static Texture2D s_SharedTrackTexture;

    float _barFullWidth;
    float _barFullHeight;

    [Header("Bar Images (Sliced + width fill)")]
    [SerializeField] Image healthBar;
    [SerializeField] Image happinessBar;
    [SerializeField] Image energyBar;
    [SerializeField] Image hungerBar;

    // Runtime-created track images (background).
    Image _healthTrack;
    Image _happinessTrack;
    Image _energyTrack;
    Image _hungerTrack;

    [Header("Colors")]
    [SerializeField] Color healthColor    = new Color(0.90f, 0.30f, 0.35f, 1f);
    [SerializeField] Color happinessColor = new Color(0.95f, 0.80f, 0.25f, 1f);
    [SerializeField] Color energyColor    = new Color(0.25f, 0.60f, 0.95f, 1f);
    [SerializeField] Color hungerColor    = new Color(0.45f, 0.80f, 0.40f, 1f);

    [Header("Border")]
    [Tooltip("Color of the border drawn around both the fill bar and the background track.")]
    [SerializeField] Color borderColor = new Color(0.10f, 0.10f, 0.10f, 1f);
    [Tooltip("Border thickness as a fraction of bar height (e.g. 0.08 ≈ 8%). Values near 1 hide the colored fill.")]
    [SerializeField] [Range(0.02f, 0.25f)] float borderThicknessFraction = 0.08f;
    [Tooltip("Alpha of the background track image (0 = invisible, 1 = opaque).")]
    [SerializeField] [Range(0f, 1f)] float trackAlpha = 0.35f;

    [Header("Number-Key Boost")]
    [Tooltip("When enabled, keys 2-4 add boostAmount to Happiness, Energy, Hunger. Health is derived and not boosted directly.")]
    [SerializeField] bool numberKeyBoostEnabled = true;
    [SerializeField] [Min(0f)] float boostAmount = 10f;

    [Header("Feeling Text")]
    [Tooltip("TMP text under Pet Statistics / Left Box / Stats / FeelingText. Auto-resolved by name if unset.")]
    [SerializeField] TMP_Text feelingText;

    [Header("Bar animation")]
    [Tooltip("When true, fill width eases toward GlobalData; when false, fills match stats every frame.")]
    [SerializeField] bool smoothBarChanges = true;
    [Tooltip("Approximate seconds for the bar to settle after a stat change. Smaller = snappier.")]
    [SerializeField] [Min(0.02f)] float barSmoothTime = 0.35f;

    float _displayHealth;
    float _displayHappiness;
    float _displayEnergy;
    float _displayHunger;
    float _velHealth;
    float _velHappiness;
    float _velEnergy;
    float _velHunger;

    [Header("Bar shape")]
    [Tooltip("Corner roundness vs bar thickness. Only the flat middle stretches; 9-slice keeps corners from squashing.")]
    [SerializeField] [Range(0.06f, 0.24f)] float cornerRadiusFractionOfHeight = 0.125f;

    void Awake()
    {
        ResolveBarReferences();
        ResolveFeelingText();
        GlobalData.RecomputeHealth();
        ResetDisplayedStatsToGlobal();
    }

    void ResolveFeelingText()
    {
        if (feelingText != null) return;
        Transform stats = transform.parent;
        if (stats == null) return;
        Transform t = stats.Find("FeelingText");
        if (t != null)
            feelingText = t.GetComponent<TMP_Text>();
    }

    void ResetDisplayedStatsToGlobal()
    {
        _displayHealth    = GlobalData.PetHealth;
        _displayHappiness = GlobalData.PetHappiness;
        _displayEnergy    = GlobalData.PetEnergy;
        _displayHunger    = GlobalData.PetHunger;
        _velHealth = _velHappiness = _velEnergy = _velHunger = 0f;
    }

    IEnumerator Start()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();

        var layout = GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.childAlignment = TextAnchor.MiddleLeft;

        CacheBarDimensions();
        BuildSharedSprites();

        ApplyFillSprite(healthBar);
        ApplyFillSprite(happinessBar);
        ApplyFillSprite(energyBar);
        ApplyFillSprite(hungerBar);

        ApplyBarColors();
        ConfigureBarSlicedMode(healthBar);
        ConfigureBarSlicedMode(happinessBar);
        ConfigureBarSlicedMode(energyBar);
        ConfigureBarSlicedMode(hungerBar);
        PrepareLeftAnchoredBarRects();

        // Create background tracks behind each fill bar.
        _healthTrack    = CreateTrack(healthBar,    healthColor);
        _happinessTrack = CreateTrack(happinessBar, happinessColor);
        _energyTrack    = CreateTrack(energyBar,    energyColor);
        _hungerTrack    = CreateTrack(hungerBar,    hungerColor);

        SyncFills();
    }

    void OnDestroy()
    {
        if (!Application.isPlaying)
            return;
        DestroyAsset(ref s_SharedFillSprite);
        DestroyAsset(ref s_SharedFillTexture);
        DestroyAsset(ref s_SharedTrackSprite);
        DestroyAsset(ref s_SharedTrackTexture);
    }

    static void DestroyAsset<T>(ref T obj) where T : Object
    {
        if (obj != null) { Destroy(obj); obj = null; }
    }

    // ── Dimension helpers ──────────────────────────────────────────────────────

    void CacheBarDimensions()
    {
        Image sample = healthBar ?? happinessBar ?? energyBar ?? hungerBar;
        if (sample == null) return;
        Rect r = sample.rectTransform.rect;
        _barFullWidth  = r.width  > 1f ? r.width  : 297f;
        _barFullHeight = r.height > 1f ? r.height : 38f;
    }

    // ── Sprite generation ──────────────────────────────────────────────────────

    void BuildSharedSprites()
    {
        int texH   = Mathf.Clamp(Mathf.RoundToInt(_barFullHeight), 24, 64);
        float radius = Mathf.Min(texH * 0.5f - 1f, Mathf.Max(3.5f, cornerRadiusFractionOfHeight * texH));
        int border = Mathf.Clamp(Mathf.RoundToInt(radius), 4, texH / 2 - 2);
        int texW   = Mathf.Max(BarTexWidth, border * 2 + 24);
        int bThick = Mathf.Max(1, Mathf.RoundToInt(borderThicknessFraction * texH));
        int bThickMax = Mathf.Max(2, texH / 4);
        if (bThick > bThickMax)
            bThick = bThickMax;

        if (s_SharedFillSprite == null)
        {
            s_SharedFillTexture = CreateBorderedRoundedRectTexture(texW, texH, radius, bThick, borderColor, fillInside: true);
            s_SharedFillTexture.name = "PetStatBarFill9Slice";
            s_SharedFillTexture.filterMode = FilterMode.Bilinear;
            s_SharedFillTexture.wrapMode   = TextureWrapMode.Clamp;
            s_SharedFillSprite = MakeSprite(s_SharedFillTexture, texW, texH, border, "PetStatBarFill9Slice");
        }

        if (s_SharedTrackSprite == null)
        {
            s_SharedTrackTexture = CreateBorderedRoundedRectTexture(texW, texH, radius, bThick, borderColor, fillInside: false);
            s_SharedTrackTexture.name = "PetStatBarTrack9Slice";
            s_SharedTrackTexture.filterMode = FilterMode.Bilinear;
            s_SharedTrackTexture.wrapMode   = TextureWrapMode.Clamp;
            s_SharedTrackSprite = MakeSprite(s_SharedTrackTexture, texW, texH, border, "PetStatBarTrack9Slice");
        }
    }

    static Sprite MakeSprite(Texture2D tex, int w, int h, int border, string spriteName)
    {
        var borders = new Vector4(border, border, border, border);
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borders);
        sp.name = spriteName;
        return sp;
    }

    /// <summary>
    /// Generates a rounded-rect texture where:
    /// - The outer silhouette is the full rounded rect (alpha = 1 inside, 0 outside).
    /// - A dark border ring of <paramref name="borderThick"/> pixels is drawn at the edge.
    /// - The center fill is white (tinted by Image.color at runtime) when <paramref name="fillInside"/> is true,
    ///   or transparent when false (track background: only the border ring is opaque).
    /// </summary>
    static Texture2D CreateBorderedRoundedRectTexture(int width, int height, float radius, int borderThick, Color borderCol, bool fillInside)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        radius = Mathf.Min(radius, Mathf.Min(width, height) * 0.5f - 0.01f);
        float innerRadius = Mathf.Max(1f, radius - borderThick);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = x + 0.5f;
                float cy = y + 0.5f;

                // Outer shape alpha (the full rounded rect).
                float outerD = SignedDistanceRoundBox(cx, cy, width, height, radius);
                float outerA = Mathf.Clamp01(0.5f - outerD);

                if (outerA <= 0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Inner shape: inset the rounded rect by borderThick pixels on every side.
                float shrunkenD = SignedDistanceRoundBoxRect(cx, cy, width, height, innerRadius, borderThick);
                float innerA = Mathf.Clamp01(0.5f - shrunkenD);

                // borderA: 1 in the border ring, 0 in the interior.
                float borderA = outerA * (1f - innerA);

                Color pixel;
                if (fillInside)
                {
                    // Fill texture: border uses borderCol, interior uses white (tinted by Image.color).
                    Color fillCol = new Color(1f, 1f, 1f, innerA * outerA);
                    Color bCol    = new Color(borderCol.r, borderCol.g, borderCol.b, borderA * borderCol.a);
                    pixel = AlphaBlendOver(fillCol, bCol);
                }
                else
                {
                    // Track texture: only the border ring is drawn; interior is transparent.
                    pixel = new Color(borderCol.r, borderCol.g, borderCol.b, borderA * borderCol.a);
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary>SDF for a rounded rect whose interior is inset by <paramref name="inset"/> pixels on each side.</summary>
    static float SignedDistanceRoundBoxRect(float x, float y, float w, float h, float r, float inset)
    {
        float halfW = w * 0.5f - inset;
        float halfH = h * 0.5f - inset;
        if (halfW <= 0f || halfH <= 0f) return 1f; // fully clipped
        var p = new Vector2(x - w * 0.5f, y - h * 0.5f);
        var b = new Vector2(halfW - r, halfH - r);
        b = new Vector2(Mathf.Max(0f, b.x), Mathf.Max(0f, b.y));
        var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
        float part = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        var qPos = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return part + qPos.magnitude - r;
    }

    /// <summary>Blend <paramref name="src"/> over <paramref name="dst"/> (Porter-Duff src-over).</summary>
    static Color AlphaBlendOver(Color dst, Color src)
    {
        float outA = src.a + dst.a * (1f - src.a);
        if (outA < 0.0001f) return Color.clear;
        float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
        float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
        float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;
        return new Color(r, g, b, outA);
    }

    void ApplyFillSprite(Image img)
    {
        if (img == null || s_SharedFillSprite == null) return;
        img.sprite = s_SharedFillSprite;
    }

    // ── Track creation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a full-width background track Image as a sibling of <paramref name="fillBar"/>,
    /// positioned to exactly underlay it. A <see cref="LayoutElement"/> with ignoreLayout=true
    /// prevents the VerticalLayoutGroup from giving it its own slot in the stack.
    /// The track copies the fill bar's <c>localScale</c> so a designer-applied shrink on the fill
    /// (e.g. <c>localScale = 0.85</c>) still produces a visually-aligned track at 100% fill.
    /// </summary>
    Image CreateTrack(Image fillBar, Color barColor)
    {
        if (fillBar == null || s_SharedTrackSprite == null) return null;

        var trackGO = new GameObject(fillBar.gameObject.name + "_Track", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        trackGO.transform.SetParent(fillBar.transform.parent, false);

        trackGO.GetComponent<LayoutElement>().ignoreLayout = true;

        trackGO.transform.SetSiblingIndex(fillBar.transform.GetSiblingIndex());

        var img = trackGO.GetComponent<Image>();
        img.sprite = s_SharedTrackSprite;
        img.type   = Image.Type.Sliced;
        img.preserveAspect = false;
        img.color  = new Color(barColor.r, barColor.g, barColor.b, trackAlpha);

        RectTransform rt     = trackGO.GetComponent<RectTransform>();
        RectTransform fillRt = fillBar.rectTransform;
        rt.pivot           = fillRt.pivot;
        rt.anchorMin       = fillRt.anchorMin;
        rt.anchorMax       = fillRt.anchorMax;
        rt.anchoredPosition = fillRt.anchoredPosition;
        rt.localScale       = fillRt.localScale;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _barFullWidth);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   _barFullHeight);

        return img;
    }

    // ── References ─────────────────────────────────────────────────────────────

    void ResolveBarReferences()
    {
        if (healthBar == null)    healthBar    = FindChildImage("HealthBar");
        if (happinessBar == null) happinessBar = FindChildImage("HappinessBar");
        if (energyBar == null)    energyBar    = FindChildImage("EnergyBar");
        if (hungerBar == null)    hungerBar    = FindChildImage("HungerBar");
    }

    Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    // ── Update loop ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!Application.isPlaying) return;

        if (numberKeyBoostEnabled)
            HandleBoostInput();

        GlobalData.RecomputeHealth();
        UpdateFeeling();
        SyncFills();
    }

    void HandleBoostInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[Key.Digit2].wasPressedThisFrame) { GlobalData.PetHappiness = Mathf.Min(100f, GlobalData.PetHappiness + boostAmount); GlobalData.RecomputeHealth(); }
        if (kb[Key.Digit3].wasPressedThisFrame) { GlobalData.PetEnergy    = Mathf.Min(100f, GlobalData.PetEnergy    + boostAmount); GlobalData.RecomputeHealth(); }
        if (kb[Key.Digit4].wasPressedThisFrame) { GlobalData.PetHunger    = Mathf.Min(100f, GlobalData.PetHunger    + boostAmount); GlobalData.RecomputeHealth(); }
#else
        if (Input.GetKeyDown(KeyCode.Alpha2)) { GlobalData.PetHappiness = Mathf.Min(100f, GlobalData.PetHappiness + boostAmount); GlobalData.RecomputeHealth(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { GlobalData.PetEnergy    = Mathf.Min(100f, GlobalData.PetEnergy    + boostAmount); GlobalData.RecomputeHealth(); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { GlobalData.PetHunger    = Mathf.Min(100f, GlobalData.PetHunger    + boostAmount); GlobalData.RecomputeHealth(); }
#endif
    }

    // ── Feeling word ───────────────────────────────────────────────────────────

    /// <summary>Single ordered list of (label, predicate) pairs evaluated top-down. The first match wins,
    /// so high-priority states (Dying, Miserable) come first and Content / "Okay" sit at the bottom.</summary>
    static readonly System.Collections.Generic.List<(string Label, System.Func<float, float, float, float, bool> Predicate)> Feelings = new()
    {
        ("Dying",      (he, ha, en, hu) => he <= 5f),
        ("Miserable",  (he, ha, en, hu) => en <= 15f && hu <= 15f),
        ("Sick",       (he, ha, en, hu) => he <= 25f),
        ("Famished",   (he, ha, en, hu) => hu <= 20f),
        ("Exhausted",  (he, ha, en, hu) => en <= 20f),
        ("Sad",        (he, ha, en, hu) => ha <= 20f),
        ("Hungry",     (he, ha, en, hu) => hu <= 40f),
        ("Sleepy",     (he, ha, en, hu) => en <= 40f),
        ("Gloomy",     (he, ha, en, hu) => ha <= 40f),
        ("Joyful",     (he, ha, en, hu) => ha >= 80f && en >= 70f),
        ("Energetic",  (he, ha, en, hu) => en >= 80f && hu >= 70f),
        ("Happy",      (he, ha, en, hu) => ha >= 70f && hu >= 70f),
        ("Content",    (he, ha, en, hu) => he >= 70f && ha >= 60f),
        ("Okay",       (he, ha, en, hu) => true),
    };

    void UpdateFeeling()
    {
        if (feelingText == null) return;
        string label = ResolveFeeling(GlobalData.PetHealth, GlobalData.PetHappiness, GlobalData.PetEnergy, GlobalData.PetHunger);
        if (feelingText.text != label)
            feelingText.text = label;
    }

    static string ResolveFeeling(float health, float happiness, float energy, float hunger)
    {
        foreach (var f in Feelings)
            if (f.Predicate(health, happiness, energy, hunger))
                return f.Label;
        return "Okay :|";
    }

    void SyncFills()
    {
        float dt = Time.deltaTime;
        float th = GlobalData.PetHealth;
        float tp = GlobalData.PetHappiness;
        float te = GlobalData.PetEnergy;
        float tn = GlobalData.PetHunger;

        if (smoothBarChanges && _barFullWidth > 0.01f && dt > 0f)
        {
            _displayHealth    = Mathf.SmoothDamp(_displayHealth,    th, ref _velHealth,    barSmoothTime, Mathf.Infinity, dt);
            _displayHappiness = Mathf.SmoothDamp(_displayHappiness, tp, ref _velHappiness, barSmoothTime, Mathf.Infinity, dt);
            _displayEnergy    = Mathf.SmoothDamp(_displayEnergy,    te, ref _velEnergy,    barSmoothTime, Mathf.Infinity, dt);
            _displayHunger    = Mathf.SmoothDamp(_displayHunger,    tn, ref _velHunger,    barSmoothTime, Mathf.Infinity, dt);
        }
        else
        {
            _displayHealth    = th;
            _displayHappiness = tp;
            _displayEnergy    = te;
            _displayHunger    = tn;
        }

        SetBarFillWidth(healthBar,    _displayHealth    / 100f);
        SetBarFillWidth(happinessBar, _displayHappiness / 100f);
        SetBarFillWidth(energyBar,    _displayEnergy    / 100f);
        SetBarFillWidth(hungerBar,    _displayHunger    / 100f);
    }

    void SetBarFillWidth(Image img, float normalizedFill)
    {
        if (img == null || _barFullWidth <= 0.01f) return;
        float w = _barFullWidth * Mathf.Clamp01(normalizedFill);
        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
    }

    void ApplyBarColors()
    {
        if (healthBar != null)    healthBar.color    = healthColor;
        if (happinessBar != null) happinessBar.color = happinessColor;
        if (energyBar != null)    energyBar.color    = energyColor;
        if (hungerBar != null)    hungerBar.color    = hungerColor;
    }

    static void ConfigureBarSlicedMode(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
    }

    /// <summary>Pivot at left-center so shortening the bar keeps the left edge fixed.</summary>
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
}
