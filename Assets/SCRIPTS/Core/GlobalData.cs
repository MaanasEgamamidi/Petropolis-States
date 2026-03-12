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
    /// <summary>Full time of day (0–24, e.g. 7.5 = 7:30 AM). Updated by DayNightController when the hour changes. Use Mathf.FloorToInt(CurrentTimeOfDay) for hour.</summary>
    public static float CurrentTimeOfDay { get; private set; }
    static bool _printedForCurrentChoice;

    // Pet stats and economy
    public static float PlayerMoney { get; set; } = 150.0f;
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
