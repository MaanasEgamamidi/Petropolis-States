using UnityEngine;

/// <summary>
/// Shared black-fade-to-ClosingScene helper used by every game-over path so starvation,
/// loan default, and any future endings all flow through the same transition. Call
/// <see cref="TriggerStarvation"/> or <see cref="TriggerPetTaken"/> exactly once; the
/// guard prevents re-entry if multiple paths race (e.g. Health hit 0 on the same frame
/// a loan defaulted). Inspect <see cref="LastReason"/> from the closing scene to pick
/// different subtext.
/// </summary>
public static class GameOverFlow
{
    public enum GameOverReason
    {
        None,
        /// <summary>Pet health hit zero (legacy) or two+ care stats depleted.</summary>
        PetStarved,
        PetNeglect,
        PetTakenByBank,
    }

    public static GameOverReason LastReason { get; private set; } = GameOverReason.None;

    /// <summary>True after any Trigger* call until the next session. Prevents duplicate fades.</summary>
    public static bool IsTriggered { get; private set; }

    const string ClosingSceneName = "ClosingScene";
    const float  DefaultFadeDuration = 0.85f;

    /// <summary>Pet starved or too many needs hit zero. Fades to ClosingScene.</summary>
    public static void TriggerStarvation(float fadeDuration = DefaultFadeDuration)
    {
        Trigger(GameOverReason.PetStarved, fadeDuration);
    }

    /// <summary>At least two care stats reached zero. Same closing flow as <see cref="TriggerStarvation"/> with reason <see cref="GameOverReason.PetNeglect"/>.</summary>
    public static void TriggerPetNeglect(float fadeDuration = DefaultFadeDuration)
    {
        Trigger(GameOverReason.PetNeglect, fadeDuration);
    }

    /// <summary>Loan defaulted / bank took the pet. Fades to ClosingScene with reason = PetTakenByBank.</summary>
    public static void TriggerPetTaken(float fadeDuration = DefaultFadeDuration)
    {
        Trigger(GameOverReason.PetTakenByBank, fadeDuration);
    }

    static void Trigger(GameOverReason reason, float fadeDuration)
    {
        if (IsTriggered) return;
        IsTriggered = true;
        LastReason = reason;
        Time.timeScale = 1f;
        GameObject panel = FadeTransitionRunner.CreateFullScreenBlackFadePanel();
        FadeTransitionRunner.RunFadeThenLoadScene(panel, Mathf.Max(0.02f, fadeDuration), ClosingSceneName);
    }

    /// <summary>
    /// Resets the triggered flag so a fresh play-session (e.g. returning to the main menu
    /// then starting a new run) can trigger game-over again. LastReason is intentionally
    /// kept so ClosingSequenceController can still read it during the fade.
    /// </summary>
    public static void ResetForNewSession()
    {
        IsTriggered = false;
    }
}
