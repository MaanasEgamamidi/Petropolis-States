using System;
using System.Collections.Generic;
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

    // ─── Events ──────────────────────────────────────────────────────────────
    /// <summary>Fires when PlayerMoney changes. Positive delta = gain, negative = loss.</summary>
    public static event Action<float> OnMoneyChanged;
    /// <summary>Fires when the player confirms character selection.</summary>
    public static event Action OnCharacterConfirmed;
    /// <summary>Fires when the player confirms pet selection.</summary>
    public static event Action OnPetConfirmed;

    // Pet stats and economy
    /// <summary>Starting cash for a new session; live balance is <c>PlayerMoney</c>.</summary>
    public const float StartingPlayerMoney = 150f;

    static float _playerMoney = StartingPlayerMoney;
    public static float PlayerMoney
    {
        get => _playerMoney;
        set
        {
            float delta = value - _playerMoney;
            _playerMoney = value;
            if (delta != 0f) OnMoneyChanged?.Invoke(delta);
        }
    }
    public static string PetName { get; set; } = "Dog";
    public static float PetHunger { get; set; } = 100f;
    public static float PetHealth { get; set; } = 100f;
    public static float PetHappiness { get; set; } = 100f;
    public static float PetEnergy { get; set; } = 100f;
    public static float PetCleanliness { get; set; } = 100f;

    // ─── Inventory ───────────────────────────────────────────────────────────
    // Key:   item name (must match the names in NPC_Personalities/supermarket_owner.md)
    // Value: float[2] => [0] = base / list price, [1] = quantity the player currently owns
    public const int InventoryPriceIndex    = 0;
    public const int InventoryQuantityIndex = 1;

    public static readonly Dictionary<string, float[]> Inventory = new Dictionary<string, float[]>
    {
        { "Normal Dog Food",       new float[] { 20f, 0f } },
        { "Enhanced Dog Food",     new float[] { 40f, 0f } },
        { "Vitamins",              new float[] { 30f, 0f } },
        { "Portable Watering Cup", new float[] { 20f, 0f } },
    };

    /// <summary>Matches <c>NPC_Personalities/supermarket_owner.md</c> — +20% Hunger per feeding.</summary>
    public const string NormalDogFoodItemName = "Normal Dog Food";
    /// <summary>Matches supermarket guide — +40% Hunger per feeding.</summary>
    public const string EnhancedDogFoodItemName = "Enhanced Dog Food";
    public const string VitaminsItemName = "Vitamins";
    public const string PortableWateringCupItemName = "Portable Watering Cup";
    public const float NormalDogFoodHungerPerUse = 20f;
    public const float EnhancedDogFoodHungerPerUse = 40f;
    public const float VitaminsEnergyPerUse = 30f;
    public const float PortableWateringCupEnergyPerUse = 20f;

    /// <summary>Which pet stat an <see cref="ItemEffect"/> applies to.</summary>
    public enum PetStat { Hunger, Energy, Happiness, Cleanliness }

    /// <summary>Describes what happens when one unit of an item is used on the pet.</summary>
    public readonly struct ItemEffect
    {
        public readonly PetStat Stat;
        public readonly float Amount;
        public ItemEffect(PetStat stat, float amount) { Stat = stat; Amount = amount; }
    }

    /// <summary>Per-use effect for each inventory item. Keys match <see cref="Inventory"/>.</summary>
    public static readonly Dictionary<string, ItemEffect> ItemEffects = new Dictionary<string, ItemEffect>
    {
        { NormalDogFoodItemName,        new ItemEffect(PetStat.Hunger, NormalDogFoodHungerPerUse) },
        { EnhancedDogFoodItemName,      new ItemEffect(PetStat.Hunger, EnhancedDogFoodHungerPerUse) },
        { VitaminsItemName,             new ItemEffect(PetStat.Energy, VitaminsEnergyPerUse) },
        { PortableWateringCupItemName,  new ItemEffect(PetStat.Energy, PortableWateringCupEnergyPerUse) },
    };

    /// <summary>Fires after a successful purchase. Args: item name, quantity added, price paid per unit.</summary>
    public static event Action<string, int, float> OnItemPurchased;

    /// <summary>Fires after any change to inventory quantities (purchase or use). Consumers re-read via <see cref="GetItemQuantity"/>.</summary>
    public static event Action OnInventoryChanged;

    /// <summary>Fires after one unit of dog food (normal or enhanced) is consumed via F key or the inventory Use button. Args: food item name.</summary>
    public static event Action<string> OnPetFed;

    /// <summary>
    /// Attempts to buy <paramref name="quantity"/> of <paramref name="itemName"/> at
    /// <paramref name="pricePerUnit"/>. Deducts money and increases inventory on success.
    /// Returns false if the item is unknown, quantity is non-positive, or the player cannot afford it.
    /// </summary>
    public static bool TryBuyItem(string itemName, int quantity, float pricePerUnit)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0 || pricePerUnit < 0f) return false;
        if (!Inventory.TryGetValue(itemName, out float[] entry)) return false;

        float totalCost = pricePerUnit * quantity;
        if (PlayerMoney < totalCost) return false;

        PlayerMoney -= totalCost;
        entry[InventoryQuantityIndex] += quantity;
        OnItemPurchased?.Invoke(itemName, quantity, pricePerUnit);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Returns how many of <paramref name="itemName"/> the player currently owns (0 if unknown).</summary>
    public static int GetItemQuantity(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && Inventory.TryGetValue(itemName, out float[] entry))
            return Mathf.RoundToInt(entry[InventoryQuantityIndex]);
        return 0;
    }

    /// <summary>True if the player has at least one dog food item usable with <see cref="TryFeedDogFromInventory"/>.</summary>
    public static bool HasAnyDogFoodInInventory()
    {
        return GetItemQuantity(NormalDogFoodItemName) > 0 || GetItemQuantity(EnhancedDogFoodItemName) > 0;
    }

    /// <summary>
    /// Consumes one unit of dog food from inventory (normal first, then enhanced), and adds Hunger
    /// per the supermarket item guide. Returns false if no food remains.
    /// </summary>
    public static bool TryFeedDogFromInventory()
    {
        if (TryUseItem(NormalDogFoodItemName))
            return true;
        if (TryUseItem(EnhancedDogFoodItemName))
            return true;
        return false;
    }

    /// <summary>
    /// Consumes one unit of <paramref name="itemName"/> from inventory and applies its
    /// <see cref="ItemEffect"/> to the pet (clamped to 100). Fires <see cref="OnInventoryChanged"/>
    /// and, for dog food, <see cref="OnPetFed"/>. Returns false if the item is unknown or quantity is 0.
    /// </summary>
    public static bool TryUseItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        if (!Inventory.TryGetValue(itemName, out float[] entry)) return false;
        if (!ItemEffects.TryGetValue(itemName, out ItemEffect effect)) return false;

        int q = Mathf.RoundToInt(entry[InventoryQuantityIndex]);
        if (q <= 0) return false;

        entry[InventoryQuantityIndex] = q - 1;
        ApplyItemEffect(effect);
        PetHealth = (PetEnergy + PetHunger) * 0.5f;

        OnInventoryChanged?.Invoke();
        if (itemName == NormalDogFoodItemName || itemName == EnhancedDogFoodItemName)
            OnPetFed?.Invoke(itemName);
        return true;
    }

    static void ApplyItemEffect(ItemEffect effect)
    {
        switch (effect.Stat)
        {
            case PetStat.Hunger:
                PetHunger = Mathf.Min(100f, PetHunger + effect.Amount);
                break;
            case PetStat.Energy:
                PetEnergy = Mathf.Min(100f, PetEnergy + effect.Amount);
                break;
            case PetStat.Happiness:
                PetHappiness = Mathf.Min(100f, PetHappiness + effect.Amount);
                break;
            case PetStat.Cleanliness:
                PetCleanliness = Mathf.Min(100f, PetCleanliness + effect.Amount);
                break;
        }
    }

    /// <summary>Call from character selection when the player confirms. Stores choice for the next scene.</summary>
    public static void SaveChoice(string gender, int body, int head, string name = "")
    {
        StoredGender = gender ?? "";
        StoredBody = body;
        StoredHead = head;
        StoredName = string.IsNullOrWhiteSpace(name) ? "Player" : name;
        _printedForCurrentChoice = false;
        OnCharacterConfirmed?.Invoke();
    }

    /// <summary>Call from PetChoose when the player confirms. Stores pet skin (1-6) for World scene.</summary>
    public static void SavePetChoice(int petSkin)
    {
        StoredPetSkin = Mathf.Clamp(petSkin, 1, 6);
        OnPetConfirmed?.Invoke();
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
