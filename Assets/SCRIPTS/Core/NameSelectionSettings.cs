using UnityEngine;

/// <summary>
/// When mandatory (default), character and pet confirm flows require non-empty custom names.
/// Toggle in the Unity Editor: Edit &gt; Mandatory Name Selection.
/// Stored in PlayerPrefs so it applies during Play mode and in builds.
/// </summary>
public static class NameSelectionSettings
{
    public const string PrefsKey = "Petropolis_MandatoryNameSelection";

    /// <summary>True if both player and dog names must be entered before continuing.</summary>
    public static bool IsMandatory => PlayerPrefs.GetInt(PrefsKey, 1) != 0;
}
