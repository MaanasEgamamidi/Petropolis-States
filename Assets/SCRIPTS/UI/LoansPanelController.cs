using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// World: press <b>L</b> (keyboard) or <b>D-pad Right</b> / <b>right trigger (RT)</b> (gamepad) to toggle the loans panel. Opening hides the minimap, money counter,
/// day-night clock, and pet-statistics HUD widgets; closing restores them. Mirrors the
/// <see cref="InventoryPanelController"/> pattern. Live numbers come from the loan fields on
/// <see cref="GlobalData"/>; when no loan is active the body collapses to a "Visit the bank..."
/// hint and the Pay Off button is hidden.
/// </summary>
public class LoansPanelController : MonoBehaviour
{
    const string WorldSceneName = "World";

    [Tooltip("Root GameObject of the loans panel (backdrop + window). Gets SetActive(true/false) on open/close.")]
    [SerializeField] GameObject panelRoot;

    [Header("Active Loan Fields (optional — all null-safe)")]
    [Tooltip("Header / big title line.")]
    [SerializeField] TMP_Text titleText;
    [Tooltip("Short status word: 'No active loan' or 'Active loan'. Goes next to the title.")]
    [SerializeField] TMP_Text statusText;
    [Tooltip("Multi-line breakdown (Owed, Days left, Interest, Principal). Filled as a single string when any per-field label below is null.")]
    [SerializeField] TMP_Text detailsText;
    [Tooltip("If set, overrides the principal line in detailsText.")]
    [SerializeField] TMP_Text principalText;
    [Tooltip("If set, overrides the amount-owed line in detailsText.")]
    [SerializeField] TMP_Text amountOwedText;
    [Tooltip("If set, overrides the days-remaining line in detailsText.")]
    [SerializeField] TMP_Text daysRemainingText;
    [Tooltip("If set, overrides the interest-rate line in detailsText.")]
    [SerializeField] TMP_Text interestText;
    [Tooltip("Bottom-of-panel hint / warning text.")]
    [SerializeField] TMP_Text hintText;
    [SerializeField] Button payOffButton;
    [SerializeField] TMP_Text payOffButtonLabel;

    [Tooltip("HUD widgets to hide while the loans panel is open (mirror InventoryPanelController). Their previous active state is restored on close.")]
    [SerializeField] GameObject[] uiToHideWhileOpen;

    readonly List<bool> _uiWasActive = new List<bool>();

    /// <summary>True while the loans panel is open. Other systems may check this to gate input.</summary>
    public static bool IsOpen { get; private set; }

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        IsOpen = false;

        if (payOffButton != null)
        {
            payOffButton.onClick.RemoveAllListeners();
            payOffButton.onClick.AddListener(OnPayOffClicked);
        }
    }

    void OnEnable()
    {
        GlobalData.OnLoanChanged   += HandleLoanChanged;
        GlobalData.OnMoneyChanged  += HandleMoneyChanged;
    }

    void OnDisable()
    {
        GlobalData.OnLoanChanged   -= HandleLoanChanged;
        GlobalData.OnMoneyChanged  -= HandleMoneyChanged;
        if (IsOpen)
        {
            RestoreCursorAfterMenu();
            IsOpen = false;
        }
    }

    void HandleLoanChanged() => RefreshAll();
    void HandleMoneyChanged(float _) => RefreshPayButtonOnly();

    void Update()
    {
        if (!Application.isPlaying) return;
        if (SceneManager.GetActiveScene().name != WorldSceneName) return;
        if (WorldPauseController.IsPaused) return;
        if (NpcProximityChatOpener.IsWorldNpcChatOpen)
        {
            if (IsOpen) Close();
            return;
        }
        if (IsTypingInFocusedInput()) return;

        if (IsOpen && CloseKeyPressedThisFrame())
        {
            Close();
            return;
        }

        if (!TogglePressedThisFrame()) return;

        if (IsOpen) Close();
        else Open();
    }

    void LateUpdate()
    {
        if (IsOpen)
            UnlockCursorForMenu();
    }

    void Open()
    {
        HideHudWidgets();
        if (panelRoot != null)
            panelRoot.SetActive(true);
        IsOpen = true;
        UnlockCursorForMenu();
        RefreshAll();
    }

    void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        RestoreHudWidgets();
        RestoreCursorAfterMenu();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        IsOpen = false;
    }

    void OnPayOffClicked()
    {
        if (!GlobalData.HasActiveLoan) return;
        if (GlobalData.TryRepayLoan())
        {
            RefreshAll();
        }
    }

    void RefreshAll()
    {
        if (panelRoot == null) return;

        bool active = GlobalData.HasActiveLoan;

        if (titleText != null)
            titleText.text = "Bank Loans";

        if (statusText != null)
            statusText.text = active ? "Active loan" : "No active loan";

        if (active)
        {
            float owed = GlobalData.LoanAmountOwed;
            int   days = GlobalData.LoanDaysRemaining;
            int   term = GlobalData.LoanTermDays;
            float prin = GlobalData.LoanPrincipal;
            int   pct  = Mathf.RoundToInt(GlobalData.LoanInterestRate * 100f);
            string owedLine = $"Owed: ${owed:0}";
            string daysLine = days == 1 ? "Days left: 1" : $"Days left: {days} / {term}";
            string intLine  = $"Interest: {pct}%";
            string prinLine = $"Principal: ${prin:0}";

            if (amountOwedText    != null) amountOwedText.text    = owedLine;
            if (daysRemainingText != null) daysRemainingText.text = daysLine;
            if (interestText      != null) interestText.text      = intLine;
            if (principalText     != null) principalText.text     = prinLine;
            if (detailsText       != null) detailsText.text       = $"{owedLine}\n{daysLine}\n{intLine}\n{prinLine}";
            if (hintText          != null) hintText.text          = days <= 1
                ? $"Pay off today or the bank takes {GlobalData.PetName}."
                : $"Pay off in full before the term ends, or the bank takes {GlobalData.PetName}.";
        }
        else
        {
            if (amountOwedText    != null) amountOwedText.text    = string.Empty;
            if (daysRemainingText != null) daysRemainingText.text = string.Empty;
            if (interestText      != null) interestText.text      = string.Empty;
            if (principalText     != null) principalText.text     = string.Empty;
            if (detailsText       != null) detailsText.text       = string.Empty;
            if (hintText          != null) hintText.text          = "Visit the Petropolis Bank to take out a loan.";
        }

        RefreshPayButtonOnly();
    }

    void RefreshPayButtonOnly()
    {
        if (payOffButton == null) return;

        bool active = GlobalData.HasActiveLoan;
        payOffButton.gameObject.SetActive(active);
        if (!active) return;

        bool canAfford = GlobalData.PlayerMoney >= GlobalData.LoanAmountOwed;
        payOffButton.interactable = canAfford;

        if (payOffButtonLabel != null)
        {
            payOffButtonLabel.text = canAfford
                ? $"Pay Off (${GlobalData.LoanAmountOwed:0})"
                : $"Need ${GlobalData.LoanAmountOwed:0}";
        }
    }

    void HideHudWidgets()
    {
        _uiWasActive.Clear();
        if (uiToHideWhileOpen == null) return;

        for (int i = 0; i < uiToHideWhileOpen.Length; i++)
        {
            GameObject go = uiToHideWhileOpen[i];
            if (go == null)
            {
                _uiWasActive.Add(false);
                continue;
            }
            _uiWasActive.Add(go.activeSelf);
            go.SetActive(false);
        }
    }

    void RestoreHudWidgets()
    {
        if (uiToHideWhileOpen == null) return;
        for (int i = 0; i < uiToHideWhileOpen.Length; i++)
        {
            GameObject go = uiToHideWhileOpen[i];
            if (go == null) continue;
            bool wasActive = i < _uiWasActive.Count ? _uiWasActive[i] : true;
            go.SetActive(wasActive);
        }
        _uiWasActive.Clear();
    }

    static void UnlockCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#if ENABLE_INPUT_SYSTEM
        foreach (var sai in Object.FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(true);
#endif
    }

    static void RestoreCursorAfterMenu()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (var sai in Object.FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(false);
#endif
    }

    static bool IsTypingInFocusedInput()
    {
        if (EventSystem.current == null) return false;
        GameObject sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return false;
        var field = sel.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }

    static bool TogglePressedThisFrame() => GameplayInput.LoanPanelTogglePressedThisFrame();

    /// <summary>Escape or gamepad B (East) — close-only shortcut while the loans panel is open.</summary>
    static bool CloseKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        return GameplayInput.GamepadCancelPressedThisFrame();
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
