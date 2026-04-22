using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shared keyboard + gamepad checks for world gameplay (XInput / standard Unity <see cref="Gamepad"/> layout).
/// Face: South=A confirm, East=B, West=X interact, North=Y.
/// </summary>
public static class GameplayInput
{
#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// <see cref="Gamepad.current"/> is often null on Windows until the Input System picks a "current" device
    /// (sometimes only after mouse or keyboard activity). Menus that rely on gamepad alone should use this
    /// so the first connected pad works immediately.
    /// </summary>
    public static Gamepad GetUsableGamepad()
    {
        if (Gamepad.current != null)
            return Gamepad.current;

        var all = Gamepad.all;
        if (TryPickFirstGamepad(all, out var pad))
            return pad;

        // Windows / XInput: Gamepad devices can appear one frame late until another input path runs.
        // Legacy joystick enumeration often forces the OS sync so Gamepad.all populates without a mouse click.
        if (HasLegacyJoystickName())
        {
            InputSystem.Update();
            all = Gamepad.all;
            if (TryPickFirstGamepad(all, out pad))
                return pad;
        }

        return null;
    }

    static bool TryPickFirstGamepad(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Gamepad> all, out Gamepad pad)
    {
        if (all.Count > 0)
        {
            pad = all[0];
            pad.MakeCurrent();
            return true;
        }

        pad = null;
        return false;
    }

    static bool HasLegacyJoystickName()
    {
        var names = Input.GetJoystickNames();
        if (names == null)
            return false;
        for (int i = 0; i < names.Length; i++)
        {
            if (!string.IsNullOrEmpty(names[i]))
                return true;
        }

        return false;
    }

    public static bool InteractPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;
        return Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
    }

    public static bool ChatPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            return true;
        return Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
    }

    /// <summary>Escape or right bumper (RB) — toggle pause menu.</summary>
    public static bool PausePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
    }

    /// <summary>View / Back (the small left-of-center button, Unity's <c>selectButton</c>) — opens / toggles the gameplay settings (pause menu).</summary>
    public static bool GameplaySettingsMenuPressedThisFrame()
    {
        return Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
    }

    /// <summary>M or left bumper (LB) — toggles the minimap.</summary>
    public static bool MinimapTogglePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            return true;
        return Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
    }

    /// <summary>T or D-pad Up — toggle the teleport menu.</summary>
    public static bool TeleportMenuTogglePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            return true;
        return Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;
    }

    /// <summary>G, D-pad Left, or Menu/Start (the "hamburger" button) — toggle the inventory panel.</summary>
    public static bool InventoryTogglePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            return true;
        if (Gamepad.current == null)
            return false;
        return Gamepad.current.dpad.left.wasPressedThisFrame
               || Gamepad.current.startButton.wasPressedThisFrame;
    }

    /// <summary>F or D-pad Down — feed the pet.</summary>
    public static bool FeedPetPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;
        return Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame;
    }

    /// <summary>L, D-pad Right, or right trigger (RT) — toggle the loans panel.</summary>
    public static bool LoanPanelTogglePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            return true;
        var pad = GetUsableGamepad();
        return pad != null
               && (pad.dpad.right.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame);
    }

    /// <summary>Gamepad B (East) — cancel / back (e.g. close pause when mapped in UI).</summary>
    public static bool GamepadCancelPressedThisFrame()
    {
        var pad = GetUsableGamepad();
        return pad != null && pad.buttonEast.wasPressedThisFrame;
    }

    /// <summary>Gamepad A (South) — submit / confirm the currently-focused UI element.</summary>
    public static bool GamepadSubmitPressedThisFrame()
    {
        var pad = GetUsableGamepad();
        return pad != null && pad.buttonSouth.wasPressedThisFrame;
    }

    /// <summary>
    /// One-shot D-pad navigation direction. Returns Vector2Int components in {-1,0,1} for up/down/left/right
    /// discrete D-pad presses. Left-stick is intentionally not included here because the virtual UI cursor
    /// already consumes it for pointer motion; reusing the same stick for selection navigation would move
    /// the selection and the cursor at the same time.
    /// </summary>
    public static Vector2Int GamepadNavigateStepThisFrame()
    {
        var pad = GetUsableGamepad();
        if (pad == null)
            return Vector2Int.zero;

        int x = 0;
        int y = 0;

        if (pad.dpad.left.wasPressedThisFrame) x -= 1;
        if (pad.dpad.right.wasPressedThisFrame) x += 1;
        if (pad.dpad.down.wasPressedThisFrame) y -= 1;
        if (pad.dpad.up.wasPressedThisFrame) y += 1;

        return new Vector2Int(Mathf.Clamp(x, -1, 1), Mathf.Clamp(y, -1, 1));
    }
#else
    public static bool InteractPressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.E);
    public static bool ChatPressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.C);
    public static bool PausePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape);
    public static bool MinimapTogglePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.M);
    public static bool TeleportMenuTogglePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.T);
    public static bool InventoryTogglePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.G);
    public static bool FeedPetPressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F);
    public static bool LoanPanelTogglePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.L);
    public static bool GamepadCancelPressedThisFrame() => false;
    public static bool GamepadSubmitPressedThisFrame() => false;
    public static Vector2Int GamepadNavigateStepThisFrame() => Vector2Int.zero;
#endif
}
