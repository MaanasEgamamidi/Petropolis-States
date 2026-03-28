using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-initialising singleton that records session economy data and writes an HTML report on quit.
/// Tracks hourly money snapshots, high/low watermarks, and total session length.
/// </summary>
public class SessionTracker : MonoBehaviour
{
    public struct MoneySnapshot
    {
        public float GameHour;
        public float Money;
    }

    public static SessionTracker Instance { get; private set; }

    float _sessionStartRealTime;
    float _startingMoney;
    float _highestMoney;
    float _lowestMoney;
    int _lastRecordedHour = -1;
    float _startGameHour;
    bool _firstHourRecorded;

    readonly List<MoneySnapshot> _snapshots = new List<MoneySnapshot>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance != null) return;

        var go = new GameObject("SessionTracker");
        DontDestroyOnLoad(go);
        go.AddComponent<SessionTracker>();
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

        _sessionStartRealTime = Time.realtimeSinceStartup;
        _startingMoney = GlobalData.PlayerMoney;
        _highestMoney = _startingMoney;
        _lowestMoney = _startingMoney;
    }

    void OnEnable()
    {
        GlobalData.OnMoneyChanged += OnMoneyChanged;
    }

    void OnDisable()
    {
        GlobalData.OnMoneyChanged -= OnMoneyChanged;
    }

    void OnMoneyChanged(float delta)
    {
        float current = GlobalData.PlayerMoney;
        if (current > _highestMoney) _highestMoney = current;
        if (current < _lowestMoney) _lowestMoney = current;
    }

    void Update()
    {
        float tod = GlobalData.CurrentTimeOfDay;
        int hour = Mathf.FloorToInt(tod);

        if (!_firstHourRecorded)
        {
            _startGameHour = tod;
            RecordSnapshot(tod);
            _lastRecordedHour = hour;
            _firstHourRecorded = true;
            return;
        }

        if (hour != _lastRecordedHour)
        {
            RecordSnapshot(tod);
            _lastRecordedHour = hour;
        }
    }

    void RecordSnapshot(float gameHour)
    {
        _snapshots.Add(new MoneySnapshot
        {
            GameHour = gameHour,
            Money = GlobalData.PlayerMoney
        });
    }

    void OnApplicationQuit()
    {
        if (!ShouldGenerateReport())
            return;

        RecordSnapshot(GlobalData.CurrentTimeOfDay);

        float sessionRealSeconds = Time.realtimeSinceStartup - _sessionStartRealTime;

        float totalGameHours = ComputeTotalGameHours();

        ReportWriter.Generate(
            playerName: GlobalData.StoredName ?? "Player",
            petName: GlobalData.PetName ?? "Unknown",
            sessionRealSeconds: sessionRealSeconds,
            totalGameHours: totalGameHours,
            startingMoney: _startingMoney,
            endingMoney: GlobalData.PlayerMoney,
            highestMoney: _highestMoney,
            lowestMoney: _lowestMoney,
            snapshots: _snapshots
        );
    }

    static bool ShouldGenerateReport()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorPrefs.GetBool("Petropolis_GenerateSessionReports", true);
#else
        return true;
#endif
    }

    float ComputeTotalGameHours()
    {
        if (_snapshots.Count < 2) return 0f;

        float total = 0f;
        for (int i = 1; i < _snapshots.Count; i++)
        {
            float diff = _snapshots[i].GameHour - _snapshots[i - 1].GameHour;
            if (diff < 0f) diff += 24f;
            total += diff;
        }
        return total;
    }
}
