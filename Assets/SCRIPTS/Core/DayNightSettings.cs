using UnityEngine;

/// <summary>
/// Persisted length of one full in-game day (real-time minutes). Same 80/20 day vs night proportions as <see cref="DayNightController"/>; only total scale changes.
/// </summary>
public static class DayNightSettings
{
    public const string PrefsKey = "Petropolis_FullCycleMinutes";
    public const float DefaultMinutes = 15f;
    public const float MinMinutes = 1f;
    public const float MaxMinutes = 999f;

    /// <summary>
    /// Effective cycle length: saved value if the player has changed settings, otherwise the scene default from <paramref name="sceneDefaultMinutes"/>.
    /// </summary>
    public static float GetFullCycleMinutes(float sceneDefaultMinutes)
    {
        float fallback = Mathf.Clamp(sceneDefaultMinutes, MinMinutes, MaxMinutes);
        if (!PlayerPrefs.HasKey(PrefsKey))
            return fallback;
        return Mathf.Clamp(PlayerPrefs.GetFloat(PrefsKey, DefaultMinutes), MinMinutes, MaxMinutes);
    }

    public static void SetFullCycleMinutes(float minutes)
    {
        minutes = Mathf.Clamp(minutes, MinMinutes, MaxMinutes);
        PlayerPrefs.SetFloat(PrefsKey, minutes);
        PlayerPrefs.Save();
    }

    /// <summary>Whole minutes as digits for the menu field (no prefs key yet → <see cref="DefaultMinutes"/>).</summary>
    public static string GetMenuDisplayMinutes()
    {
        float m = PlayerPrefs.HasKey(PrefsKey)
            ? PlayerPrefs.GetFloat(PrefsKey, DefaultMinutes)
            : DefaultMinutes;
        m = Mathf.Clamp(m, MinMinutes, MaxMinutes);
        return Mathf.RoundToInt(m).ToString();
    }
}
