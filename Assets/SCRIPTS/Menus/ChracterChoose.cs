using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the character-selection screen.
/// • Male / Female buttons show the correct 3-D character.
/// • 5 Body toggles activate one of the five body meshes on the active character.
/// • 5 Head toggles activate one of the five head meshes on the active character.
/// • Confirm saves the choices to PlayerPrefs and loads the next scene.
///
/// Hierarchy expected:
///   Characters
///     Male_all
///       Body_01  Body_02  Body_03  Body_04  Body_05
///       Head_01  Head_02  Head_03  Head_04  Head_05
///     Female_All
///       Body_01  Body_02  Body_03  Body_04  Body_05
///       Head_01  Head_02  Head_03  Head_04  Head_05
/// </summary>
public class ChracterChoose : MonoBehaviour
{
    // ─── Gender ──────────────────────────────────────────────────────────────
    [Header("Gender – 3-D Character Roots")]
    [Tooltip("Root of the Male character (Male_all).")]
    public GameObject male;
    [Tooltip("Root of the Female character (Female_All).")]
    public GameObject female;

    [Header("Gender – UI Buttons")]
    public Button maleButton;
    public Button femaleButton;

    // ─── Male body / head variants ────────────────────────────────────────────
    [Header("Male Body Options  (Body_01 → Body_05)")]
    public GameObject[] maleBodies = new GameObject[5];

    [Header("Male Head Options  (Head_01 → Head_05)")]
    public GameObject[] maleHeads = new GameObject[5];

    // ─── Female body / head variants ──────────────────────────────────────────
    [Header("Female Body Options  (Body_01 → Body_05)")]
    public GameObject[] femaleBodies = new GameObject[5];

    [Header("Female Head Options  (Head_01 → Head_05)")]
    public GameObject[] femaleHeads = new GameObject[5];

    // ─── UI Toggles ──────────────────────────────────────────────────────────
    [Header("Body Toggles  (Option1 → Option5)")]
    public Toggle[] bodyToggles = new Toggle[5];

    [Header("Head Toggles  (Option1 → Option5)")]
    public Toggle[] headToggles = new Toggle[5];

    // ─── Confirm ─────────────────────────────────────────────────────────────
    [Header("Confirm")]
    public Button confirmButton;
    [Tooltip("TMP InputField for the player's name.")]
    public TMP_InputField nameInputField;
    [Tooltip("Name of the scene to load after confirming.")]
    public string nextSceneName = "SampleScene";

    // ─── State ───────────────────────────────────────────────────────────────
    /// <summary>0 = Male, 1 = Female.</summary>
    public int selectedGender = 0;
    /// <summary>0-based body index (0–4).</summary>
    public int selectedBody = 0;
    /// <summary>0-based head index (0–4).</summary>
    public int selectedHead = 0;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Gender buttons
        if (maleButton   != null) maleButton.onClick.AddListener(SelectMale);
        if (femaleButton != null) femaleButton.onClick.AddListener(SelectFemale);

        // Body toggles
        for (int i = 0; i < bodyToggles.Length; i++)
        {
            int idx = i;
            if (bodyToggles[i] != null)
                bodyToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn) OnBodyToggled(idx);
                });
        }

        // Head toggles
        for (int i = 0; i < headToggles.Length; i++)
        {
            int idx = i;
            if (headToggles[i] != null)
                headToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn) OnHeadToggled(idx);
                });
        }

        // Confirm button
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    void Start()
    {
        // Apply the default selections on scene load
        RefreshAll();
    }

    // ─── Gender ───────────────────────────────────────────────────────────────

    /// <summary>Called by the Male UI button.</summary>
    public void SelectMale()
    {
        selectedGender = 0;
        RefreshAll();
    }

    /// <summary>Called by the Female UI button.</summary>
    public void SelectFemale()
    {
        selectedGender = 1;
        RefreshAll();
    }

    // ─── Toggle callbacks ────────────────────────────────────────────────────

    void OnBodyToggled(int index)
    {
        selectedBody = index;
        RefreshBodies();
    }

    void OnHeadToggled(int index)
    {
        selectedHead = index;
        RefreshHeads();
    }

    // ─── Confirm ─────────────────────────────────────────────────────────────

    /// <summary>Saves the selection and loads the gameplay scene.</summary>
    public void OnConfirm()
    {
        string genderStr = selectedGender == 0 ? "male" : "female";
        int bodyOneBased = selectedBody + 1; // GlobalData uses 1-5
        int headOneBased = selectedHead + 1;
        string playerName = (nameInputField != null) ? nameInputField.text : "";

        GlobalData.SaveChoice(genderStr, bodyOneBased, headOneBased, playerName);
        GlobalData.ScheduleWorldWelcomeNotification(3f);

        PlayerPrefs.SetInt("SelectedGender", selectedGender);
        PlayerPrefs.SetInt("SelectedBody",   selectedBody);
        PlayerPrefs.SetInt("SelectedHead",   selectedHead);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // ─── Refresh helpers ─────────────────────────────────────────────────────

    /// <summary>Refreshes the entire character display (gender + body + head).</summary>
    void RefreshAll()
    {
        bool isMale = selectedGender == 0;

        // Show the right gender root
        if (male   != null) male.SetActive(isMale);
        if (female != null) female.SetActive(!isMale);

        RefreshBodies();
        RefreshHeads();
    }

    /// <summary>
    /// Activates only the chosen body variant on the currently visible character.
    /// The inactive character's bodies are also kept in sync so that switching
    /// gender never shows a stale combination.
    /// </summary>
    void RefreshBodies()
    {
        ActivateOnly(maleBodies,   selectedBody);
        ActivateOnly(femaleBodies, selectedBody);
    }

    /// <summary>Same as RefreshBodies but for heads.</summary>
    void RefreshHeads()
    {
        ActivateOnly(maleHeads,   selectedHead);
        ActivateOnly(femaleHeads, selectedHead);
    }

    /// <summary>
    /// Sets each element of <paramref name="options"/> active/inactive
    /// so that only index <paramref name="activeIndex"/> is active.
    /// </summary>
    static void ActivateOnly(GameObject[] options, int activeIndex)
    {
        if (options == null) return;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null)
                options[i].SetActive(i == activeIndex);
        }
    }
}