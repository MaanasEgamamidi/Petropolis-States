using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Back/Next buttons in PetChoose scene to cycle through dog materials (Dog_01–Dog_06).
/// Confirm saves the choice to GlobalData and fades to World scene.
/// </summary>
public class PetChooseController : MonoBehaviour
{
    [Tooltip("The dog's SkinnedMeshRenderer. Auto-finds if not assigned.")]
    [SerializeField] SkinnedMeshRenderer dogRenderer;

    [Tooltip("Dog materials in order: Dog_01, Dog_02, Dog_03, Dog_04, Dog_05, Dog_06.")]
    [SerializeField] Material[] dogMaterials = new Material[6];

    [Header("Optional – auto-found if not assigned")]
    [SerializeField] Button backButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button confirmButton;

    [Header("Fade to World")]
    [Tooltip("The fade black panel (must have CanvasGroup). FadeCanvas or a child with CanvasGroup.")]
    [SerializeField] GameObject fadePanel;
    [Tooltip("Scene to load after confirm (e.g. World).")]
    [SerializeField] string nextSceneName = "World";

    const int MIN_INDEX = 1;
    const int MAX_INDEX = 6;
    int _currentIndex = 1;

    void Awake()
    {
        if (dogRenderer == null)
        {
            var sr = FindFirstObjectByType<SkinnedMeshRenderer>();
            if (sr != null)
                dogRenderer = sr;
        }

        if (backButton == null)
            backButton = GameObject.Find("Back")?.GetComponent<Button>();
        if (nextButton == null)
            nextButton = GameObject.Find("Next")?.GetComponent<Button>();
        if (confirmButton == null)
            confirmButton = GameObject.Find("Confirm")?.GetComponent<Button>();

        if (fadePanel == null)
        {
            var fadeCanvas = GameObject.Find("FadeCanvas");
            if (fadeCanvas != null)
            {
                var cg = fadeCanvas.GetComponent<CanvasGroup>();
                if (cg != null)
                    fadePanel = fadeCanvas;
                else
                    fadePanel = fadeCanvas.transform.Find("FadePanel")?.gameObject ?? fadeCanvas.GetComponentInChildren<CanvasGroup>(true)?.gameObject;
            }
            if (fadePanel == null)
                fadePanel = GameObject.Find("FadePanel");
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    void Start()
    {
        ApplyMaterial(_currentIndex);
    }

    public void OnBackClicked()
    {
        _currentIndex = _currentIndex <= MIN_INDEX ? MAX_INDEX : _currentIndex - 1;
        ApplyMaterial(_currentIndex);
    }

    public void OnNextClicked()
    {
        _currentIndex = _currentIndex >= MAX_INDEX ? MIN_INDEX : _currentIndex + 1;
        ApplyMaterial(_currentIndex);
    }

    public void OnConfirmClicked()
    {
        GlobalData.SavePetChoice(_currentIndex);

        if (GlobalData.PetChooseOpenedFromWorld)
        {
            GlobalData.PetChooseOpenedFromWorld = false;
            if (fadePanel != null && fadePanel.GetComponent<CanvasGroup>() != null)
            {
                FadeTransitionRunner.RunFadeThenUnloadScene(fadePanel, 0.85f, "PetChoose");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("PetChoose");
            }
        }
        else
        {
            if (fadePanel != null && fadePanel.GetComponent<CanvasGroup>() != null)
            {
                FadeTransitionRunner.RunFadeThenLoadScene(fadePanel, 0.85f, nextSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }
    }

    void ApplyMaterial(int oneBasedIndex)
    {
        if (dogRenderer == null || dogMaterials == null) return;

        int i = Mathf.Clamp(oneBasedIndex, 1, 6) - 1;
        if (i >= 0 && i < dogMaterials.Length && dogMaterials[i] != null)
        {
            var mats = dogRenderer.materials;
            if (mats.Length > 0)
            {
                mats[0] = dogMaterials[i];
                dogRenderer.materials = mats;
            }
        }
    }
}
