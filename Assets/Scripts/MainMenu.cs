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
    [Tooltip("Drag your full-screen black image here for quit fade. It must have a CanvasGroup component.")]
    [SerializeField] GameObject fadeToBlackPanel;
    [Tooltip("Define presets, then call HideUnchosen() for preset 0 or HideUnchosenWithPreset(0), (1), (2)... from buttons.")]
    [SerializeField] KeepVisiblePreset[] presets;

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
        GameObject panel = fadeToBlackPanel;
        if (panel == null && canvas != null)
            panel = canvas.transform.Find("FadePanel")?.gameObject;
        if (panel != null)
        {
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                panel.SetActive(true);
                StartCoroutine(FadeToBlackAndQuit(cg, 1f));
                return;
            }
        }
        // If no fade panel assigned or no CanvasGroup, quit immediately
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private System.Collections.IEnumerator FadeToBlackAndQuit(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
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

            if (isBackground || isInList)
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
