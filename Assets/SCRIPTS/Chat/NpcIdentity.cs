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
    /// Reads every context file, fills in runtime tokens, and joins the files
    /// into one prompt string separated by newlines.
    /// </summary>
    /// <remarks>
    /// Substituted tokens:
    /// <list type="bullet">
    ///   <item><c>{name}</c> → this NPC's name.</item>
    ///   <item><c>[pet name]</c> → <see cref="GlobalData.PetName"/> (the player's dog).</item>
    ///   <item><c>[pet type]</c> → <c>dog</c> (all pets in Petropolis are dogs).</item>
    /// </list>
    /// </remarks>
    public string BuildSystemPrompt()
    {
        if (contextFiles == null || contextFiles.Count == 0)
            return string.Empty;

        string petName = string.IsNullOrWhiteSpace(GlobalData.PetName) ? "your dog" : GlobalData.PetName;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < contextFiles.Count; i++)
        {
            if (contextFiles[i] == null) continue;
            if (sb.Length > 0) sb.Append('\n');
            string text = contextFiles[i].text
                .Replace("{name}", npcName)
                .Replace("[pet name]", petName)
                .Replace("[pet type]", "dog");
            sb.Append(text);
        }
        return sb.ToString();
    }
}
