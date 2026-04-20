#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Windows only: synthesizes keyboard chords from the gamepad.
/// <list type="bullet">
/// <item>Hold right trigger → hold Left Ctrl + Left Windows (released when trigger released).</item>
/// <item>Press Menu/Start (hamburger) → tap Ctrl + Windows + Space (if RT was holding the first chord, it is briefly released then restored if the trigger is still pulled).</item>
/// </list>
/// Other platforms / no gamepad: no-op.
/// </summary>
[DefaultExecutionOrder(500)]
public sealed class WindowsCtrlWinGamepadBridge : MonoBehaviour
{
    const float RightTriggerPressThreshold = 0.65f;
    const float RightTriggerReleaseThreshold = 0.35f;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    const uint KeyeventfKeyup = 0x0002;
    const byte VkLcontrol = 0xA2;
    const byte VkLwin = 0x5B;
    const byte VkSpace = 0x20;

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    /// <summary>True while we have sent key-down for Ctrl+Win because RT is pulled (not because of Space tap).</summary>
    bool _ctrlWinHeldFromRightTrigger;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!Application.isPlaying)
            return;
        var go = new GameObject(nameof(WindowsCtrlWinGamepadBridge));
        DontDestroyOnLoad(go);
        go.AddComponent<WindowsCtrlWinGamepadBridge>();
#endif
    }

    void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        ReleaseCtrlWinIfHeldFromTrigger();
#endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (pauseStatus)
            ReleaseCtrlWinIfHeldFromTrigger();
#endif
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var pad = Gamepad.current;
        if (pad == null)
        {
            ReleaseCtrlWinIfHeldFromTrigger();
            return;
        }

        float rt = pad.rightTrigger.ReadValue();

        if (pad.startButton.wasPressedThisFrame)
            SimulateCtrlWinSpaceTap(pad);

        bool pullActive = rt >= RightTriggerPressThreshold;
        bool pullReleased = rt < RightTriggerReleaseThreshold;

        if (pullReleased && _ctrlWinHeldFromRightTrigger)
        {
            CtrlWinSyntheticKeyUp();
            _ctrlWinHeldFromRightTrigger = false;
        }
        else if (pullActive && !_ctrlWinHeldFromRightTrigger)
        {
            CtrlWinSyntheticKeyDown();
            _ctrlWinHeldFromRightTrigger = true;
        }
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    static void CtrlWinSyntheticKeyDown()
    {
        keybd_event(VkLcontrol, 0, 0, UIntPtr.Zero);
        keybd_event(VkLwin, 0, 0, UIntPtr.Zero);
    }

    static void CtrlWinSyntheticKeyUp()
    {
        keybd_event(VkLwin, 0, KeyeventfKeyup, UIntPtr.Zero);
        keybd_event(VkLcontrol, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    void ReleaseCtrlWinIfHeldFromTrigger()
    {
        if (!_ctrlWinHeldFromRightTrigger)
            return;
        CtrlWinSyntheticKeyUp();
        _ctrlWinHeldFromRightTrigger = false;
    }

    /// <summary>If RT was holding Ctrl+Win, release; send Ctrl+Win+Space; if RT still pulled, press Ctrl+Win again.</summary>
    void SimulateCtrlWinSpaceTap(Gamepad pad)
    {
        bool hadTriggerHold = _ctrlWinHeldFromRightTrigger;
        if (hadTriggerHold)
        {
            CtrlWinSyntheticKeyUp();
            _ctrlWinHeldFromRightTrigger = false;
        }

        keybd_event(VkLcontrol, 0, 0, UIntPtr.Zero);
        keybd_event(VkLwin, 0, 0, UIntPtr.Zero);
        keybd_event(VkSpace, 0, 0, UIntPtr.Zero);
        keybd_event(VkSpace, 0, KeyeventfKeyup, UIntPtr.Zero);
        keybd_event(VkLwin, 0, KeyeventfKeyup, UIntPtr.Zero);
        keybd_event(VkLcontrol, 0, KeyeventfKeyup, UIntPtr.Zero);

        float rt = pad.rightTrigger.ReadValue();
        if (rt >= RightTriggerPressThreshold)
        {
            CtrlWinSyntheticKeyDown();
            _ctrlWinHeldFromRightTrigger = true;
        }
    }
#endif
}
#else
using UnityEngine;

/// <summary>Inactive without the Input System package.</summary>
public sealed class WindowsCtrlWinGamepadBridge : MonoBehaviour { }
#endif
