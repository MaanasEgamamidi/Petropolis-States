using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SetupUI : EditorWindow
{
    [MenuItem("Tools/Setup World UI")]
    public static void Setup()
    {
        // Find the UI Canvas
        GameObject canvasObj = GameObject.Find("UI");
        if (canvasObj == null)
        {
            Debug.LogError("Could not find GameObject named 'UI'.");
            return;
        }

        // --- Money Counter (Top Left) ---
        GameObject moneyCounter = new GameObject("Money Counter");
        moneyCounter.transform.SetParent(canvasObj.transform, false);
        RectTransform moneyRect = moneyCounter.AddComponent<RectTransform>();
        
        // Anchor Top Left
        moneyRect.anchorMin = new Vector2(0, 1);
        moneyRect.anchorMax = new Vector2(0, 1);
        moneyRect.pivot = new Vector2(0, 1);
        moneyRect.anchoredPosition = new Vector2(20, -20);
        moneyRect.sizeDelta = new Vector2(250, 50);

        Text moneyText = moneyCounter.AddComponent<Text>();
        moneyText.text = "$150.00";
        moneyText.fontSize = 32;
        moneyText.fontStyle = FontStyle.Bold;
        moneyText.alignment = TextAnchor.UpperLeft;
        // Use Ariel default
        moneyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        moneyText.color = Color.white;
        
        // Add shadow for visibility
        Outline moneyShadow = moneyCounter.AddComponent<Outline>();
        moneyShadow.effectColor = Color.black;
        moneyShadow.effectDistance = new Vector2(2, -2);


        // --- Pet Stats Panel (Top Right) ---
        GameObject petStatsPanel = new GameObject("Pet Stats Panel");
        petStatsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = petStatsPanel.AddComponent<RectTransform>();
        
        // Anchor Top Right
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(250, 300);

        // Add a background image
        Image panelImage = petStatsPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);

        // Vertical Layout Group
        VerticalLayoutGroup vlg = petStatsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // Helper function to create stat text
        GameObject CreateStatText(string name, string defaultText, int fontSize = 20, FontStyle style = FontStyle.Normal)
        {
            GameObject statObj = new GameObject(name);
            statObj.transform.SetParent(petStatsPanel.transform, false);
            Text t = statObj.AddComponent<Text>();
            t.text = defaultText;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            
            RectTransform rt = statObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, fontSize + 10);
            return statObj;
        }

        CreateStatText("Pet Name Label", "Max (Dog)", 24, FontStyle.Bold);
        CreateStatText("Hunger Text", "Hunger: 100%");
        CreateStatText("Health Text", "Health: 100%");
        CreateStatText("Happiness Text", "Happiness: 100%");
        CreateStatText("Energy Text", "Energy: 100%");
        CreateStatText("Cleanliness Text", "Cleanliness: 100%");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("World UI Setup Complete!");
    }
}
