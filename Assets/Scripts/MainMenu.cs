using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class KeepVisiblePreset
{
    public string name;
    public bool fade;
    public GameObject[] gameObjects;
}

public class MainMenu : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Image fadeImage;
    [Tooltip("Duration in seconds for each fade (to black and back). Used by any preset that has Fade enabled.")]
    [SerializeField] float fadeSpeed = 0.5f;
    [Tooltip("Define presets, then call HideUnchosen() for preset 0 or HideUnchosenWithPreset(0), (1), (2)... from buttons.")]
    [SerializeField] KeepVisiblePreset[] presets;

    const float FadeHoldTime = 0.15f;

    void Start()
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
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
        KeepVisiblePreset preset = presets[presetIndex];
        if (preset.fade && fadeImage != null)
            StartCoroutine(FadeThenApplyPreset(preset));
        else
            HideUnchosenWithList(preset.gameObjects);
    }

    System.Collections.IEnumerator FadeThenApplyPreset(KeepVisiblePreset preset)
    {
        float duration = Mathf.Max(0.01f, fadeSpeed);
        fadeImage.gameObject.SetActive(true);

        // Fade to black (0 → 1) over duration
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f);

        yield return new WaitForSeconds(FadeHoldTime);

        HideUnchosenWithList(preset.gameObjects);

        // Fade back to transparent (1 → 0), same duration and curve reversed
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.gameObject.SetActive(false);
    }

    void HideUnchosenWithList(GameObject[] toKeepVisible)
    {
        if (canvas == null) return;

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (fadeImage != null && child == fadeImage.transform)
                continue;

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
