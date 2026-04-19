using System.Collections.Generic;

/// <summary>
/// Static catalog of all badges in Petropolis. Used by <see cref="BadgeManager"/> for tier evaluation
/// and by <see cref="ReportWriter"/> to render the Badges section of the session report.
///
/// Image paths are repo-relative (e.g. "Assets/Badges/1. The Pavement Pounder/bronze.png") so the
/// HTML report can resolve them on disk.
/// </summary>
public enum BadgeId
{
    PavementPounder = 1,
    ParkPro         = 2,
    FaithfulCompanion = 3,
    HighRoller      = 4,
    Philanthropist  = 5,
    VipPassenger    = 6,
    OffTheLeash     = 7,
    SocialButterfly = 8,
}

/// <summary>0 = not earned. 1/2/3 = bronze/silver/gold for tiered badges, or 1 = earned for single-tier badges.</summary>
public static class BadgeTier
{
    public const int None   = 0;
    public const int Bronze = 1;
    public const int Silver = 2;
    public const int Gold   = 3;
    public const int Earned = 1;
}

public sealed class BadgeDefinition
{
    public BadgeId Id;
    public string DisplayName;
    /// <summary>Plain-English category (shown in tooltip header).</summary>
    public string Category;
    /// <summary>True for badges with bronze/silver/gold; false for single-tier badges (#7, #8).</summary>
    public bool IsTiered;
    /// <summary>True for badges that exist but have no tracking implementation yet (#4, #5, #6).</summary>
    public bool NotImplemented;
    /// <summary>One-liner per tier shown in the report tooltip. Index 0 = bronze, 1 = silver, 2 = gold (or just [0] for single-tier).</summary>
    public string[] TierCriteria;
    /// <summary>Repo-relative image paths. For tiered badges length 3 (bronze, silver, gold); single-tier length 1.</summary>
    public string[] TierImagePaths;
}

public static class BadgeDefinitions
{
    public const float PavementPounderBronzeMeters = 200f;
    public const float PavementPounderSilverMeters = 10000f;
    public const float PavementPounderGoldMeters   = 100000f;

    public const float ParkProBronzeGameHours = 1f;
    public const float ParkProSilverGameHours = 5f;
    public const float ParkProGoldGameHours   = 20f;

    public const float FaithfulCompanionBronzeGameDays = 1f;
    public const float FaithfulCompanionSilverGameDays = 5f;
    public const float FaithfulCompanionGoldGameDays   = 20f;

    static readonly Dictionary<BadgeId, BadgeDefinition> _byId = new()
    {
        [BadgeId.PavementPounder] = new BadgeDefinition
        {
            Id = BadgeId.PavementPounder,
            DisplayName = "The Pavement Pounder",
            Category = "Distance",
            IsTiered = true,
            TierCriteria = new[]
            {
                "Bronze: Walk 200 meters with your pet.",
                "Silver: Walk 10,000 meters with your pet.",
                "Gold: Walk 100,000 meters with your pet.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/1. The Pavement Pounder/bronze.png",
                "Assets/Badges/1. The Pavement Pounder/silver.png",
                "Assets/Badges/1. The Pavement Pounder/gold.png",
            },
        },
        [BadgeId.ParkPro] = new BadgeDefinition
        {
            Id = BadgeId.ParkPro,
            DisplayName = "Park Pro",
            Category = "Time in Park",
            IsTiered = true,
            TierCriteria = new[]
            {
                "Bronze: Spend 1 cumulative in-game hour at the park.",
                "Silver: Spend 5 cumulative in-game hours at the park.",
                "Gold: Spend 20 cumulative in-game hours at the park.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/2. Park Pro/bronze.png",
                "Assets/Badges/2. Park Pro/silver.png",
                "Assets/Badges/2. Park Pro/gold.png",
            },
        },
        [BadgeId.FaithfulCompanion] = new BadgeDefinition
        {
            Id = BadgeId.FaithfulCompanion,
            DisplayName = "Faithful Companion",
            Category = "Days Owned",
            IsTiered = true,
            TierCriteria = new[]
            {
                "Bronze: Have your pet for 1 in-game day.",
                "Silver: Have your pet for 5 in-game days.",
                "Gold: Have your pet for 20 in-game days.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/3. Faithful Companion/bronze.png",
                "Assets/Badges/3. Faithful Companion/silver.png",
                "Assets/Badges/3. Faithful Companion/gold.png",
            },
        },
        [BadgeId.HighRoller] = new BadgeDefinition
        {
            Id = BadgeId.HighRoller,
            DisplayName = "High Roller",
            Category = "Money Spent",
            IsTiered = true,
            NotImplemented = true,
            TierCriteria = new[]
            {
                "Bronze: Spend $10 on toys/food.",
                "Silver: Spend $100 on luxury items.",
                "Gold: Spend $1000 on the finest pet gear.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/4. High Roller/bronze.png",
                "Assets/Badges/4. High Roller/silver.png",
                "Assets/Badges/4. High Roller/gold.png",
            },
        },
        [BadgeId.Philanthropist] = new BadgeDefinition
        {
            Id = BadgeId.Philanthropist,
            DisplayName = "The Philanthropist",
            Category = "Donations",
            IsTiered = true,
            NotImplemented = true,
            TierCriteria = new[]
            {
                "Bronze: Donate $50 to the shelter.",
                "Silver: Donate $200 to the shelter.",
                "Gold: Donate $5000 to the shelter.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/5, The Philanthropist/bronze.png",
                "Assets/Badges/5, The Philanthropist/silver.png",
                "Assets/Badges/5, The Philanthropist/gold.png",
            },
        },
        [BadgeId.VipPassenger] = new BadgeDefinition
        {
            Id = BadgeId.VipPassenger,
            DisplayName = "VIP Passenger",
            Category = "Taxi Rides",
            IsTiered = true,
            NotImplemented = true,
            TierCriteria = new[]
            {
                "Bronze: Take the taxi 1 time.",
                "Silver: Take the taxi 10 times.",
                "Gold: Take the taxi 50 times.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/6. VIP Passenger/bronze.png",
                "Assets/Badges/6. VIP Passenger/silver.png",
                "Assets/Badges/6. VIP Passenger/gold.png",
            },
        },
        [BadgeId.OffTheLeash] = new BadgeDefinition
        {
            Id = BadgeId.OffTheLeash,
            DisplayName = "Off the Leash",
            Category = "Out of Bounds",
            IsTiered = false,
            TierCriteria = new[]
            {
                "Leave the map bounds at least once.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/7. Off The Leash/badge.png",
            },
        },
        [BadgeId.SocialButterfly] = new BadgeDefinition
        {
            Id = BadgeId.SocialButterfly,
            DisplayName = "The Social Butterfly",
            Category = "NPC Interactions",
            IsTiered = false,
            TierCriteria = new[]
            {
                "Talk to every single NPC in Petropolis at least once.",
            },
            TierImagePaths = new[]
            {
                "Assets/Badges/8. The Social Butterfly/badge.png",
            },
        },
    };

    public static IReadOnlyDictionary<BadgeId, BadgeDefinition> All => _byId;

    public static BadgeDefinition Get(BadgeId id) => _byId[id];

    /// <summary>Returns "Bronze" / "Silver" / "Gold" / "" for a tier value.</summary>
    public static string TierName(int tier)
    {
        return tier switch
        {
            BadgeTier.Bronze => "Bronze",
            BadgeTier.Silver => "Silver",
            BadgeTier.Gold   => "Gold",
            _ => string.Empty,
        };
    }
}
