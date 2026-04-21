using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// When any pet statistic crosses from at or above <see cref="thresholdPercent"/> to below it,
/// shows the paired <see cref="NotificationController"/> (e.g. StatisticWarning under Notifications).
/// </summary>
[DisallowMultipleComponent]
public class PetStatLowWarningController : MonoBehaviour
{
    const float DefaultThreshold = 30f;
    const float DefaultHoldSeconds = 5f;

    [SerializeField] NotificationController notification;
    [SerializeField] [Range(1f, 99f)] float thresholdPercent = DefaultThreshold;
    [SerializeField] [Min(0.1f)] float visibleHoldSeconds = DefaultHoldSeconds;

    float _prevHealth;
    float _prevHappiness;
    float _prevEnergy;
    float _prevHunger;
    float _prevCleanliness;

    void Awake()
    {
        if (notification == null)
            notification = GetComponent<NotificationController>();
        CapturePrevFromGlobal();
    }

    void OnEnable()
    {
        CapturePrevFromGlobal();
    }

    void CapturePrevFromGlobal()
    {
        _prevHealth = GlobalData.PetHealth;
        _prevHappiness = GlobalData.PetHappiness;
        _prevEnergy = GlobalData.PetEnergy;
        _prevHunger = GlobalData.PetHunger;
        _prevCleanliness = GlobalData.PetCleanliness;
    }

    void LateUpdate()
    {
        if (notification == null)
            return;

        float h = GlobalData.PetHealth;
        float p = GlobalData.PetHappiness;
        float e = GlobalData.PetEnergy;
        float n = GlobalData.PetHunger;
        float c = GlobalData.PetCleanliness;

        var crossed = new List<string>(5);
        if (WasAboveNowBelow(_prevHealth, h)) crossed.Add("Health");
        if (WasAboveNowBelow(_prevHappiness, p)) crossed.Add("Happiness");
        if (WasAboveNowBelow(_prevEnergy, e)) crossed.Add("Energy");
        if (WasAboveNowBelow(_prevHunger, n)) crossed.Add("Hunger");
        if (WasAboveNowBelow(_prevCleanliness, c)) crossed.Add("Cleanliness");

        _prevHealth = h;
        _prevHappiness = p;
        _prevEnergy = e;
        _prevHunger = n;
        _prevCleanliness = c;

        if (crossed.Count == 0)
            return;

        notification.ShowMessage(BuildMessage(crossed, thresholdPercent), visibleHoldSeconds);
    }

    bool WasAboveNowBelow(float prev, float current)
    {
        return prev >= thresholdPercent && current < thresholdPercent;
    }

    static string BuildMessage(List<string> names, float threshold)
    {
        int t = Mathf.RoundToInt(threshold);
        var sb = new StringBuilder(128);
        if (names.Count == 1)
        {
            sb.Append(names[0]);
            sb.Append(" statistic is below ");
            sb.Append(t);
            sb.Append("%, take care immediately to keep your pet healthy");
            return sb.ToString();
        }

        for (int i = 0; i < names.Count; i++)
        {
            if (i > 0)
            {
                if (i == names.Count - 1)
                    sb.Append(", and ");
                else
                    sb.Append(", ");
            }
            sb.Append(names[i]);
        }

        sb.Append(" statistics are below ");
        sb.Append(t);
        sb.Append("%, take care immediately to keep your pet healthy");
        return sb.ToString();
    }
}
