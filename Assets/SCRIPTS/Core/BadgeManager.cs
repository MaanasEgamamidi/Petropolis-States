using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-session badge tracker. Created on <see cref="GlobalData.OnPetConfirmed"/> alongside
/// <see cref="SessionTracker"/>. Aggregates metrics from gameplay systems, evaluates tier promotions
/// each frame, and dispatches earned-tier events to <see cref="BadgeNotificationQueue"/>.
///
/// Progress is per-session only (not persisted to PlayerPrefs).
/// </summary>
public class BadgeManager : MonoBehaviour
{
    public static BadgeManager Instance { get; private set; }

    float _parkGameHours;
    float _petOwnedGameHours;
    bool _leftBounds;
    readonly HashSet<string> _npcsTalkedTo = new();
    int _totalNpcCount;

    readonly Dictionary<BadgeId, int> _earnedTier = new();

    public float ParkGameHours => _parkGameHours;
    public float PetOwnedGameHours => _petOwnedGameHours;
    public float DistanceWithPetMeters => SessionTracker.Instance != null ? SessionTracker.Instance.DistanceWithPetMeters : 0f;
    public bool LeftBounds => _leftBounds;
    public int NpcsTalkedToCount => _npcsTalkedTo.Count;
    public int TotalNpcCount => _totalNpcCount;
    public IReadOnlyDictionary<BadgeId, int> EarnedTiers => _earnedTier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void SubscribeCreateOnPetConfirmed()
    {
        GlobalData.OnPetConfirmed += CreateIfNeeded;
    }

    static void CreateIfNeeded()
    {
        if (Instance) return;

        var go = new GameObject("BadgeManager");
        DontDestroyOnLoad(go);
        go.AddComponent<BadgeManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (BadgeId id in System.Enum.GetValues(typeof(BadgeId)))
            _earnedTier[id] = BadgeTier.None;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RecountNpcs();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RecountNpcs();
    }

    void RecountNpcs()
    {
        var found = FindObjectsByType<NpcIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var unique = new HashSet<string>();
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null) continue;
            string n = found[i].NpcName;
            if (string.IsNullOrEmpty(n)) continue;
            unique.Add(n);
        }
        if (unique.Count > _totalNpcCount)
            _totalNpcCount = unique.Count;
    }

    // ── Metric ingestion ────────────────────────────────────────────

    public static void AddParkGameHours(float gameHourDelta)
    {
        if (Instance == null || gameHourDelta <= 0f) return;
        Instance._parkGameHours += gameHourDelta;
    }

    public static void AddPetOwnedGameHours(float gameHourDelta)
    {
        if (Instance == null || gameHourDelta <= 0f) return;
        Instance._petOwnedGameHours += gameHourDelta;
    }

    public static void MarkOutOfBounds()
    {
        if (Instance == null) return;
        Instance._leftBounds = true;
    }

    public static void MarkNpcTalkedTo(string npcName)
    {
        if (Instance == null || string.IsNullOrEmpty(npcName)) return;
        Instance._npcsTalkedTo.Add(npcName);
    }

    void Update()
    {
        EvaluateAll();
    }

    void EvaluateAll()
    {
        TryPromote(BadgeId.PavementPounder, EvaluatePavementPounder());
        TryPromote(BadgeId.ParkPro,         EvaluateParkPro());
        TryPromote(BadgeId.FaithfulCompanion, EvaluateFaithfulCompanion());
        TryPromote(BadgeId.OffTheLeash,     _leftBounds ? BadgeTier.Earned : BadgeTier.None);
        TryPromote(BadgeId.SocialButterfly, EvaluateSocialButterfly());
    }

    int EvaluatePavementPounder()
    {
        float m = DistanceWithPetMeters;
        if (m >= BadgeDefinitions.PavementPounderGoldMeters)   return BadgeTier.Gold;
        if (m >= BadgeDefinitions.PavementPounderSilverMeters) return BadgeTier.Silver;
        if (m >= BadgeDefinitions.PavementPounderBronzeMeters) return BadgeTier.Bronze;
        return BadgeTier.None;
    }

    int EvaluateParkPro()
    {
        float h = _parkGameHours;
        if (h >= BadgeDefinitions.ParkProGoldGameHours)   return BadgeTier.Gold;
        if (h >= BadgeDefinitions.ParkProSilverGameHours) return BadgeTier.Silver;
        if (h >= BadgeDefinitions.ParkProBronzeGameHours) return BadgeTier.Bronze;
        return BadgeTier.None;
    }

    int EvaluateFaithfulCompanion()
    {
        float days = _petOwnedGameHours / 24f;
        if (days >= BadgeDefinitions.FaithfulCompanionGoldGameDays)   return BadgeTier.Gold;
        if (days >= BadgeDefinitions.FaithfulCompanionSilverGameDays) return BadgeTier.Silver;
        if (days >= BadgeDefinitions.FaithfulCompanionBronzeGameDays) return BadgeTier.Bronze;
        return BadgeTier.None;
    }

    int EvaluateSocialButterfly()
    {
        if (_totalNpcCount <= 0) return BadgeTier.None;
        return _npcsTalkedTo.Count >= _totalNpcCount ? BadgeTier.Earned : BadgeTier.None;
    }

    void TryPromote(BadgeId id, int newTier)
    {
        int current = _earnedTier[id];
        if (newTier <= current) return;

        // Promote one tier at a time so we fire a notification for every tier crossed
        // (e.g. cumulative jump straight to gold should still notify bronze + silver + gold).
        for (int t = current + 1; t <= newTier; t++)
        {
            _earnedTier[id] = t;
            BadgeNotificationQueue.Enqueue(id, t);
        }
    }
}
