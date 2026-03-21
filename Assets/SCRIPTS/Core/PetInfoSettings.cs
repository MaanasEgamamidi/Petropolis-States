using UnityEngine;

/// <summary>
/// Persisted pet stats panel behavior (Main Menu → Settings → Gameplay Pet Info dropdown order).
/// </summary>
public enum PetInfoDisplayMode
{
    AlwaysOn = 0,
    Toggle = 1,
    HoldToShow = 2
}

public static class PetInfoSettings
{
    const string PrefsKey = "Petropolis_PetInfoMode";

    static int _cached = -1;

    public static PetInfoDisplayMode Mode
    {
        get
        {
            if (_cached < 0)
                _cached = PlayerPrefs.GetInt(PrefsKey, (int)PetInfoDisplayMode.AlwaysOn);
            _cached = Mathf.Clamp(_cached, 0, 2);
            return (PetInfoDisplayMode)_cached;
        }
        set
        {
            _cached = Mathf.Clamp((int)value, 0, 2);
            PlayerPrefs.SetInt(PrefsKey, _cached);
            PlayerPrefs.Save();
        }
    }
}
