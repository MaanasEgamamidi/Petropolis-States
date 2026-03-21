using UnityEngine;

/// <summary>
/// Persisted minimap mode (matches Main Menu → Settings → Gameplay TMP_Dropdown order: Circle, Square, None).
/// </summary>
public enum MinimapMode
{
    Circle = 0,
    Square = 1,
    None = 2
}

public static class MinimapSettings
{
    const string PrefsKey = "Petropolis_MinimapMode";

    static int _cached = -1;

    public static MinimapMode Mode
    {
        get
        {
            if (_cached < 0)
                _cached = PlayerPrefs.GetInt(PrefsKey, (int)MinimapMode.Circle);
            _cached = Mathf.Clamp(_cached, 0, 2);
            return (MinimapMode)_cached;
        }
        set
        {
            _cached = Mathf.Clamp((int)value, 0, 2);
            PlayerPrefs.SetInt(PrefsKey, _cached);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// In-play hide (e.g. M key). Not saved; cleared when minimap UI applies settings on load.
    /// </summary>
    public static bool RuntimeSuppressed { get; set; }

    public static bool IsVisible => Mode != MinimapMode.None && !RuntimeSuppressed;

    /// <summary>Current minimap UI opacity (0–1). Driven by MinimapDisplayController while playing.</summary>
    public static float PresentationAlpha { get; set; }
}
