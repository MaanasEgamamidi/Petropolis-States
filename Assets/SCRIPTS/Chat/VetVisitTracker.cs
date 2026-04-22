using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Listens to the hospital receptionist's <see cref="ChatManager"/> and, after every NPC reply,
/// silently asks the language model to extract whatever vet visit (if any) the receptionist just
/// *finalized booking* in that exchange. Successful bookings are applied via
/// <see cref="GlobalData.TryCompleteVetVisit"/>, which deducts the agreed price, raises the pet's
/// Hunger and Energy by the visit's health boost, and recomputes Health — so a successfully
/// negotiated visit increases Health, and the two underlying stats that make up the Health
/// average come up together. Nothing is printed to the chatbox; only a Debug.Log summary.
/// Attach this to the same GameObject as the hospital NPC's <see cref="ChatManager"/>.
/// </summary>
[RequireComponent(typeof(ChatManager))]
public class VetVisitTracker : MonoBehaviour
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
        sb.AppendLine("You are a strict JSON extraction assistant for the pet hospital in a dog-care game.");
        sb.AppendLine("You will receive the most recent exchange between a pet OWNER and the hospital RECEPTIONIST.");
        sb.AppendLine("Identify ONLY a vet visit that the receptionist has *finalized booking* in this most recent reply");
        sb.AppendLine("(e.g. the receptionist agreed on a price AND confirmed the visit will happen / is booked).");
        sb.AppendLine("Offers that are still being negotiated, rejected, or only considered do NOT count.");
        sb.AppendLine("At most ONE visit is booked per exchange.");
        sb.AppendLine();
        sb.AppendLine("Return STRICT JSON, no markdown, no commentary. Exact shape:");
        sb.AppendLine("{\"visits\":[{\"visit\":\"<visit name>\",\"price\":<number>}]}");
        sb.AppendLine();
        sb.AppendLine("If nothing was booked in this exchange, return exactly:");
        sb.AppendLine("{\"visits\":[]}");
        sb.AppendLine();
        sb.AppendLine("\"visit\" MUST be one of these exact strings:");
        foreach (var kv in GlobalData.VetVisits)
            sb.AppendLine($"- \"{kv.Key}\"");
        sb.AppendLine();
        sb.AppendLine("\"price\" is the final agreed price the owner is paying for that visit (after any haggling).");
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
            sb.Append("OWNER: ");
            sb.AppendLine(lastUser);
        }
        if (!string.IsNullOrEmpty(lastNpc))
        {
            sb.Append("RECEPTIONIST: ");
            sb.AppendLine(lastNpc);
        }
        return sb.ToString();
    }

    void OnExtractionComplete(string reply, string error)
    {
        if (error != null)
        {
            Debug.LogWarning($"[VetVisitTracker] Extraction request failed: {error}");
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
            Debug.LogWarning($"[VetVisitTracker] Could not parse extraction JSON: {ex.Message}\nRaw: {reply}");
            return;
        }

        var visits = root["visits"] as JArray;
        if (visits == null || visits.Count == 0)
        {
            Debug.Log("[VetVisitTracker] No vet visit booked in last exchange.");
            return;
        }

        var summary = new StringBuilder();
        summary.AppendLine("[VetVisitTracker] Vet visit(s) booked in last message:");

        bool anyApplied = false;
        foreach (JToken v in visits)
        {
            string visitName = (string)v["visit"];
            float  price     = (float?)v["price"] ?? 0f;

            if (string.IsNullOrEmpty(visitName)) continue;
            if (!GlobalData.VetVisits.ContainsKey(visitName))
            {
                summary.AppendLine($"  - (skipped unknown visit) {visitName} @ ${price:0.##}");
                continue;
            }

            float healthBefore = GlobalData.PetHealth;
            bool booked = GlobalData.TryCompleteVetVisit(visitName, price);
            if (booked)
            {
                anyApplied = true;
                float healthDelta = GlobalData.PetHealth - healthBefore;
                summary.AppendLine($"  - {visitName} @ ${price:0.##} (Health {healthBefore:0.#} -> {GlobalData.PetHealth:0.#}, +{healthDelta:0.#})");
                // Reuse the shop's transcript hook so the session report can highlight the
                // booking inline in the chat log the same way purchases are highlighted.
                SessionChatLog.AttachPurchaseToLastNpcMessage(_chat.NpcName, visitName, 1, price);
            }
            else
            {
                summary.AppendLine($"  - (could not afford / rejected) {visitName} @ ${price:0.##}");
            }
        }

        if (anyApplied) summary.AppendLine($"  Money remaining: ${GlobalData.PlayerMoney:0.##}");

        Debug.Log(summary.ToString());
    }
}
