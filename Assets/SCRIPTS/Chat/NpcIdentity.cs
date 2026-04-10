using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines an NPC's identity for the chat system.
/// Attach to the ChatPanel GameObject and configure in the Inspector.
/// </summary>
public class NpcIdentity : MonoBehaviour
{
    [Header("NPC Name")]
    [SerializeField] string npcName = "Shopkeeper";

    [Header("Greeting")]
    [SerializeField, TextArea(2, 4)] string greeting = "Hello! I'm {name}. How can I help you?";

    [Header("Personality Context")]
    [Tooltip("Markdown files whose contents are combined into a single system prompt.")]
    [SerializeField] List<TextAsset> contextFiles = new();

    public string NpcName => npcName;

    public string FormattedGreeting => greeting.Replace("{name}", npcName);

    /// <summary>
    /// Reads every context file, replaces {name} tokens, and joins them
    /// into one prompt string separated by newlines.
    /// </summary>
    public string BuildSystemPrompt()
    {
        if (contextFiles == null || contextFiles.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < contextFiles.Count; i++)
        {
            if (contextFiles[i] == null) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(contextFiles[i].text.Replace("{name}", npcName));
        }
        return sb.ToString();
    }
}
