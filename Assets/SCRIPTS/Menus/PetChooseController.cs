using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Pet name")]
    [Tooltip("TMP field for the dog's name. Required when Edit > Mandatory Name Selection is on.")]
    [SerializeField] TMP_InputField petNameInputField;

    [Header("Fade to World")]
    [Tooltip("The fade black panel (must have CanvasGroup). FadeCanvas or a child with CanvasGroup.")]
    [SerializeField] GameObject fadePanel;
    [Tooltip("Scene to load after confirm (e.g. World).")]
    [SerializeField] string nextSceneName = "World";

    const int MIN_INDEX = 1;
    const int MAX_INDEX = 6;
    int _currentIndex = 1;

    CanvasGroup _confirmDimGroup;

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
        {
            _confirmDimGroup = ButtonStyleHelper.EnsureDimCanvasGroup(confirmButton);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        if (petNameInputField != null)
            petNameInputField.onValueChanged.AddListener(OnPetNameFieldChanged);
    }

    void Start()
    {
        ApplyMaterial(_currentIndex);
        RefreshConfirmInteractable();
    }

    void OnPetNameFieldChanged(string _) => RefreshConfirmInteractable();

    void RefreshConfirmInteractable()
    {
        if (confirmButton == null)
            return;
        bool allow = !NameSelectionSettings.IsMandatory
            || !string.IsNullOrWhiteSpace((petNameInputField != null) ? petNameInputField.text : "");
        ButtonStyleHelper.ApplyInteractableWithDim(confirmButton, _confirmDimGroup, allow);
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
        string petName = (petNameInputField != null) ? petNameInputField.text : "";
        if (NameSelectionSettings.IsMandatory && string.IsNullOrWhiteSpace(petName))
            return;

        GlobalData.PetName = string.IsNullOrWhiteSpace(petName) ? "Dog" : petName.Trim();
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
