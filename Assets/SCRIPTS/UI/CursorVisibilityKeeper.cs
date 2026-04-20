using UnityEngine;

/// <summary>
/// Keeps the cursor visible and unlocked. Attach to EventSystem in menu/UI scenes
/// where the cursor should never disappear (e.g. PetChoose, CharacterChoose, Main Menu).
/// When <see cref="SuppressMenuHardwareCursor"/> is set (gamepad virtual pointer idle-hide),
/// this script stops forcing visibility so the pointer can stay hidden.
/// </summary>
public class CursorVisibilityKeeper : MonoBehaviour
{
    /// <summary>Set by <c>ControllerVirtualCursor</c> while the gamepad pointer is idle-hidden.</summary>
    public static bool SuppressMenuHardwareCursor { get; set; }

    void Start()
    {
        if (!SuppressMenuHardwareCursor)
            UnlockCursor();
    }

    void Update()
    {
        if (SuppressMenuHardwareCursor)
            return;
        // Continuously enforce in case another system tries to lock the cursor
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            UnlockCursor();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !SuppressMenuHardwareCursor)
            UnlockCursor();
    }

    static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
