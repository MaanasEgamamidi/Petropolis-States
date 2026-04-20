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
/// </summary>
public static class SetupInventoryUI
{
    const string PrefabDir = "Assets/Prefabs/UI";
    const string PrefabPath = PrefabDir + "/InventoryRow.prefab";

    const int PanelWidth = 780;
    const int PanelHeight = 520;
    const int RowHeight = 46;

    static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.14f, 0.96f);
    static readonly Color RowStripeColor = new Color(1f, 1f, 1f, 0.04f);
    static readonly Color HeaderRowColor = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color BodyTextColor = new Color(0.93f, 0.93f, 0.95f, 1f);
    static readonly Color DimTextColor = new Color(0.72f, 0.72f, 0.78f, 1f);
    static readonly Color EffectColor = new Color(0.55f, 0.88f, 0.62f, 1f);
    static readonly Color CostColor = new Color(0.98f, 0.86f, 0.45f, 1f);
    static readonly Color UseNormalColor = new Color(0.22f, 0.55f, 0.95f, 1f);
    static readonly Color UseHoverColor = new Color(0.30f, 0.65f, 1f, 1f);
    static readonly Color UsePressColor = new Color(0.16f, 0.42f, 0.78f, 1f);
    static readonly Color UseDisabledColor = new Color(0.25f, 0.28f, 0.33f, 1f);

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
        GameObject emptyState = CreateEmptyStateLabel(panel.transform);

        var controller = inventoryCanvas.GetComponent<InventoryPanelController>();
        if (controller == null)
            controller = inventoryCanvas.AddComponent<InventoryPanelController>();

        GameObject[] hideWhileOpen = FindHudWidgets();

        SetSerializedField(controller, "panelRoot", panelGroup);
        SetSerializedField(controller, "rowsParent", rows.transform);
        SetSerializedField(controller, "rowPrefab", rowPrefab.GetComponent<InventoryRowView>());
        SetSerializedField(controller, "emptyStateLabel", emptyState.GetComponent<TMP_Text>());
        SetSerializedFieldArray(controller, "uiToHideWhileOpen", hideWhileOpen);

        inventoryCanvas.SetActive(true);
        panelGroup.SetActive(false);

        var stage = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(stage);

        Debug.Log($"[SetupInventoryUI] Built Inventory panel under '{inventoryCanvas.name}' with {GlobalData.Inventory.Count} row(s). Prefab: {PrefabPath}");
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

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 18);
        layout.spacing = 10;
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
        le.minHeight = 54;
        le.preferredHeight = 54;

        var hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 0, 6);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        CreateText(header.transform, "Title", "Inventory", 30, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, BodyTextColor, flexW: 1f);
        CreateText(header.transform, "Hint", "Press G to close", 16, FontStyles.Italic,
            TextAlignmentOptions.MidlineRight, DimTextColor, flexW: 1f);
        return header;
    }

    static GameObject CreateColumnHeader(Transform parent)
    {
        GameObject col = new GameObject("ColumnHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        col.transform.SetParent(parent, false);
        col.GetComponent<Image>().color = HeaderRowColor;

        var le = col.AddComponent<LayoutElement>();
        le.minHeight = RowHeight;
        le.preferredHeight = RowHeight;

        var hlg = col.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 0, 0);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        AddColumnLabel(col.transform, "Item",   TextAlignmentOptions.MidlineLeft,   2f);
        AddColumnLabel(col.transform, "Amount", TextAlignmentOptions.Center,         1f);
        AddColumnLabel(col.transform, "Effect", TextAlignmentOptions.MidlineLeft,   2f);
        AddColumnLabel(col.transform, "Cost",   TextAlignmentOptions.MidlineRight,  1f);
        AddColumnLabel(col.transform, "",       TextAlignmentOptions.Center,         1f);

        return col;
    }

    static void AddColumnLabel(Transform parent, string text, TextAlignmentOptions align, float flex)
    {
        CreateText(parent, string.IsNullOrEmpty(text) ? "Action" : text,
            text, 16, FontStyles.Bold, align, DimTextColor, flexW: flex);
    }

    static GameObject CreateRowsContainer(Transform parent)
    {
        GameObject rows = new GameObject("Rows", typeof(RectTransform));
        rows.transform.SetParent(parent, false);

        var le = rows.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;

        var vlg = rows.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 4, 4);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return rows;
    }

    static GameObject CreateEmptyStateLabel(Transform parent)
    {
        GameObject go = CreateText(parent, "EmptyState",
            "Your inventory is empty. Visit the supermarket owner to buy items.",
            18, FontStyles.Italic, TextAlignmentOptions.Center, DimTextColor, flexW: 1f);

        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = 48;

        go.SetActive(false);
        return go;
    }

    static GameObject BuildOrReplaceRowPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        GameObject root = new GameObject("InventoryRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            RectTransform rrt = (RectTransform)root.transform;
            rrt.sizeDelta = new Vector2(PanelWidth - 80, RowHeight);
            root.GetComponent<Image>().color = RowStripeColor;

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = RowHeight;
            le.preferredHeight = RowHeight;

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 0, 0);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            GameObject nameGO   = CreateText(root.transform, "NameText",   "Item Name",        18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,  BodyTextColor, flexW: 2f);
            GameObject amountGO = CreateText(root.transform, "AmountText", "0",                 18, FontStyles.Bold,   TextAlignmentOptions.Center,        BodyTextColor, flexW: 1f);
            GameObject effectGO = CreateText(root.transform, "EffectText", "+0 Hunger",         16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,  EffectColor,   flexW: 2f);
            GameObject costGO   = CreateText(root.transform, "CostText",   "$0",                18, FontStyles.Normal, TextAlignmentOptions.MidlineRight, CostColor,     flexW: 1f);
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
        le.preferredWidth = 110;
        le.minWidth = 90;
        le.preferredHeight = 36;
        le.minHeight = 36;

        Image img = go.GetComponent<Image>();
        img.color = UseNormalColor;

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

        CreateText(go.transform, "Text", "Use", 16, FontStyles.Bold,
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
            le.preferredHeight = fontSize + 8f;
        }

        return go;
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
