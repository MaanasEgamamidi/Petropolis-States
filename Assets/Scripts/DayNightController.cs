using UnityEngine;
using TMPro;
using itsmakingthings_daynightcycle;

/// <summary>
/// Displays time in 12-hour format (e.g. "12 PM") and wires day/night events.
/// Adjusts cycle speed so day is ~80% of playtime and night/evening ~20%.
/// Attach to DayNightCycle_Controller. All customization via Inspector on DayNightCycle.
/// </summary>
[DefaultExecutionOrder(-50)]
public class DayNightController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the DayNightCycle component (on same GameObject or sibling).")]
    public DayNightCycle dayNightCycle;

    [Tooltip("TextMeshProUGUI to display time. Leave null to skip display.")]
    public TextMeshProUGUI timeDisplayText;

    [Header("Optional: Night Lights")]
    [Tooltip("If assigned, TurnLightsOn/TurnLightsOff will be called when night/day starts.")]
    public SimpleLightController simpleLightController;

    [Header("Day/Night Balance")]
    [Tooltip("Time of day when the game starts (24h format, e.g. 7 = 7 AM).")]
    [SerializeField] float startTimeOfDay = 7f;
    [Tooltip("Real-time duration of one full 24h cycle in minutes. Day is 80% of this, night 20%.")]
    [SerializeField] float fullCycleMinutes = 15f;
    float baseDaySpeed;
    int _lastHourSent = -1;

    private void Start()
    {
        if (dayNightCycle == null)
            dayNightCycle = GetComponent<DayNightCycle>();

        // Ensure ambient lighting is driven by DayNightCycle (Color mode)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // Use SolidColor so DayNightCycle's backgroundColor shows (not a static skybox that can go black)
        if (dayNightCycle != null && dayNightCycle.sceneCamera != null)
            dayNightCycle.sceneCamera.clearFlags = CameraClearFlags.SolidColor;

        // Ensure sun correctly follows the pivot (pivot rotation drives day/night arc)
        if (dayNightCycle != null && dayNightCycle.sunLight != null)
            dayNightCycle.sunLight.transform.localRotation = Quaternion.identity;

        // Apply start time (runs before DayNightCycle.Start)
        if (dayNightCycle != null)
            dayNightCycle.startTimeOfDay = startTimeOfDay;

        // Full cycle = 15 min. With 80/20 split: day 12h takes 12 min, night 12h takes 3 min.
        if (dayNightCycle != null && fullCycleMinutes > 0f)
        {
            float dayRealSeconds = fullCycleMinutes * 60f * 0.8f;  // 80% of cycle
            baseDaySpeed = 12f / dayRealSeconds;  // 12 game-hours per day phase
        }
        else
            baseDaySpeed = 1f / 60f;  // fallback: ~0.0167 for 15-min cycle

        // Clear any placeholder text; Update() will set the real time immediately
        if (timeDisplayText != null) timeDisplayText.text = string.Empty;

        if (simpleLightController != null && dayNightCycle != null)
        {
            dayNightCycle.OnDayStarted.AddListener(simpleLightController.TurnLightsOff);
            dayNightCycle.OnNightStarted.AddListener(simpleLightController.TurnLightsOn);
            // Set initial state based on current time
            bool isNight = dayNightCycle.TimeOfDay < 6f || dayNightCycle.TimeOfDay >= 18f;
            if (isNight)
                simpleLightController.TurnLightsOn();
            else
                simpleLightController.TurnLightsOff();
        }
    }

    private void Update()
    {
        if (dayNightCycle == null) return;

        // Day (6-18) = 80% of playtime, Night/evening (18-6) = 20% - run night 4x faster
        bool isDay = dayNightCycle.TimeOfDay >= 6f && dayNightCycle.TimeOfDay < 18f;
        dayNightCycle.cycleSpeed = isDay ? baseDaySpeed : baseDaySpeed * 4f;

        float timeOfDay = dayNightCycle.TimeOfDay;
        int hours = Mathf.FloorToInt(timeOfDay);

        // Update GlobalData whenever the hour changes
        if (hours != _lastHourSent)
        {
            _lastHourSent = hours;
            GlobalData.SetTimeOfDay(timeOfDay);
        }

        if (timeDisplayText == null) return;

        int h12 = (hours % 12 == 0) ? 12 : (hours % 12);
        string period = (hours < 12) ? "AM" : "PM";

        timeDisplayText.text = $"{h12} {period}";
    }
}
