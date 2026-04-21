using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the chat UI: spawns message bubbles, scrolls, and manages input.
/// Lives on the same GameObject as <see cref="NpcIdentity"/>.
/// No API / networking logic lives here.
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] ChatInputField chatInput;
    [SerializeField] Button         sendButton;
    [SerializeField] ScrollRect     scrollRect;
    [SerializeField] RectTransform  messageContainer;

    [Header("Bubble Prefabs")]
    [SerializeField] GameObject playerBubblePrefab;
    [SerializeField] GameObject npcBubblePrefab;

    NpcIdentity _npcIdentity;

    /// <summary>Raised after a user message is appended to History.</summary>
    public event Action OnUserMessageCommitted;

    /// <summary>Raised after an NPC reply is appended to History. Arg is the reply text.</summary>
    public event Action<string> OnNpcReplyReceived;

    public struct ChatMessage
    {
        public string role;
        public string content;
    }

    readonly List<ChatMessage> _history = new();

    public IReadOnlyList<ChatMessage> History => _history;

    public string NpcName => _npcIdentity != null ? _npcIdentity.NpcName : "NPC";

    void Awake()
    {
        _npcIdentity = GetComponent<NpcIdentity>();
        sendButton.onClick.AddListener(() => OnSendMessage(chatInput.Text.Trim()));
        chatInput.OnSubmitMessage += OnSendMessage;
    }

    void Start()
    {
        UpdatePlaceholder();
        ShowGreeting();
    }

    void UpdatePlaceholder()
    {
        var placeholder = chatInput.InputField.placeholder as TMP_Text;
        if (placeholder != null)
            placeholder.text = $"Message {NpcName}...";
    }

    void ShowGreeting()
    {
        if (_npcIdentity == null) return;

        string text = _npcIdentity.FormattedGreeting;
        string visible = StripHiddenBlocks(text);
        SessionChatLog.RecordMessage(NpcName, fromPlayer: false, visible, GlobalData.CurrentTimeOfDay);
        SpawnNpcBubble(visible);
        _history.Add(new ChatMessage { role = "assistant", content = text });
    }

    void OnSendMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        chatInput.Clear();
        chatInput.InputField.ActivateInputField();

        SpawnPlayerBubble(text);
        _history.Add(new ChatMessage { role = "user", content = text });

        SessionChatLog.RecordMessage(NpcName, fromPlayer: true, text, GlobalData.CurrentTimeOfDay);
        BadgeManager.MarkNpcTalkedTo(NpcName);

        StartCoroutine(ScrollToBottom());
        OnUserMessageCommitted?.Invoke();
    }

    /// <summary>
    /// Disables or re-enables the send button and input field (e.g. while waiting for an API reply).
    /// </summary>
    public void SetInteractionLocked(bool locked)
    {
        sendButton.interactable = !locked;
        chatInput.InputField.interactable = !locked;
    }

    // ── Public API for external systems (AI service, etc.) ──────────

    public void ReceiveNpcReply(string text)
    {
        // Display / log only the human-facing part; the full reply (with any hidden
        // meta blocks like [PRICES]...[/PRICES]) stays in history so the model can
        // see its own prior state and so ShopPurchaseTracker can parse the raw text.
        string visible = StripHiddenBlocks(text);

        SpawnNpcBubble(visible);
        _history.Add(new ChatMessage { role = "assistant", content = text });
        SessionChatLog.RecordMessage(NpcName, fromPlayer: false, visible, GlobalData.CurrentTimeOfDay);
        OnNpcReplyReceived?.Invoke(text);
    }

    // ── Hidden-Block Stripping ──────────────────────────────────────

    // Matches a [PRICES]...[/PRICES] block (case-insensitive, spans lines) plus any
    // whitespace/newlines immediately surrounding it, so removing it doesn't leave
    // a blank gap at the end of the message.
    static readonly Regex _hiddenBlockRegex = new Regex(
        @"\s*\[PRICES\].*?\[/PRICES\]\s*",
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Strips meta blocks that are meant for the game / model but not the player
    /// (currently just [PRICES]...[/PRICES]). Safe to call on any string.
    /// </summary>
    static string StripHiddenBlocks(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return _hiddenBlockRegex.Replace(text, "").Trim();
    }

    // ── Bubble Spawning ─────────────────────────────────────────────

    void SpawnPlayerBubble(string text)
    {
        string playerName = string.IsNullOrEmpty(GlobalData.StoredName) ? "You" : GlobalData.StoredName;
        var go = Instantiate(playerBubblePrefab, messageContainer);
        go.GetComponent<MessageBubbleController>()?.SetContent(playerName, text);
        StartCoroutine(ScrollToBottom());
    }

    GameObject SpawnNpcBubble(string text)
    {
        var go = Instantiate(npcBubblePrefab, messageContainer);
        go.GetComponent<MessageBubbleController>()?.SetContent(NpcName, text);
        StartCoroutine(ScrollToBottom());
        return go;
    }

    // ── Scroll ──────────────────────────────────────────────────────

    IEnumerator ScrollToBottom()
    {
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
