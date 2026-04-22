using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Listens to the bank loan officer's <see cref="ChatManager"/> and, after every NPC reply,
/// silently asks the language model to extract whatever loan (if any) the officer just
/// *finalized* in that exchange. Successful extractions are applied via
/// <see cref="GlobalData.TryOpenLoan"/>, which credits the principal to the player's money
/// and starts the day-countdown for pet-seizure on default. Nothing is printed to the
/// chatbox; only a Debug.Log summary.
/// Attach this to the same GameObject as the bank NPC's <see cref="ChatManager"/>.
/// </summary>
[RequireComponent(typeof(ChatManager))]
public class BankLoanTracker : MonoBehaviour
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
        if (_chat.History.Count < 2) return;
        // Don't attempt extraction if a loan is already active — the officer's persona
        // rules already decline it, and the game enforces one-at-a-time.
        if (GlobalData.HasActiveLoan) return;

        StartCoroutine(OpenAiChatCompletionClient.SendSingleTurnRequest(
            BuildSystemPrompt(),
            BuildExchangeText(),
            OnExtractionComplete));
    }

    static string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a strict JSON extraction assistant for the bank in a dog-care game.");
        sb.AppendLine("You will receive the most recent exchange between a pet OWNER and the bank LOAN OFFICER.");
        sb.AppendLine("Identify ONLY a loan that the officer has *finalized* in this most recent reply");
        sb.AppendLine("(e.g. the officer committed to writing the loan AND confirmed the principal, interest, and term).");
        sb.AppendLine("Offers that are still being negotiated, declined, or only considered do NOT count.");
        sb.AppendLine("At most ONE loan is finalized per exchange.");
        sb.AppendLine();
        sb.AppendLine("Return STRICT JSON, no markdown, no commentary. Exact shape:");
        sb.AppendLine("{\"loan\":{\"principal\":<int>,\"interest_pct\":<int>,\"term_days\":<int>}}");
        sb.AppendLine();
        sb.AppendLine("If no loan was finalized in this exchange, return exactly:");
        sb.AppendLine("{\"loan\":null}");
        sb.AppendLine();
        sb.AppendLine($"\"principal\" is a whole dollar integer in [{GlobalData.LoanMinPrincipal:0}, {GlobalData.LoanMaxPrincipal:0}].");
        sb.AppendLine($"\"interest_pct\" is a whole percent integer in [{GlobalData.LoanMinInterestPct * 100f:0}, {GlobalData.LoanMaxInterestPct * 100f:0}].");
        sb.AppendLine($"\"term_days\" is a whole-day integer in [{GlobalData.LoanMinTermDays}, {GlobalData.LoanMaxTermDays}].");
        sb.AppendLine();
        sb.AppendLine("The officer's reply usually ends with a hidden [LOAN] block like:");
        sb.AppendLine("  [LOAN]");
        sb.AppendLine("  principal=200");
        sb.AppendLine("  interest_pct=20");
        sb.AppendLine("  term_days=3");
        sb.AppendLine("  status=finalized");
        sb.AppendLine("  [/LOAN]");
        sb.AppendLine("If status is anything other than exactly 'finalized', return {\"loan\":null}.");
        sb.AppendLine("If the status is 'finalized', return the numbers in that same block.");
        return sb.ToString();
    }

    string BuildExchangeText()
    {
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
            sb.Append("OFFICER: ");
            sb.AppendLine(lastNpc);
        }
        return sb.ToString();
    }

    void OnExtractionComplete(string reply, string error)
    {
        if (error != null)
        {
            Debug.LogWarning($"[BankLoanTracker] Extraction request failed: {error}");
            return;
        }
        if (string.IsNullOrEmpty(reply)) return;

        JObject root;
        try
        {
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
            Debug.LogWarning($"[BankLoanTracker] Could not parse extraction JSON: {ex.Message}\nRaw: {reply}");
            return;
        }

        JToken loanTok = root["loan"];
        if (loanTok == null || loanTok.Type == JTokenType.Null)
        {
            Debug.Log("[BankLoanTracker] No loan finalized in last exchange.");
            return;
        }

        int principalInt  = (int?)loanTok["principal"]    ?? 0;
        int interestInt   = (int?)loanTok["interest_pct"] ?? 0;
        int termDays      = (int?)loanTok["term_days"]    ?? 0;
        float principal   = principalInt;
        float interestPct = interestInt / 100f;

        if (GlobalData.HasActiveLoan)
        {
            Debug.LogWarning($"[BankLoanTracker] Officer finalized a loan but one is already active — ignored. ({principalInt}/{interestInt}%/{termDays}d)");
            return;
        }

        bool opened = GlobalData.TryOpenLoan(principal, interestPct, termDays);
        if (opened)
        {
            Debug.Log($"[BankLoanTracker] Loan opened: ${principal:0} at {interestInt}% over {termDays} day(s). Owed ${GlobalData.LoanAmountOwed:0}. Money after credit: ${GlobalData.PlayerMoney:0.##}");
            SessionChatLog.AttachPurchaseToLastNpcMessage(_chat.NpcName, $"Loan ${principal:0} @ {interestInt}% / {termDays}d", 1, GlobalData.LoanAmountOwed);
        }
        else
        {
            Debug.LogWarning($"[BankLoanTracker] Rejected out-of-range loan: ${principalInt} at {interestInt}% over {termDays} day(s).");
        }
    }
}
