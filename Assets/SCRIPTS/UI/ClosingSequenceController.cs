using System.Collections;
using TMPro;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// After load (e.g. from pause quit), shows the closing screen for <see cref="displaySeconds"/> or until
/// the player clicks, then fades to black and exits the application. If a <see cref="reasonSubtext"/>
/// TMP is assigned, it is filled from <see cref="GameOverFlow.LastReason"/> so the pet-taken ending
/// shows different flavor text than starvation / quit.
/// </summary>
public class ClosingSequenceController : MonoBehaviour
{
    [SerializeField] [Min(0f)] float displaySeconds = 5f;
    [SerializeField] [Min(0.02f)] float quitFadeDuration = 0.85f;
    [Tooltip("Wait this long (real-time) so the incoming transition fade can finish before the hold timer.")]
    [SerializeField] [Min(0f)] float incomingFadeCompensation = 0.45f;
    [Tooltip("Ignore mouse clicks for this many seconds after load to avoid the pause Quit click carrying over.")]
    [SerializeField] [Min(0f)] float clickGraceAfterLoad = 0.3f;

    [Header("Reason-specific subtext (optional)")]
    [Tooltip("TMP line populated from GameOverFlow.LastReason. Leave null to skip.")]
    [SerializeField] TMP_Text reasonSubtext;
    [TextArea(1, 3)] [SerializeField] string subtextStarved  = "{pet} couldn't hold on.";
    [TextArea(1, 3)] [SerializeField] string subtextNeglect  = "{pet} had too many needs go unmet.";
    [TextArea(1, 3)] [SerializeField] string subtextPetTaken = "The bank came and took {pet}.";
    [TextArea(1, 3)] [SerializeField] string subtextDefault  = string.Empty;

    void Start()
    {
        ApplyReasonSubtext();
        StartCoroutine(ClosingRoutine());
    }

    void ApplyReasonSubtext()
    {
        if (reasonSubtext == null) return;
        string pet = string.IsNullOrWhiteSpace(GlobalData.PetName) ? "your pet" : GlobalData.PetName;
        string template = GameOverFlow.LastReason switch
        {
            GameOverFlow.GameOverReason.PetStarved     => subtextStarved,
            GameOverFlow.GameOverReason.PetNeglect     => subtextNeglect,
            GameOverFlow.GameOverReason.PetTakenByBank => subtextPetTaken,
            _                                          => subtextDefault,
        };
        reasonSubtext.text = string.IsNullOrEmpty(template) ? string.Empty : template.Replace("{pet}", pet);
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
