/// <summary>
/// Builds onboarding copy for Pet Statistics based on <see cref="PetInfoSettings"/>.
/// </summary>
public static class PetStatisticsHintText
{
    public static string Build()
    {
        switch (PetInfoSettings.Mode)
        {
            case PetInfoDisplayMode.AlwaysOn:
                return "Pet Statistics on the left stay open. Change that in Main Menu → Settings → Gameplay → Pet Info.";
            case PetInfoDisplayMode.Toggle:
                return "Press Q to toggle Pet Statistics on the left. Change that in Main Menu → Settings → Gameplay → Pet Info.";
            case PetInfoDisplayMode.HoldToShow:
            default:
                return "Hold Q to open Pet Statistics on the left; release to hide. Change that in Main Menu → Settings → Gameplay → Pet Info.";
        }
    }

    /// <summary>Single-line hint for the sliding Pet Statistics panel (onboarding toast).</summary>
    public static string BuildSliderOneLiner()
    {
        switch (PetInfoSettings.Mode)
        {
            case PetInfoDisplayMode.AlwaysOn:
                return "Pet Statistics live on the left—you can change how they behave in Settings → Gameplay → Pet Info.";
            case PetInfoDisplayMode.Toggle:
                return "Press Q to slide Pet Statistics in and out from the left.";
            case PetInfoDisplayMode.HoldToShow:
            default:
                return "Hold Q to slide Pet Statistics out from the left; release to hide them again.";
        }
    }
}
