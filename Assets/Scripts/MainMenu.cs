using UnityEngine;

[System.Serializable]
public class KeepVisiblePreset
{
    public string name;
    public GameObject[] gameObjects;
}

public class MainMenu : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [Tooltip("Define presets, then call HideUnchosen() for preset 0 or HideUnchosenWithPreset(0), (1), (2)... from buttons.")]
    [SerializeField] KeepVisiblePreset[] presets;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Play()
    {
        
    }
    
    public void Settings()
    {
        
    }

    public void Quit()
    {
        
    }

    /// <summary>
    /// Hides all children of Canvas except "Background" and the GameObjects in preset 0.
    /// </summary>
    public void HideUnchosen()
    {
        HideUnchosenWithPreset(0);
    }

    /// <summary>
    /// Same as HideUnchosen but uses the preset at the given index. Use from buttons: OnClick → HideUnchosenWithPreset and set the int (0, 1, 2...) in the inspector.
    /// </summary>
    public void HideUnchosenWithPreset(int presetIndex)
    {
        if (presets == null || presetIndex < 0 || presetIndex >= presets.Length) return;
        HideUnchosenWithList(presets[presetIndex].gameObjects);
    }

    void HideUnchosenWithList(GameObject[] toKeepVisible)
    {
        if (canvas == null) return;

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            bool isBackground = child.name == "Background";
            bool isInList = toKeepVisible != null && System.Array.Exists(toKeepVisible, go => go != null && go.transform == child);

            if (!isBackground && !isInList)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
