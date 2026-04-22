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

    /// <summary>Care stats (hunger, energy, happiness, cleanliness) at or below this count as "empty" for neglect death.</summary>
    public const float VitalDepletedEpsilon = 0.05f;

    /// <summary>How many of the four care stats are at or near zero. Used for neglect-based game over.</summary>
    public static int CountDepletedCareStats()
    {
        int n = 0;
        if (PetHunger <= VitalDepletedEpsilon) n++;
        if (PetEnergy <= VitalDepletedEpsilon) n++;
        if (PetHappiness <= VitalDepletedEpsilon) n++;
        if (PetCleanliness <= VitalDepletedEpsilon) n++;
        return n;
    }

    /// <summary>True when at least two care stats are depleted — triggers end screen.</summary>
    public static bool IsNeglectDeathConditionMet() => CountDepletedCareStats() >= 2;

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

    /// <summary>Matches <c>NPC_Personalities/supermarket_owner.md</c> — +20% Hunger and Energy per feeding.</summary>
    public const string NormalDogFoodItemName = "Normal Dog Food";
    /// <summary>Matches supermarket guide — +40% Hunger and Energy per feeding.</summary>
    public const string EnhancedDogFoodItemName = "Enhanced Dog Food";
    public const string VitaminsItemName = "Vitamins";
    public const string PortableWateringCupItemName = "Portable Watering Cup";
    public const float NormalDogFoodHungerPerUse = 20f;
    public const float EnhancedDogFoodHungerPerUse = 40f;
    public const float VitaminsEnergyPerUse = 30f;
    public const float PortableWateringCupEnergyPerUse = 20f;

    // How many individual uses each purchased pack grants. Must match
    // NPC_Personalities/supermarket_owner.md (Normal Dog Food = 3 feedings, etc.).
    public const int NormalDogFoodUsesPerPack = 3;
    public const int EnhancedDogFoodUsesPerPack = 2;
    public const int VitaminsUsesPerPack = 2;
    public const int PortableWateringCupUsesPerPack = 4;

    /// <summary>
    /// Uses granted per purchased pack of each item. The price in <see cref="Inventory"/> is the
    /// price of one pack; <see cref="TryBuyItem"/> multiplies the purchased pack count by this
    /// value so the stored quantity reflects total remaining uses (which <see cref="TryUseItem"/>
    /// decrements one at a time).
    /// </summary>
    public static readonly Dictionary<string, int> ItemUsesPerPack = new Dictionary<string, int>
    {
        { NormalDogFoodItemName,       NormalDogFoodUsesPerPack },
        { EnhancedDogFoodItemName,     EnhancedDogFoodUsesPerPack },
        { VitaminsItemName,            VitaminsUsesPerPack },
        { PortableWateringCupItemName, PortableWateringCupUsesPerPack },
    };

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

    // ─── Vet Visits ──────────────────────────────────────────────────────────
    // Bookings offered by the hospital NPC. A successful negotiation (extracted by
    // VetVisitTracker from the NPC's confirming reply) charges the agreed price and
    // raises the pet's Health. Health is the average of Energy and Hunger — see
    // TryUseItem / ApplyVetTreatment — so a vet visit lifts both underlying stats,
    // which in turn makes the Health stat recompute higher.
    public const string BasicCheckupVisitName  = "Basic Checkup";
    public const string WellnessExamVisitName  = "Wellness Exam";
    public const string FullTreatmentVisitName = "Full Treatment";

    public const float BasicCheckupHealthBoost  = 15f;
    public const float WellnessExamHealthBoost  = 30f;
    public const float FullTreatmentHealthBoost = 60f;

    /// <summary>Describes a single vet visit: its posted base price and the health percentage it restores.</summary>
    public readonly struct VetVisit
    {
        public readonly float BasePrice;
        public readonly float HealthBoost;
        public VetVisit(float basePrice, float healthBoost) { BasePrice = basePrice; HealthBoost = healthBoost; }
    }

    /// <summary>
    /// Authoritative catalogue of vet visits the hospital receptionist can book. Keys match the
    /// names used in `NPC_Personalities/vet/vet_visits.md` and the `[PRICES]` block the NPC emits.
    /// </summary>
    public static readonly Dictionary<string, VetVisit> VetVisits = new Dictionary<string, VetVisit>
    {
        { BasicCheckupVisitName,  new VetVisit( 30f, BasicCheckupHealthBoost)  },
        { WellnessExamVisitName,  new VetVisit( 60f, WellnessExamHealthBoost)  },
        { FullTreatmentVisitName, new VetVisit(120f, FullTreatmentHealthBoost) },
    };

    /// <summary>Fires after a vet visit is successfully booked and applied. Args: visit name, price paid, health delta actually applied (after clamping).</summary>
    public static event Action<string, float, float> OnVetVisitCompleted;

    // ─── Bank Loans ──────────────────────────────────────────────────────────
    // One active loan at a time. The player negotiates a loan with the bank NPC
    // (principal, interest percent, term in in-game days). On TryOpenLoan the
    // principal is credited to PlayerMoney and the amount owed
    // (principal + interest) must be repaid in full via TryRepayLoan before the
    // term counts down to 0. LoanDayTicker drives AdvanceOneDay from
    // DayNightCycle.OnDayStarted. If the term expires with a balance owed,
    // ForceDefault is invoked and GameOverFlow takes the pet.

    public const float LoanMinPrincipal   = 50f;
    public const float LoanMaxPrincipal   = 500f;
    /// <summary>Minimum negotiated APR as a decimal (5% = 0.05).</summary>
    public const float LoanMinInterestPct = 0.05f;
    /// <summary>Maximum negotiated APR as a decimal (40% = 0.40).</summary>
    public const float LoanMaxInterestPct = 0.40f;
    public const int   LoanMinTermDays    = 1;
    public const int   LoanMaxTermDays    = 7;

    /// <summary>True when an outstanding loan exists and has not been repaid.</summary>
    public static bool HasActiveLoan { get; private set; }
    /// <summary>Original principal credited on TryOpenLoan.</summary>
    public static float LoanPrincipal { get; private set; }
    /// <summary>Negotiated interest rate as a decimal (e.g. 0.20 = 20%).</summary>
    public static float LoanInterestRate { get; private set; }
    /// <summary>Original term in in-game days at loan open time.</summary>
    public static int LoanTermDays { get; private set; }
    /// <summary>Days remaining until default. Decremented each new in-game day by LoanDayTicker.</summary>
    public static int LoanDaysRemaining { get; private set; }
    /// <summary>Total amount the player must repay (principal + interest), rounded to the nearest whole dollar.</summary>
    public static float LoanAmountOwed { get; private set; }

    /// <summary>Fires when a loan is successfully opened. Args: principal, interest rate, term days.</summary>
    public static event Action<float, float, int> OnLoanOpened;
    /// <summary>Fires when an active loan is fully repaid. Arg: amount repaid.</summary>
    public static event Action<float> OnLoanRepaid;
    /// <summary>Fires whenever any loan field changes (open, repay, day tick). Consumers re-read the static fields.</summary>
    public static event Action OnLoanChanged;
    /// <summary>Fires when a loan's term expired with an outstanding balance. Consumers should end the game.</summary>
    public static event Action OnLoanDefaulted;

    /// <summary>
    /// Attempts to open a new loan. Fails if a loan is already active or any value is out of range.
    /// On success: credits <paramref name="principal"/> to <see cref="PlayerMoney"/>, stores the loan
    /// state (amount owed = round(principal * (1 + interestRate))), and fires
    /// <see cref="OnLoanOpened"/> + <see cref="OnLoanChanged"/>.
    /// </summary>
    public static bool TryOpenLoan(float principal, float interestRate, int termDays)
    {
        if (HasActiveLoan) return false;
        if (principal < LoanMinPrincipal || principal > LoanMaxPrincipal) return false;
        if (interestRate < LoanMinInterestPct || interestRate > LoanMaxInterestPct) return false;
        if (termDays < LoanMinTermDays || termDays > LoanMaxTermDays) return false;

        LoanPrincipal     = Mathf.Round(principal);
        LoanInterestRate  = interestRate;
        LoanTermDays      = termDays;
        LoanDaysRemaining = termDays;
        LoanAmountOwed    = Mathf.Round(LoanPrincipal * (1f + interestRate));
        HasActiveLoan     = true;

        PlayerMoney += LoanPrincipal;
        OnLoanOpened?.Invoke(LoanPrincipal, interestRate, termDays);
        OnLoanChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Pays off the active loan in full. Fails if no loan is active or the player can't afford it.
    /// On success: deducts <see cref="LoanAmountOwed"/> from <see cref="PlayerMoney"/>, clears loan
    /// state, fires <see cref="OnLoanRepaid"/> + <see cref="OnLoanChanged"/>.
    /// </summary>
    public static bool TryRepayLoan()
    {
        if (!HasActiveLoan) return false;
        if (PlayerMoney < LoanAmountOwed) return false;

        float paid = LoanAmountOwed;
        PlayerMoney -= paid;
        ClearLoanState();

        OnLoanRepaid?.Invoke(paid);
        OnLoanChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Decrements the active loan's days-remaining by 1. If it reaches 0 with an outstanding balance,
    /// fires <see cref="OnLoanDefaulted"/> (the ticker wires this to the pet-taken game-over flow).
    /// No-op when no loan is active.
    /// </summary>
    public static void AdvanceOneDay()
    {
        if (!HasActiveLoan) return;
        LoanDaysRemaining = Mathf.Max(0, LoanDaysRemaining - 1);
        OnLoanChanged?.Invoke();
        if (LoanDaysRemaining <= 0)
            OnLoanDefaulted?.Invoke();
    }

    /// <summary>
    /// Immediately treats the loan as defaulted (used by menu tests / cheats). Fires
    /// <see cref="OnLoanDefaulted"/> even if days remain; does NOT clear loan state (so the pet-taken
    /// screen can still inspect the numbers).
    /// </summary>
    public static void ForceDefault()
    {
        if (!HasActiveLoan) return;
        LoanDaysRemaining = 0;
        OnLoanChanged?.Invoke();
        OnLoanDefaulted?.Invoke();
    }

    static void ClearLoanState()
    {
        HasActiveLoan     = false;
        LoanPrincipal     = 0f;
        LoanInterestRate  = 0f;
        LoanTermDays      = 0;
        LoanDaysRemaining = 0;
        LoanAmountOwed    = 0f;
    }

    /// <summary>
    /// Attempts to buy <paramref name="quantity"/> packs of <paramref name="itemName"/> at
    /// <paramref name="pricePerUnit"/> (price per pack). Deducts money and increases inventory
    /// by <c>quantity * ItemUsesPerPack[item]</c> so the stored count represents remaining uses
    /// (e.g. buying 1 Normal Dog Food adds 3 feedings). Returns false if the item is unknown,
    /// quantity is non-positive, or the player cannot afford it.
    /// </summary>
    public static bool TryBuyItem(string itemName, int quantity, float pricePerUnit)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0 || pricePerUnit < 0f) return false;
        if (!Inventory.TryGetValue(itemName, out float[] entry)) return false;

        float totalCost = pricePerUnit * quantity;
        if (PlayerMoney < totalCost) return false;

        int usesPerPack = ItemUsesPerPack.TryGetValue(itemName, out int upp) && upp > 0 ? upp : 1;
        int addedUses = quantity * usesPerPack;

        PlayerMoney -= totalCost;
        entry[InventoryQuantityIndex] += addedUses;
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
    /// and Energy per the supermarket item guide. Returns false if no food remains.
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
    /// <see cref="ItemEffect"/> to the pet (clamped to 100). Dog food also adds the same amount to
    /// <see cref="PetEnergy"/> as to <see cref="PetHunger"/>. Fires <see cref="OnInventoryChanged"/>
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
        if (itemName == NormalDogFoodItemName || itemName == EnhancedDogFoodItemName)
            PetEnergy = Mathf.Min(100f, PetEnergy + effect.Amount);
        RecomputeHealth();

        OnInventoryChanged?.Invoke();
        if (itemName == NormalDogFoodItemName || itemName == EnhancedDogFoodItemName)
            OnPetFed?.Invoke(itemName);
        return true;
    }

    /// <summary>
    /// Recomputes <see cref="PetHealth"/> as the average of <see cref="PetEnergy"/> and
    /// <see cref="PetHunger"/>. Call whenever either underlying stat changes so Health stays in sync.
    /// </summary>
    public static void RecomputeHealth()
    {
        PetHealth = (PetEnergy + PetHunger) * 0.5f;
    }

    /// <summary>
    /// Attempts to book <paramref name="visitName"/> at <paramref name="agreedPrice"/>. On success:
    /// deducts money, raises both <see cref="PetEnergy"/> and <see cref="PetHunger"/> by the visit's
    /// health boost (clamped to 100), recomputes <see cref="PetHealth"/>, and fires
    /// <see cref="OnVetVisitCompleted"/> with the actual health delta applied. Returns false if the
    /// visit name is unknown, the price is non-positive above the free case, or the player can't afford it.
    /// </summary>
    public static bool TryCompleteVetVisit(string visitName, float agreedPrice)
    {
        if (string.IsNullOrWhiteSpace(visitName) || agreedPrice < 0f) return false;
        if (!VetVisits.TryGetValue(visitName, out VetVisit visit)) return false;
        if (PlayerMoney < agreedPrice) return false;

        float healthBefore = PetHealth;
        PlayerMoney -= agreedPrice;

        PetEnergy = Mathf.Min(100f, PetEnergy + visit.HealthBoost);
        PetHunger = Mathf.Min(100f, PetHunger + visit.HealthBoost);
        RecomputeHealth();

        float healthDelta = PetHealth - healthBefore;
        OnVetVisitCompleted?.Invoke(visitName, agreedPrice, healthDelta);
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
