using UnityEditor;
using UnityEngine;

/// <summary>
/// Requires non-empty player and dog names during Play when enabled.
/// Toggle via Edit &gt; Mandatory Name Selection (uses PlayerPrefs so runtime can read it).
/// </summary>
internal static class MandatoryNameSelectionMenu
{
    private const string MenuPath = "Edit/Mandatory Name Selection";

    public static bool IsMandatory
    {
        get => PlayerPrefs.GetInt(NameSelectionSettings.PrefsKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(NameSelectionSettings.PrefsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    [MenuItem(MenuPath, false, 302)]
    private static void Toggle()
    {
        IsMandatory = !IsMandatory;
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, IsMandatory);
        return true;
    }
}
