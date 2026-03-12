using UnityEngine;

/// <summary>
/// Keeps the cursor visible and unlocked. Attach to EventSystem in menu/UI scenes
/// where the cursor should never disappear (e.g. PetChoose, CharacterChoose, Main Menu).
/// </summary>
public class CursorVisibilityKeeper : MonoBehaviour
{
    void Start()
    {
        UnlockCursor();
    }

    void Update()
    {
        // Continuously enforce in case another system tries to lock the cursor
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            UnlockCursor();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            UnlockCursor();
    }

    static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
