using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// World: press <b>G</b> to toggle the player's inventory panel. Opening hides the minimap,
/// money counter, day-night clock, and pet statistics HUD widgets; closing restores them.
/// Spawns one <see cref="InventoryRowView"/> per entry in <see cref="GlobalData.Inventory"/>
/// under <see cref="rowsParent"/>, and refreshes row amounts whenever
/// <see cref="GlobalData.OnInventoryChanged"/> fires.
/// </summary>
public class InventoryPanelController : MonoBehaviour
{
    const string WorldSceneName = "World";

    [Tooltip("Root GameObject of the inventory panel (backdrop + window). Gets SetActive(true/false) on open/close.")]
    [SerializeField] GameObject panelRoot;

    [Tooltip("Parent transform that holds the spawned InventoryRowView instances (e.g. a VerticalLayoutGroup).")]
    [SerializeField] Transform rowsParent;

    [Tooltip("Prefab used for each inventory row; will be instantiated once per item in GlobalData.Inventory.")]
    [SerializeField] InventoryRowView rowPrefab;

    [Tooltip("HUD widgets to hide while the inventory is open (Minimap, Money counter, DayNight clock, Pet Statistics). Their previous active state is restored on close.")]
    [SerializeField] GameObject[] uiToHideWhileOpen;

    readonly List<InventoryRowView> _rows = new List<InventoryRowView>();
    readonly List<bool> _uiWasActive = new List<bool>();

    /// <summary>True while the inventory panel is open. Other systems may check this to gate input.</summary>
    public static bool IsOpen { get; private set; }

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        IsOpen = false;

        SpawnRows();
    }

    void OnEnable()
    {
        GlobalData.OnInventoryChanged += HandleInventoryChanged;
    }

    void OnDisable()
    {
        GlobalData.OnInventoryChanged -= HandleInventoryChanged;
        if (IsOpen)
        {
            RestoreCursorAfterMenu();
            IsOpen = false;
        }
    }

    void SpawnRows()
    {
        if (rowsParent == null || rowPrefab == null) return;

        foreach (string itemName in GlobalData.Inventory.Keys)
        {
            InventoryRowView row = Instantiate(rowPrefab, rowsParent);
            row.gameObject.name = $"Row - {itemName}";
            row.Bind(itemName);
            _rows.Add(row);
        }
    }

    void HandleInventoryChanged()
    {
        RefreshAll();
    }

    void RefreshAll()
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].Refresh();
    }

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

        if (!TogglePressedThisFrame()) return;

        if (IsOpen) Close();
        else Open();
    }

    void LateUpdate()
    {
        // Continuously re-assert the UI-menu cursor state every frame while open so other systems
        // (focus changes, OnApplicationFocus, ControllerVirtualCursor, etc.) cannot flip the cursor
        // back to hidden / spinning. Mirrors NpcProximityChatOpener.LateUpdate.
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

    static bool IsTypingInFocusedInput()
    {
        if (EventSystem.current == null) return false;
        GameObject sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return false;
        var field = sel.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }

    static bool TogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }
}
