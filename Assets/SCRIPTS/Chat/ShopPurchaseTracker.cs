using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Listens to a shopkeeper <see cref="ChatManager"/> and, after every NPC reply, silently asks the
/// language model to extract whatever the player just finalized buying in that last exchange.
/// Confirmed purchases are pushed into <see cref="GlobalData.Inventory"/>; nothing is printed to
/// the chatbox — only a Debug.Log summary listing the items and how many the user bought.
/// Attach this to the same GameObject as the supermarket owner's <see cref="ChatManager"/>.
/// </summary>
[RequireComponent(typeof(ChatManager))]
public class ShopPurchaseTracker : MonoBehaviour
{
    ChatManager _chat;

    void Awake()
    {
        _chat = GetComponent<ChatManager>();
    }

    void OnEnable()  => _chat.OnNpcReplyReceived += OnNpcReply;
    void OnDisable() => _chat.OnNpcReplyReceived -= OnNpcReply;

    void OnNpcReply(string _)
    {
        // Need at least one user message + the latest NPC reply to have a meaningful exchange.
        if (_chat.History.Count < 2) return;
        StartCoroutine(OpenAiChatCompletionClient.SendSingleTurnRequest(
            BuildSystemPrompt(),
            BuildExchangeText(),
            OnExtractionComplete));
    }

    static string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a strict JSON extraction assistant for a dog-supermarket game.");
        sb.AppendLine("You will receive the most recent exchange between a CUSTOMER and the SHOPKEEPER.");
        sb.AppendLine("Identify ONLY items that the shopkeeper has *finalized selling* in this most recent reply");
        sb.AppendLine("(e.g. the shopkeeper agreed to the price and/or handed the item over). Offers that are still");
        sb.AppendLine("being negotiated, rejected, or only considered do NOT count.");
        sb.AppendLine();
        sb.AppendLine("Return STRICT JSON, no markdown, no commentary. Exact shape:");
        sb.AppendLine("{\"purchases\":[{\"item\":\"<item name>\",\"quantity\":<int>,\"price_per_unit\":<number>}]}");
        sb.AppendLine();
        sb.AppendLine("If nothing was purchased in this exchange, return exactly:");
        sb.AppendLine("{\"purchases\":[]}");
        sb.AppendLine();
        sb.AppendLine("\"item\" MUST be one of these exact strings:");
        foreach (var kv in GlobalData.Inventory)
            sb.AppendLine($"- \"{kv.Key}\"");
        sb.AppendLine();
        sb.AppendLine("\"price_per_unit\" is the final agreed price per single unit (after any haggling).");
        sb.AppendLine("\"quantity\" is a positive integer.");
        return sb.ToString();
    }

    string BuildExchangeText()
    {
        // Grab the last user message and the last assistant message from chat history.
        IReadOnlyList<ChatManager.ChatMessage> h = _chat.History;
        string lastUser = null, lastNpc = null;
        for (int i = h.Count - 1; i >= 0; i--)
        {
            if (lastNpc == null && h[i].role == "assistant") lastNpc = h[i].content;
            else if (lastUser == null && h[i].role == "user") lastUser = h[i].content;
            if (lastUser != null && lastNpc != null) break;
        }

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(lastUser))
        {
            sb.Append("CUSTOMER: ");
            sb.AppendLine(lastUser);
        }
        if (!string.IsNullOrEmpty(lastNpc))
        {
            sb.Append("SHOPKEEPER: ");
            sb.AppendLine(lastNpc);
        }
        return sb.ToString();
    }

    void OnExtractionComplete(string reply, string error)
    {
        if (error != null)
        {
            Debug.LogWarning($"[ShopPurchaseTracker] Extraction request failed: {error}");
            return;
        }
        if (string.IsNullOrEmpty(reply)) return;

        JObject root;
        try
        {
            // Be forgiving: strip any accidental markdown fences.
            string json = reply.Trim();
            if (json.StartsWith("```"))
            {
                int first = json.IndexOf('\n');
                int last  = json.LastIndexOf("```");
                if (first >= 0 && last > first) json = json.Substring(first + 1, last - first - 1).Trim();
            }
            root = JObject.Parse(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ShopPurchaseTracker] Could not parse extraction JSON: {ex.Message}\nRaw: {reply}");
            return;
        }

        var purchases = root["purchases"] as JArray;
        if (purchases == null || purchases.Count == 0)
        {
            Debug.Log("[ShopPurchaseTracker] No purchases in last exchange.");
            return;
        }

        var summary = new StringBuilder();
        summary.AppendLine("[ShopPurchaseTracker] Items bought in last message:");

        bool anyApplied = false;
        foreach (JToken p in purchases)
        {
            string item      = (string)p["item"];
            int    quantity  = (int?)p["quantity"] ?? 0;
            float  unitPrice = (float?)p["price_per_unit"] ?? 0f;

            if (string.IsNullOrEmpty(item) || quantity <= 0) continue;
            if (!GlobalData.Inventory.ContainsKey(item))
            {
                summary.AppendLine($"  - (skipped unknown item) {item} x{quantity}");
                continue;
            }

            bool bought = GlobalData.TryBuyItem(item, quantity, unitPrice);
            if (bought)
            {
                anyApplied = true;
                int owned = GlobalData.GetItemQuantity(item);
                summary.AppendLine($"  - {item} x{quantity} @ ${unitPrice:0.##} each (now own {owned})");
            }
            else
            {
                summary.AppendLine($"  - (could not afford / rejected) {item} x{quantity} @ ${unitPrice:0.##}");
            }
        }

        if (anyApplied) summary.AppendLine($"  Money remaining: ${GlobalData.PlayerMoney:0.##}");

        Debug.Log(summary.ToString());
    }
}
