#if ENABLE_INPUT_SYSTEM
using UnityEngine;
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

    /// <summary>Gamepad B (East) — cancel / back (e.g. close pause when mapped in UI).</summary>
    public static bool GamepadCancelPressedThisFrame()
    {
        return Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
    }
#else
    public static bool InteractPressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.E);
    public static bool ChatPressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.C);
    public static bool PausePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape);
    public static bool MinimapTogglePressedThisFrame() => UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.M);
    public static bool GamepadCancelPressedThisFrame() => false;
#endif
}
