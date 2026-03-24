using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalData : MonoBehaviour
{
    public string gender; // "male" or "female"
    public int body; // 1-5
    public int head; // 1-5

    // Static storage so choice persists across scene loads
    public static string StoredGender { get; private set; }
    public static int StoredBody { get; private set; }
    public static int StoredHead { get; private set; }
    public static string StoredName { get; private set; }
    /// <summary>Pet skin index 1-6 (Dog_01 through Dog_06).</summary>
    public static int StoredPetSkin { get; private set; }
    /// <summary>True when PetChoose was opened additively from World. On Confirm, unload PetChoose instead of loading World.</summary>
    public static bool PetChooseOpenedFromWorld { get; set; }

    /// <summary>Set when the player starts loading PetChoose from World (persists until app exit). Used for follow-up UI hints.</summary>
    public static bool HasOpenedPetChooseFlowFromWorld { get; private set; }
    /// <summary>Full time of day (0–24, e.g. 7.5 = 7:30 AM). Updated by DayNightController when the hour changes. Use Mathf.FloorToInt(CurrentTimeOfDay) for hour.</summary>
    public static float CurrentTimeOfDay { get; private set; }
    static bool _printedForCurrentChoice;

    /// <summary><see cref="Time.realtimeSinceStartup"/> when the welcome toast should appear (set at character confirm).</summary>
    static float _welcomeNotificationShowAtRealtime = -1f;
    /// <summary><see cref="Time.realtimeSinceStartup"/> when the pet-stats toast should appear (set at pet confirm).</summary>
    static float _petStatsNotificationShowAtRealtime = -1f;
    /// <summary>True while a pet-confirm toast is scheduled or playing so <see cref="WorldOnboardingNotifications"/> does not duplicate it.</summary>
    public static bool PetStatsToastFromPetConfirmPending { get; private set; }

    // Pet stats and economy
    /// <summary>Starting cash for a new session; live balance is <c>PlayerMoney</c>.</summary>
    public const float StartingPlayerMoney = 150f;

    public static float PlayerMoney { get; set; } = StartingPlayerMoney;
    public static string PetName { get; set; } = "Max";
    public static float PetHunger { get; set; } = 100f;
    public static float PetHealth { get; set; } = 100f;
    public static float PetHappiness { get; set; } = 100f;
    public static float PetEnergy { get; set; } = 100f;
    public static float PetCleanliness { get; set; } = 100f;

    /// <summary>Call from character selection when the player confirms. Stores choice for the next scene.</summary>
    public static void SaveChoice(string gender, int body, int head, string name = "")
    {
        StoredGender = gender ?? "";
        StoredBody = body;
        StoredHead = head;
        StoredName = string.IsNullOrWhiteSpace(name) ? "Player" : name;
        _printedForCurrentChoice = false;
    }

    /// <summary>Call from PetChoose when the player confirms. Stores pet skin (1-6) for World scene.</summary>
    public static void SavePetChoice(int petSkin)
    {
        StoredPetSkin = Mathf.Clamp(petSkin, 1, 6);
    }

    /// <summary>True after the player has confirmed a character in CharacterChoose.</summary>
    public static bool HasCharacterChoice()
    {
        return !string.IsNullOrEmpty(StoredGender) && StoredBody > 0;
    }

    /// <summary>Call when starting additive PetChoose from the World pet shop trigger.</summary>
    public static void MarkPetChooseFlowStartedFromWorld()
    {
        HasOpenedPetChooseFlowFromWorld = true;
    }

    /// <summary>Call when the player confirms character (before loading World). Toast shows <paramref name="delaySeconds"/> after this moment (wall clock).</summary>
    public static void ScheduleWorldWelcomeNotification(float delaySeconds)
    {
        _welcomeNotificationShowAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0f, delaySeconds);
    }

    /// <summary>Call when the player confirms a pet (before leaving PetChoose). Toast shows <paramref name="delaySeconds"/> after this moment.</summary>
    public static void ScheduleWorldPetStatsNotification(float delaySeconds)
    {
        _petStatsNotificationShowAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0f, delaySeconds);
        PetStatsToastFromPetConfirmPending = true;
    }

    /// <summary>True while a welcome toast is still scheduled (not cleared after show).</summary>
    internal static bool PollWelcomeNotificationDue(out float showAtRealtime)
    {
        if (_welcomeNotificationShowAtRealtime < 0f)
        {
            showAtRealtime = 0f;
            return false;
        }

        showAtRealtime = _welcomeNotificationShowAtRealtime;
        return true;
    }

    internal static void ClearWelcomeNotificationSchedule()
    {
        _welcomeNotificationShowAtRealtime = -1f;
    }

    /// <summary>True while a pet-stats toast is still scheduled.</summary>
    internal static bool PollPetStatsNotificationDue(out float showAtRealtime)
    {
        if (_petStatsNotificationShowAtRealtime < 0f)
        {
            showAtRealtime = 0f;
            return false;
        }

        showAtRealtime = _petStatsNotificationShowAtRealtime;
        return true;
    }

    internal static void ClearPetStatsNotificationSchedule()
    {
        _petStatsNotificationShowAtRealtime = -1f;
        PetStatsToastFromPetConfirmPending = false;
    }

    /// <summary>Call from DayNightController when the hour changes. Updates CurrentTimeOfDay.</summary>
    public static void SetTimeOfDay(float timeOfDay)
    {
        CurrentTimeOfDay = timeOfDay;
    }

    /// <summary>Prints the stored character choice to the console (once per choice).</summary>
    public static void PrintStoredChoice()
    {
        if (string.IsNullOrEmpty(StoredGender) || _printedForCurrentChoice) return;
        Debug.Log($"[GlobalData] Stored character choice: name={StoredName}, gender={StoredGender}, body={StoredBody}, head={StoredHead}");
        _printedForCurrentChoice = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnGameLoad()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PrintStoredChoice();
    }
}
