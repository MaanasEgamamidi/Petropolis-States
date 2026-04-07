using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Static audio volumes persisted via PlayerPrefs. Music/SFX sliders control these. Uses dB conversion for natural perceived volume.</summary>
public static class AudioSettings
{
    const string PREFS_MUSIC = "Petropolis_MusicVolume";
    const string PREFS_SFX = "Petropolis_SFXVolume";

    static float _musicVolumeRaw = -1f;
    static float _sfxVolumeRaw = -1f;

    /// <summary>Converts slider value (0–1) to linear volume. Uses -24dB at bottom so 1/3 slider ≈ 15% volume.</summary>
    public static float SliderToLinear(float sliderValue)
    {
        if (sliderValue <= 0f) return 0f;
        return Mathf.Pow(10f, (sliderValue * 24f - 24f) / 20f);
    }

    public static float MusicVolumeRaw
    {
        get { if (_musicVolumeRaw < 0) _musicVolumeRaw = PlayerPrefs.GetFloat(PREFS_MUSIC, 1f); return _musicVolumeRaw; }
        set { _musicVolumeRaw = Mathf.Clamp01(value); PlayerPrefs.SetFloat(PREFS_MUSIC, _musicVolumeRaw); PlayerPrefs.Save(); }
    }

    public static float SFXVolumeRaw
    {
        get { if (_sfxVolumeRaw < 0) _sfxVolumeRaw = PlayerPrefs.GetFloat(PREFS_SFX, 1f); return _sfxVolumeRaw; }
        set { _sfxVolumeRaw = Mathf.Clamp01(value); PlayerPrefs.SetFloat(PREFS_SFX, _sfxVolumeRaw); PlayerPrefs.Save(); }
    }

    /// <summary>Linear volume for music playback (0–1, dB-converted).</summary>
    public static float MusicVolume => SliderToLinear(MusicVolumeRaw);

    /// <summary>Linear volume for SFX playback (0–1, dB-converted).</summary>
    public static float SFXVolume => SliderToLinear(SFXVolumeRaw);
}

/// <summary>Persistent runner for scene transitions. Survives scene loads so fade-out can complete.</summary>
public class FadeTransitionRunner : MonoBehaviour
{
    /// <summary>Full-screen black image with CanvasGroup; use with <see cref="RunFadeThenLoadScene"/> / <see cref="RunFadeThenQuit"/> when no prefab exists in the scene.</summary>
    public static GameObject CreateFullScreenBlackFadePanel()
    {
        var go = new GameObject("FadePanel");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        return go;
    }

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

    public static void RunFadeThenLoadSceneAdditive(GameObject fadePanel, float fadeDuration, string sceneName)
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
        runner.StartCoroutine(runner.FadeOutFadeInAndLoadSceneAdditive(cg, fadeDuration, sceneName));
    }

    public static void RunFadeThenUnloadScene(GameObject fadePanel, float fadeDuration, string sceneName)
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
        runner.StartCoroutine(runner.FadeOutFadeInAndUnloadScene(cg, fadeDuration, sceneName));
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

    System.Collections.IEnumerator FadeOutFadeInAndLoadSceneAdditive(CanvasGroup cg, float duration, string sceneName)
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

        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
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

    System.Collections.IEnumerator FadeOutFadeInAndUnloadScene(CanvasGroup cg, float duration, string sceneName)
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

        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op != null)
                while (!op.isDone)
                    yield return null;
        }
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

    [Header("Audio")]
    [Tooltip("Music slider. Found by name 'MusicSlider' if not assigned.")]
    [SerializeField] Slider musicSlider;
    [Tooltip("SFX slider. Found by name 'SFXSlider' if not assigned.")]
    [SerializeField] Slider sfxSlider;
    [Tooltip("Gameplay minimap mode (Circle / Square / None). Found under Canvas if not assigned.")]
    [SerializeField] TMP_Dropdown minimapModeDropdown;
    [Tooltip("Pet info panel (Always on / Toggle Q / Hold Q). Found under Canvas if not assigned.")]
    [SerializeField] TMP_Dropdown petInfoModeDropdown;
    [Tooltip("Length of one full in-game day in real minutes (digits only). Found by name LengthOfDayInput if not assigned.")]
    [SerializeField] TMP_InputField lengthOfDayInput;
    [Tooltip("Background music clip (e.g. Assets/AUDIO/background_music.mp3).")]
    [SerializeField] AudioClip backgroundMusicClip;

    static AudioSource _musicSource;

    void Start()
    {
        // Offset Dog_01 (09)'s animation by 1 second so both dogs don't animate in sync
        var dog09 = GameObject.Find("Dog_01 (09)");
        if (dog09 != null)
        {
            var animator = dog09.GetComponent<Animator>();
            if (animator != null) animator.Update(1f);
        }

        SetupAudio();
        SetupMinimapDropdown();
        SetupPetInfoDropdown();
        SetupLengthOfDayInput();
    }

    void SetupMinimapDropdown()
    {
        if (minimapModeDropdown == null && canvas != null)
        {
            foreach (var dd in canvas.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                if (dd.gameObject.name == "MinimapDropdown" || dd.gameObject.name == "Dropdown")
                {
                    minimapModeDropdown = dd;
                    break;
                }
            }
        }

        if (minimapModeDropdown == null)
            return;

        // Reflect the current mode (default Circle if never saved).
        int displayIndex = Mathf.Clamp((int)MinimapSettings.Mode, 0, minimapModeDropdown.options.Count - 1);
        minimapModeDropdown.SetValueWithoutNotify(displayIndex);
        minimapModeDropdown.onValueChanged.RemoveListener(OnMinimapModeDropdownChanged);
        minimapModeDropdown.onValueChanged.AddListener(OnMinimapModeDropdownChanged);
    }

    void OnMinimapModeDropdownChanged(int index)
    {
        MinimapSettings.Mode = (MinimapMode)Mathf.Clamp(index, 0, 2);
    }

    void SetupPetInfoDropdown()
    {
        if (petInfoModeDropdown == null && canvas != null)
        {
            foreach (var dd in canvas.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                if (dd.gameObject.name == "PetInfoDropdown")
                {
                    petInfoModeDropdown = dd;
                    break;
                }
            }
        }

        if (petInfoModeDropdown == null)
            return;

        int saved = (int)PetInfoSettings.Mode;
        petInfoModeDropdown.SetValueWithoutNotify(Mathf.Clamp(saved, 0, petInfoModeDropdown.options.Count - 1));
        petInfoModeDropdown.onValueChanged.RemoveListener(OnPetInfoModeDropdownChanged);
        petInfoModeDropdown.onValueChanged.AddListener(OnPetInfoModeDropdownChanged);
    }

    void OnPetInfoModeDropdownChanged(int index)
    {
        PetInfoSettings.Mode = (PetInfoDisplayMode)Mathf.Clamp(index, 0, 2);
    }

    void SetupLengthOfDayInput()
    {
        if (lengthOfDayInput == null && canvas != null)
        {
            foreach (var field in canvas.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (field.gameObject.name == "LengthOfDayInput")
                {
                    lengthOfDayInput = field;
                    break;
                }
            }
        }

        if (lengthOfDayInput == null)
            return;

        lengthOfDayInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        lengthOfDayInput.characterValidation = TMP_InputField.CharacterValidation.Digit;
        lengthOfDayInput.characterLimit = 3;
        lengthOfDayInput.onValidateInput = OnLengthOfDayValidateChar;
        lengthOfDayInput.onValueChanged.RemoveListener(OnLengthOfDayValueChanged);
        lengthOfDayInput.onValueChanged.AddListener(OnLengthOfDayValueChanged);
        lengthOfDayInput.onEndEdit.RemoveListener(OnLengthOfDayEndEdit);
        lengthOfDayInput.onEndEdit.AddListener(OnLengthOfDayEndEdit);

        lengthOfDayInput.SetTextWithoutNotify(DayNightSettings.GetMenuDisplayMinutes());
    }

    static char OnLengthOfDayValidateChar(string text, int charIndex, char addedChar)
    {
        return char.IsDigit(addedChar) ? addedChar : '\0';
    }

    void OnLengthOfDayValueChanged(string text)
    {
        if (lengthOfDayInput == null || string.IsNullOrEmpty(text))
            return;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]))
                sb.Append(text[i]);
        }

        string filtered = sb.ToString();
        if (filtered == text)
            return;

        int caret = lengthOfDayInput.caretPosition;
        lengthOfDayInput.SetTextWithoutNotify(filtered);
        lengthOfDayInput.caretPosition = Mathf.Min(caret, filtered.Length);
    }

    void OnLengthOfDayEndEdit(string text)
    {
        if (lengthOfDayInput == null)
            return;

        if (string.IsNullOrEmpty(text?.Trim()))
        {
            lengthOfDayInput.SetTextWithoutNotify(DayNightSettings.GetMenuDisplayMinutes());
            return;
        }

        if (!int.TryParse(text.Trim(), out int minutes) || minutes < (int)DayNightSettings.MinMinutes)
        {
            lengthOfDayInput.SetTextWithoutNotify(DayNightSettings.GetMenuDisplayMinutes());
            return;
        }

        minutes = Mathf.Clamp(minutes, (int)DayNightSettings.MinMinutes, (int)DayNightSettings.MaxMinutes);
        DayNightSettings.SetFullCycleMinutes(minutes);
        lengthOfDayInput.SetTextWithoutNotify(minutes.ToString());
    }

    void SetupAudio()
    {
        if (musicSlider == null)
        {
            musicSlider = GameObject.Find("MusicSlider")?.GetComponent<Slider>();
            if (musicSlider == null && canvas != null)
            {
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                    if (t.name == "MusicSlider") { musicSlider = t.GetComponent<Slider>(); break; }
            }
        }
        if (sfxSlider == null)
        {
            sfxSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();
            if (sfxSlider == null && canvas != null)
            {
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                    if (t.name == "SFXSlider") { sfxSlider = t.GetComponent<Slider>(); break; }
            }
        }

        if (musicSlider != null)
        {
            musicSlider.value = AudioSettings.MusicVolumeRaw;
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = AudioSettings.SFXVolumeRaw;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (backgroundMusicClip != null && _musicSource == null)
        {
            var go = new GameObject("MusicPlayer");
            _musicSource = go.AddComponent<AudioSource>();
            _musicSource.clip = backgroundMusicClip;
            _musicSource.loop = true;
            _musicSource.volume = AudioSettings.MusicVolume;
            _musicSource.Play();
            DontDestroyOnLoad(go);
        }
        else if (_musicSource != null)
        {
            _musicSource.volume = AudioSettings.MusicVolume;
        }
    }

    public void SetMusicVolume(float value)
    {
        AudioSettings.MusicVolumeRaw = value;
        if (_musicSource != null) _musicSource.volume = AudioSettings.MusicVolume;
    }

    /// <summary>Called when sliders change music volume outside this component (e.g. pause menu).</summary>
    public static void RefreshMusicVolumeFromSettings()
    {
        if (_musicSource != null) _musicSource.volume = AudioSettings.MusicVolume;
    }

    public void SetSFXVolume(float value)
    {
        AudioSettings.SFXVolumeRaw = value;
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
            FadeTransitionRunner.RunFadeThenLoadScene(panel, 0.85f, "Instructions");
            return;
        }
        SceneManager.LoadScene("Instructions");
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
