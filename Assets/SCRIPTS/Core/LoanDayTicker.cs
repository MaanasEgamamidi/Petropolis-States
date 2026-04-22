using itsmakingthings_daynightcycle;
using UnityEngine;

/// <summary>
/// Bridges <see cref="DayNightCycle.OnDayStarted"/> → <see cref="GlobalData.AdvanceOneDay"/> so
/// an active loan's days-remaining ticks down once per in-game day (same event that grants the
/// daily morning allowance in <see cref="DayNightController.GrantDailyMorningAllowance"/>).
/// Subscribes to <see cref="GlobalData.OnLoanDefaulted"/> and triggers the shared pet-taken
/// game-over fade on default. Attach to the DayNightCycle GameObject.
/// </summary>
[DefaultExecutionOrder(-40)]
public class LoanDayTicker : MonoBehaviour
{
    [Tooltip("DayNightCycle to subscribe to. If null, looks for one on this GameObject or children.")]
    [SerializeField] DayNightCycle dayNightCycle;

    [Tooltip("Black-fade duration when the loan defaults and the pet is taken.")]
    [SerializeField] [Min(0.02f)] float gameOverFadeDuration = 0.85f;

    bool _subscribedToDayEvent;
    bool _subscribedToDefault;

    void Awake()
    {
        if (dayNightCycle == null)
            dayNightCycle = GetComponent<DayNightCycle>() ?? GetComponentInChildren<DayNightCycle>(true);
    }

    void OnEnable()
    {
        if (dayNightCycle != null && !_subscribedToDayEvent)
        {
            dayNightCycle.OnDayStarted.AddListener(OnDayStarted);
            _subscribedToDayEvent = true;
        }
        if (!_subscribedToDefault)
        {
            GlobalData.OnLoanDefaulted += HandleLoanDefaulted;
            _subscribedToDefault = true;
        }
    }

    void OnDisable()
    {
        if (dayNightCycle != null && _subscribedToDayEvent)
        {
            dayNightCycle.OnDayStarted.RemoveListener(OnDayStarted);
            _subscribedToDayEvent = false;
        }
        if (_subscribedToDefault)
        {
            GlobalData.OnLoanDefaulted -= HandleLoanDefaulted;
            _subscribedToDefault = false;
        }
    }

    void OnDayStarted()
    {
        GlobalData.AdvanceOneDay();
    }

    void HandleLoanDefaulted()
    {
        Debug.Log("[LoanDayTicker] Loan defaulted — the bank is taking the pet.");
        GameOverFlow.TriggerPetTaken(gameOverFadeDuration);
    }
}
