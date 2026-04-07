using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// After load (e.g. from pause quit), shows the closing screen for <see cref="displaySeconds"/> or until
/// the player clicks, then fades to black and exits the application.
/// </summary>
public class ClosingSequenceController : MonoBehaviour
{
    [SerializeField] [Min(0f)] float displaySeconds = 5f;
    [SerializeField] [Min(0.02f)] float quitFadeDuration = 0.85f;
    [Tooltip("Wait this long (real-time) so the incoming transition fade can finish before the hold timer.")]
    [SerializeField] [Min(0f)] float incomingFadeCompensation = 0.45f;
    [Tooltip("Ignore mouse clicks for this many seconds after load to avoid the pause Quit click carrying over.")]
    [SerializeField] [Min(0f)] float clickGraceAfterLoad = 0.3f;

    void Start()
    {
        StartCoroutine(ClosingRoutine());
    }

    IEnumerator ClosingRoutine()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (incomingFadeCompensation > 0f)
            yield return new WaitForSecondsRealtime(incomingFadeCompensation);

        float endAt = Time.unscaledTime + displaySeconds;
        float ignoreClicksUntil = Time.unscaledTime + clickGraceAfterLoad;

        while (Time.unscaledTime < endAt)
        {
            if (Time.unscaledTime >= ignoreClicksUntil && MouseClickedThisFrame())
                break;
            yield return null;
        }

        var panel = FadeTransitionRunner.CreateFullScreenBlackFadePanel();
        FadeTransitionRunner.RunFadeThenQuit(panel, quitFadeDuration);
    }

    static bool MouseClickedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Mouse.current.rightButton.wasPressedThisFrame)
                return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            return true;
#endif
        return false;
    }
}
