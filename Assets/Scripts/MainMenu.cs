using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Persistent runner for scene transitions. Survives scene loads so fade-out can complete.</summary>
public class FadeTransitionRunner : MonoBehaviour
{
    public static void RunFadeThenLoadScene(GameObject fadePanel, float fadeDuration, string sceneName)
    {
        CanvasGroup cg = fadePanel != null ? fadePanel.GetComponent<CanvasGroup>() : null;
        if (cg == null) return;

        GameObject root = new GameObject("FadeTransitionRoot");
        DontDestroyOnLoad(root);

        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(root.transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        fadePanel.transform.SetParent(canvasObj.transform, false);

        var runner = root.AddComponent<FadeTransitionRunner>();
        runner.StartCoroutine(runner.FadeOutFadeInAndLoadScene(cg, fadeDuration, sceneName));
    }

    public static void RunFadeThenQuit(GameObject fadePanel, float fadeDuration)
    {
        CanvasGroup cg = fadePanel != null ? fadePanel.GetComponent<CanvasGroup>() : null;
        if (cg == null) return;

        GameObject root = new GameObject("FadeTransitionRoot");
        DontDestroyOnLoad(root);

        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(root.transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        fadePanel.transform.SetParent(canvasObj.transform, false);

        var runner = root.AddComponent<FadeTransitionRunner>();
        runner.StartCoroutine(runner.FadeOutFadeInAndQuit(cg, fadeDuration));
    }

    System.Collections.IEnumerator FadeOutFadeInAndLoadScene(CanvasGroup cg, float duration, string sceneName)
    {
        cg.alpha = 0f;
        cg.gameObject.SetActive(true);

        float half = duration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / half);
            yield return null;
        }
        cg.alpha = 1f;

        SceneManager.LoadScene(sceneName);
        yield return null;

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / half);
            yield return null;
        }
        cg.alpha = 0f;
        Destroy(gameObject);
    }

    System.Collections.IEnumerator FadeOutFadeInAndQuit(CanvasGroup cg, float duration)
    {
        cg.alpha = 0f;
        cg.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

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

    void Start()
    {
        // Offset Dog_01 (09)'s animation by 1 second so both dogs don't animate in sync
        var dog09 = GameObject.Find("Dog_01 (09)");
        if (dog09 != null)
        {
            var animator = dog09.GetComponent<Animator>();
            if (animator != null) animator.Update(1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Play()
    {
        GameObject panel = fadeToBlackPanel;
        if (panel == null && canvas != null)
            panel = canvas.transform.Find("FadePanel")?.gameObject;
        if (panel != null && panel.GetComponent<CanvasGroup>() != null)
        {
            FadeTransitionRunner.RunFadeThenLoadScene(panel, 0.85f, "CharacterChoose");
            return;
        }
        SceneManager.LoadScene("CharacterChoose");
    }
    
    public void Settings()
    {
        
    }

    public void Quit()
    {
        GameObject panel = fadeToBlackPanel;
        if (panel == null && canvas != null)
            panel = canvas.transform.Find("FadePanel")?.gameObject;
        if (panel != null && panel.GetComponent<CanvasGroup>() != null)
        {
            FadeTransitionRunner.RunFadeThenQuit(panel, 0.85f);
            return;
        }
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
