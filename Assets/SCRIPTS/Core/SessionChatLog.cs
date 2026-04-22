using System.Collections.Generic;

/// <summary>
/// Collects ordered NPC chat lines (player and NPC) for the current session; consumed by <see cref="ReportWriter"/>.
/// Individual NPC messages may also carry the purchases that were finalized as a result of that reply, so
/// the transcript can highlight where each transaction actually happened.
/// </summary>
public static class SessionChatLog
{
    /// <summary>One item the shopkeeper finalized selling as a result of a specific NPC reply.</summary>
    public struct Purchase
    {
        public string ItemName;
        public int Quantity;
        public float PricePerUnit;
    }

    public struct Entry
    {
        public string NpcName;
        public bool FromPlayer;
        public string Message;
        public float GameHour;
        public int Sequence;
        /// <summary>
        /// Null unless this specific NPC reply resolved into one or more confirmed sales
        /// (populated by <see cref="AttachPurchaseToLastNpcMessage"/>). Player lines never carry purchases.
        /// </summary>
        public List<Purchase> Purchases;
    }

    static readonly List<Entry> _entries = new List<Entry>();
    static int _nextSequence;

    public static IReadOnlyList<Entry> Entries => _entries;

    public static void Clear()
    {
        _entries.Clear();
        _nextSequence = 0;
    }

    /// <param name="fromPlayer">True for lines typed by the player; false for NPC (greeting, API reply, errors).</param>
    public static void RecordMessage(string npcName, bool fromPlayer, string message, float gameHour)
    {
        if (string.IsNullOrEmpty(message)) return;

        _entries.Add(new Entry
        {
            NpcName = string.IsNullOrEmpty(npcName) ? "NPC" : npcName,
            FromPlayer = fromPlayer,
            Message = message,
            GameHour = gameHour,
            Sequence = _nextSequence++,
            Purchases = null
        });
    }

    /// <summary>
    /// Attaches a confirmed purchase to the most recent NPC message from <paramref name="npcName"/>.
    /// Called by <c>ShopPurchaseTracker</c> after <see cref="GlobalData.TryBuyItem"/> succeeds, so the report's
    /// chat transcript can visually mark which reply resulted in a transaction and list what changed hands.
    /// No-ops if no matching NPC message exists yet.
    /// </summary>
    public static void AttachPurchaseToLastNpcMessage(string npcName, string itemName, int quantity, float pricePerUnit)
    {
        if (string.IsNullOrEmpty(itemName) || quantity <= 0) return;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry e = _entries[i];
            if (e.FromPlayer) continue;
            if (!string.IsNullOrEmpty(npcName) && e.NpcName != npcName) continue;

            if (e.Purchases == null) e.Purchases = new List<Purchase>();
            e.Purchases.Add(new Purchase
            {
                ItemName = itemName,
                Quantity = quantity,
                PricePerUnit = pricePerUnit
            });
            _entries[i] = e;
            return;
        }
    }
}
