using UnityEditor;

/// <summary>
/// Toggle for session report generation. When enabled, an HTML report is written
/// to the Session Reports/ folder every time you stop playing. Toggle via Edit > Generate Session Reports.
/// </summary>
[InitializeOnLoad]
internal static class GenerateSessionReports
{
    private const string PrefsKey = "Petropolis_GenerateSessionReports";
    private const string MenuPath = "Edit/Generate Session Reports";

    public static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefsKey, true);
        set => EditorPrefs.SetBool(PrefsKey, value);
    }

    [MenuItem(MenuPath, false, 301)]
    private static void Toggle()
    {
        IsEnabled = !IsEnabled;
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, IsEnabled);
        return true;
    }
}
