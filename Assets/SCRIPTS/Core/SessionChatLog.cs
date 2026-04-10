using System.Collections.Generic;

/// <summary>
/// Collects ordered NPC chat lines (player and NPC) for the current session; consumed by <see cref="ReportWriter"/>.
/// </summary>
public static class SessionChatLog
{
    public struct Entry
    {
        public string NpcName;
        public bool FromPlayer;
        public string Message;
        public float GameHour;
        public int Sequence;
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
            Sequence = _nextSequence++
        });
    }
}
