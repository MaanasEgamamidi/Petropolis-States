using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot Editor tool that builds the player inventory UI inside the Inventory canvas of the
/// currently-open scene (expected: World.unity). Creates the InventoryRow prefab, populates the
/// Panel hierarchy, attaches <see cref="InventoryPanelController"/>, and wires all scene references.
/// Safe to re-run — existing Panel/Backdrop children are replaced.
///
/// Styling: uses AlteHaasGrotesk Regular/Bold SDF fonts, a light (white) theme, and
/// rounded-corner 9-sliced sprites generated once into Assets/Prefabs/UI/.
/// </summary>
public static class SetupInventoryUI
{
    const string PrefabDir = "Assets/Prefabs/UI";
    const string PrefabPath = PrefabDir + "/InventoryRow.prefab";

    const string FontRegularPath = "Assets/Fonts/AlteHaasGroteskRegular SDF.asset";
    const string FontBoldPath    = "Assets/Fonts/AlteHaasGroteskBold SDF.asset";

    // Generated sprite assets (rounded rectangles, 9-sliced)
    const string PanelSpritePath  = PrefabDir + "/InventoryPanelRounded.png";
    const string RowSpritePath    = PrefabDir + "/InventoryRowRounded.png";
    const string ButtonSpritePath = PrefabDir + "/InventoryButtonRounded.png";

    const int PanelWidth = 780;
    const int PanelHeight = 520;
    const int RowHeight = 48;

    // Fixed column widths (pixels). Header cells and row cells share these exactly so columns line up pixel-perfectly.
    const float ColWidthName   = 220f;
    const float ColWidthAmount =  90f;
    const float ColWidthEffect = 170f;
    const float ColWidthCost   =  90f;
    const float ColWidthUse    = 110f;
    const int RowPadLeft = 18;
    const int RowPadRight = 18;
    const int RowSpacing = 10;

    // Light theme palette.
    static readonly Color BackdropColor      = new Color(0f,    0f,    0f,    0.35f);
    static readonly Color PanelColor         = new Color(0.98f, 0.98f, 0.99f, 1f);
    static readonly Color RowStripeColor     = new Color(0.94f, 0.95f, 0.97f, 1f);
    static readonly Color HeaderRowColor     = new Color(0.90f, 0.92f, 0.96f, 1f);
    static readonly Color BodyTextColor      = new Color(0.12f, 0.13f, 0.16f, 1f);
    static readonly Color TitleTextColor     = new Color(0.08f, 0.09f, 0.12f, 1f);
    static readonly Color DimTextColor       = new Color(0.42f, 0.44f, 0.50f, 1f);
    static readonly Color EffectColor        = new Color(0.15f, 0.62f, 0.35f, 1f);
    static readonly Color CostColor          = new Color(0.80f, 0.48f, 0.08f, 1f);
    static readonly Color UseNormalColor     = new Color(0.23f, 0.52f, 0.95f, 1f);
    static readonly Color UseHoverColor      = new Color(0.31f, 0.60f, 1.00f, 1f);
    static readonly Color UsePressColor      = new Color(0.17f, 0.42f, 0.82f, 1f);
    static readonly Color UseDisabledColor   = new Color(0.82f, 0.84f, 0.88f, 1f);
    static readonly Color UseDisabledText    = new Color(0.55f, 0.57f, 0.62f, 1f);

    // Corner radii (pixels) for the various rounded sprites.
    const int PanelCornerRadius  = 20;
    const int RowCornerRadius    = 12;
    const int ButtonCornerRadius = 10;

    static TMP_FontAsset s_fontRegular;
    static TMP_FontAsset s_fontBold;
    static Sprite s_panelSprite;
    static Sprite s_rowSprite;
    static Sprite s_buttonSprite;

    [MenuItem("Tools/Setup Inventory UI")]
    public static void Setup()
    {
        GameObject inventoryCanvas = GameObject.Find("Inventory");
        if (inventoryCanvas == null)
        {
            EditorUtility.DisplayDialog("Setup Inventory UI",
                "Could not find a root GameObject named 'Inventory' in the active scene. Open World.unity first.",
                "OK");
            return;
        }

        EnsureAssetFolders();
        LoadFonts();
        s_panelSprite  = EnsureRoundedSprite(PanelSpritePath,  PanelCornerRadius);
        s_rowSprite    = EnsureRoundedSprite(RowSpritePath,    RowCornerRadius);
        s_buttonSprite = EnsureRoundedSprite(ButtonSpritePath, ButtonCornerRadius);

        GameObject rowPrefab = BuildOrReplaceRowPrefab();

        var canvas = inventoryCanvas.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = inventoryCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = 50;

        if (inventoryCanvas.GetComponent<GraphicRaycaster>() == null)
            inventoryCanvas.AddComponent<GraphicRaycaster>();

        ClearChildren(inventoryCanvas.transform);

        GameObject panelGroup = CreatePanelGroup(inventoryCanvas.transform);
        GameObject backdrop = CreateBackdrop(panelGroup.transform);
        GameObject panel = CreatePanel(panelGroup.transform);
        GameObject header = CreateHeader(panel.transform);
        GameObject columnHeader = CreateColumnHeader(panel.transform);
        GameObject rows = CreateRowsContainer(panel.transform);

        var controller = inventoryCanvas.GetComponent<InventoryPanelController>();
        if (controller == null)
            controller = inventoryCanvas.AddComponent<InventoryPanelController>();

        GameObject[] hideWhileOpen = FindHudWidgets();

        SetSerializedField(controller, "panelRoot", panelGroup);
        SetSerializedField(controller, "rowsParent", rows.transform);
        SetSerializedField(controller, "rowPrefab", rowPrefab.GetComponent<InventoryRowView>());
        SetSerializedFieldArray(controller, "uiToHideWhileOpen", hideWhileOpen);

        inventoryCanvas.SetActive(true);
        panelGroup.SetActive(false);

        var stage = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(stage);

        Debug.Log($"[SetupInventoryUI] Built Inventory panel under '{inventoryCanvas.name}' with {GlobalData.Inventory.Count} row(s). Prefab: {PrefabPath}");
    }

    static void EnsureAssetFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    static void LoadFonts()
    {
        s_fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRegularPath);
        s_fontBold    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBoldPath);
        if (s_fontRegular == null)
            Debug.LogWarning($"[SetupInventoryUI] Could not load font at '{FontRegularPath}'. Falling back to TMP default.");
        if (s_fontBold == null)
            Debug.LogWarning($"[SetupInventoryUI] Could not load font at '{FontBoldPath}'. Bold text will fall back to Regular with faux-bold.");
    }

    static GameObject[] FindHudWidgets()
    {
        string[] names = { "MinimapCircle", "MoneyCounter", "DayNightUI", "Pet Statistics" };
        var result = new System.Collections.Generic.List<GameObject>();
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go == null) continue;
            result.Add(go);
        }
        return result.ToArray();
    }

    static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    static GameObject CreatePanelGroup(Transform parent)
    {
        GameObject go = new GameObject("PanelGroup", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject CreateBackdrop(Transform parent)
    {
        GameObject go = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = BackdropColor;
        img.raycastTarget = true;
        return go;
    }

    static GameObject CreatePanel(Transform parent)
    {
        GameObject go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image img = go.GetComponent<Image>();
        img.color = PanelColor;
        ApplyRoundedSprite(img, s_panelSprite);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 22, 22);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return go;
    }

    static GameObject CreateHeader(Transform parent)
    {
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(parent, false);
        var le = header.AddComponent<LayoutElement>();
        le.minHeight = 56;
        le.preferredHeight = 56;

        var hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 0, 8);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        CreateText(header.transform, "Title", "Inventory", 32, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, TitleTextColor, flexW: 1f);
        CreateText(header.transform, "Hint", "Press G to close", 16, FontStyles.Italic,
            TextAlignmentOptions.MidlineRight, DimTextColor, flexW: 1f);
        return header;
    }

    static GameObject CreateColumnHeader(Transform parent)
    {
        GameObject col = new GameObject("ColumnHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        col.transform.SetParent(parent, false);
        Image img = col.GetComponent<Image>();
        img.color = HeaderRowColor;
        ApplyRoundedSprite(img, s_rowSprite);

        var le = col.AddComponent<LayoutElement>();
        le.minHeight = RowHeight;
        le.preferredHeight = RowHeight;

        var hlg = col.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(RowPadLeft, RowPadRight, 0, 0);
        hlg.spacing = RowSpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        AddColumnLabel(col.transform, "Item",   TextAlignmentOptions.MidlineLeft,   ColWidthName);
        AddColumnLabel(col.transform, "Amount", TextAlignmentOptions.Center,         ColWidthAmount);
        AddColumnLabel(col.transform, "Effect", TextAlignmentOptions.MidlineLeft,   ColWidthEffect);
        AddColumnLabel(col.transform, "Cost",   TextAlignmentOptions.MidlineRight,  ColWidthCost);
        AddColumnLabel(col.transform, "",       TextAlignmentOptions.Center,         ColWidthUse);

        return col;
    }

    static void AddColumnLabel(Transform parent, string text, TextAlignmentOptions align, float fixedWidth)
    {
        GameObject go = CreateText(parent, string.IsNullOrEmpty(text) ? "Action" : text,
            text, 14, FontStyles.Bold | FontStyles.UpperCase, align, DimTextColor);
        ApplyFixedWidth(go, fixedWidth);
    }

    static void ApplyFixedWidth(GameObject go, float width)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = 0f;
    }

    static GameObject CreateRowsContainer(Transform parent)
    {
        GameObject rows = new GameObject("Rows", typeof(RectTransform));
        rows.transform.SetParent(parent, false);

        var le = rows.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;

        var vlg = rows.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 6, 4);
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return rows;
    }

    static GameObject BuildOrReplaceRowPrefab()
    {
        GameObject root = new GameObject("InventoryRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            RectTransform rrt = (RectTransform)root.transform;
            rrt.sizeDelta = new Vector2(PanelWidth - 80, RowHeight);

            Image rowImg = root.GetComponent<Image>();
            rowImg.color = RowStripeColor;
            ApplyRoundedSprite(rowImg, s_rowSprite);

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = RowHeight;
            le.preferredHeight = RowHeight;

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(RowPadLeft, RowPadRight, 0, 0);
            hlg.spacing = RowSpacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            GameObject nameGO   = CreateText(root.transform, "NameText",   "Item Name",  18, FontStyles.Bold,   TextAlignmentOptions.MidlineLeft,  BodyTextColor);
            GameObject amountGO = CreateText(root.transform, "AmountText", "0",           18, FontStyles.Bold,   TextAlignmentOptions.Center,        BodyTextColor);
            GameObject effectGO = CreateText(root.transform, "EffectText", "+0 Hunger",   15, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,  EffectColor);
            GameObject costGO   = CreateText(root.transform, "CostText",   "$0",          17, FontStyles.Bold,   TextAlignmentOptions.MidlineRight, CostColor);
            ApplyFixedWidth(nameGO,   ColWidthName);
            ApplyFixedWidth(amountGO, ColWidthAmount);
            ApplyFixedWidth(effectGO, ColWidthEffect);
            ApplyFixedWidth(costGO,   ColWidthCost);
            GameObject buttonGO = CreateUseButton(root.transform);

            var row = root.AddComponent<InventoryRowView>();
            SetSerializedField(row, "nameText",   nameGO.GetComponent<TMP_Text>());
            SetSerializedField(row, "amountText", amountGO.GetComponent<TMP_Text>());
            SetSerializedField(row, "effectText", effectGO.GetComponent<TMP_Text>());
            SetSerializedField(row, "costText",   costGO.GetComponent<TMP_Text>());
            SetSerializedField(row, "useButton",  buttonGO.GetComponent<Button>());

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            return savedPrefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static GameObject CreateUseButton(Transform parent)
    {
        GameObject go = new GameObject("UseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = ColWidthUse;
        le.minWidth = ColWidthUse;
        le.flexibleWidth = 0f;
        le.preferredHeight = 36;
        le.minHeight = 36;

        Image img = go.GetComponent<Image>();
        img.color = UseNormalColor;
        ApplyRoundedSprite(img, s_buttonSprite);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = UseNormalColor;
        colors.highlightedColor = UseHoverColor;
        colors.pressedColor = UsePressColor;
        colors.selectedColor = UseHoverColor;
        colors.disabledColor = UseDisabledColor;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        CreateText(go.transform, "Text", "Use", 15, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white, stretch: true);

        return go;
    }

    static GameObject CreateText(Transform parent, string name, string text, float fontSize,
        FontStyles style, TextAlignmentOptions align, Color color,
        float flexW = 0f, bool stretch = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();

        // Pick the right font asset for the requested weight and strip the Bold flag so
        // TMP doesn't apply faux-bold on top of our actual bold SDF font.
        bool wantBold = (style & FontStyles.Bold) != 0;
        TMP_FontAsset chosen = (wantBold && s_fontBold != null) ? s_fontBold : s_fontRegular;
        if (chosen != null)
        {
            tmp.font = chosen;
            if (wantBold && s_fontBold != null)
                style &= ~FontStyles.Bold;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;

        if (stretch)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (flexW > 0f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexW;
        }

        return go;
    }

    static void ApplyRoundedSprite(Image img, Sprite sprite)
    {
        if (img == null) return;
        if (sprite == null) return;
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
    }

    /// <summary>
    /// Returns a 9-sliced rounded-rectangle sprite asset at <paramref name="pngPath"/>, generating it
    /// on first run. The generated texture is (2*radius+2) px on a side, white, with antialiased
    /// circular corners and a border of <paramref name="radius"/> px on all sides so Image.Type.Sliced
    /// preserves the curvature at any size.
    /// </summary>
    static Sprite EnsureRoundedSprite(string pngPath, int radius)
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        if (existing != null) return existing;

        int size = radius * 2 + 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = CornerAlpha(x, y, size, radius);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(pngPath, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            // 9-slice borders equal to the corner radius keep the rounded corners intact when stretched.
            ti.spriteBorder = new Vector4(radius, radius, radius, radius);
            ti.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

    /// <summary>
    /// Returns the alpha coverage for pixel (x,y) in a square texture whose corners are rounded to
    /// <paramref name="radius"/>. Uses a simple 2x2 supersample for anti-aliasing along the arc.
    /// Non-corner pixels return 1.
    /// </summary>
    static float CornerAlpha(int x, int y, int size, int radius)
    {
        int maxX = size - 1 - radius;
        int maxY = size - 1 - radius;

        float cx, cy;
        if (x < radius && y < radius)                 { cx = radius;        cy = radius; }
        else if (x > maxX && y < radius)              { cx = maxX;          cy = radius; }
        else if (x < radius && y > maxY)              { cx = radius;        cy = maxY; }
        else if (x > maxX && y > maxY)                { cx = maxX;          cy = maxY; }
        else return 1f;

        float covered = 0f;
        const int samples = 2;
        float r2 = radius * radius;
        for (int sy = 0; sy < samples; sy++)
        {
            for (int sx = 0; sx < samples; sx++)
            {
                float px = x + (sx + 0.5f) / samples;
                float py = y + (sy + 0.5f) / samples;
                float dx = px - cx;
                float dy = py - cy;
                if (dx * dx + dy * dy <= r2) covered += 1f;
            }
        }
        return covered / (samples * samples);
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[SetupInventoryUI] Missing serialized field '{fieldName}' on {target.GetType().Name}.");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetSerializedFieldArray(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[SetupInventoryUI] Missing serialized array field '{fieldName}' on {target.GetType().Name}.");
            return;
        }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = prop.GetArrayElementAtIndex(i);
            element.objectReferenceValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
