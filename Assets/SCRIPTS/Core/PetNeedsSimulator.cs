using UnityEngine;

/// <summary>
/// Passive pet needs over time: decay rates, in-game time-of-day tweaks (energy), and recomputing derived
/// <see cref="GlobalData.PetHealth"/>. Runs on an always-active object (see <see cref="EnsureOnWorldLoad"/>)
/// so stats still tick when the Pet Statistics UI is hidden or rebuilt.
/// Zone-specific effects (home, park, bed) stay on those components and run after this in the frame.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PetNeedsSimulator : MonoBehaviour
{
    [Header("Passive decay (per second, while you have a pet)")]
    [SerializeField] [Min(0f)] float happinessDecayPerSecond = 1f;
    [SerializeField] [Min(0f)] float hungerDecayPerSecond = 1f;
    [SerializeField] [Min(0f)] float energyDecayPerSecond = 1f;
    [SerializeField] [Min(0f)] float cleanlinessDecayPerSecond = 0.5f;

    [Header("Game over")]
    [Tooltip("Black-fade before ClosingScene when two+ care stats hit zero.")]
    [SerializeField] [Min(0.02f)] float gameOverFadeDuration = 0.85f;

    [Header("Energy vs time of day (0–24 h)")]
    [Tooltip("Multiply energy decay during night — less drain while the day/night clock is in sleep hours.")]
    [SerializeField] [Min(0.1f)] float energyNightDrainMultiplier = 0.65f;
    [SerializeField] [Min(0f)] float nightStartHour = 22f;
    [SerializeField] [Min(0f)] float nightEndHour = 6f;
    [Tooltip("Slightly more energy drain in the afternoon (active hours).")]
    [SerializeField] [Min(0.1f)] float energyAfternoonDrainMultiplier = 1.15f;
    [SerializeField] [Min(0f)] float afternoonStartHour = 12f;
    [SerializeField] [Min(0f)] float afternoonEndHour = 17f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnWorldLoad()
    {
        if (Object.FindFirstObjectByType<PetNeedsSimulator>(FindObjectsInactive.Exclude) != null)
            return;

        var day = Object.FindFirstObjectByType<DayNightController>(FindObjectsInactive.Include);
        if (day != null)
        {
            day.gameObject.AddComponent<PetNeedsSimulator>();
            return;
        }

        var go = new GameObject(nameof(PetNeedsSimulator));
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PetNeedsSimulator>();
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (GlobalData.StoredPetSkin <= 0) return;
        if (NpcProximityChatOpener.IsWorldNpcChatOpen) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float hLoss = happinessDecayPerSecond * dt;
        float huLoss = hungerDecayPerSecond * dt;
        float eLoss = energyDecayPerSecond * EnergyDrainTimeMultiplier(GlobalData.CurrentTimeOfDay) * dt;
        float cLoss = cleanlinessDecayPerSecond * dt;

        GlobalData.PetHappiness = Mathf.Max(0f, GlobalData.PetHappiness - hLoss);
        GlobalData.PetHunger = Mathf.Max(0f, GlobalData.PetHunger - huLoss);
        GlobalData.PetEnergy = Mathf.Max(0f, GlobalData.PetEnergy - eLoss);
        GlobalData.PetCleanliness = Mathf.Max(0f, GlobalData.PetCleanliness - cLoss);
        GlobalData.RecomputeHealth();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (GlobalData.StoredPetSkin <= 0) return;
        if (GameOverFlow.IsTriggered) return;
        GlobalData.RecomputeHealth();
        if (GlobalData.IsNeglectDeathConditionMet())
            GameOverFlow.TriggerPetNeglect(gameOverFadeDuration);
    }

    float EnergyDrainTimeMultiplier(float timeOfDay)
    {
        float t = timeOfDay;
        if (t < 0f) t = 0f;
        if (t >= 24f) t = Mathf.Repeat(t, 24f);

        bool night = t >= nightStartHour || t < nightEndHour;
        if (night) return energyNightDrainMultiplier;

        if (t >= afternoonStartHour && t < afternoonEndHour)
            return energyAfternoonDrainMultiplier;

        return 1f;
    }
}
