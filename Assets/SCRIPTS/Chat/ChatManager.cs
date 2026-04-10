using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the chat UI: spawns message bubbles, scrolls, and manages input.
/// Reads the NPC name from an <see cref="NpcIdentity"/> on the ChatPanel.
/// No API / networking logic lives here.
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] ChatInputField chatInput;
    [SerializeField] Button         sendButton;
    [SerializeField] ScrollRect     scrollRect;
    [SerializeField] RectTransform  messageContainer;
    [SerializeField] NpcIdentity    npcIdentity;

    [Header("Bubble Prefabs")]
    [SerializeField] GameObject playerBubblePrefab;
    [SerializeField] GameObject npcBubblePrefab;

    public struct ChatMessage
    {
        public string role;
        public string content;
    }

    readonly List<ChatMessage> _history = new();

    public IReadOnlyList<ChatMessage> History => _history;

    public string NpcName => npcIdentity != null ? npcIdentity.NpcName : "NPC";

    void Awake()
    {
        sendButton.onClick.AddListener(() => OnSendMessage(chatInput.Text.Trim()));
        chatInput.OnSubmitMessage += OnSendMessage;
    }

    void Start()
    {
        UpdatePlaceholder();
        StartCoroutine(ShowGreeting());
    }

    void UpdatePlaceholder()
    {
        var placeholder = chatInput.InputField.placeholder as TMP_Text;
        if (placeholder != null)
            placeholder.text = $"Message {NpcName}...";
    }

    IEnumerator ShowGreeting()
    {
        yield return null;
        if (npcIdentity == null) yield break;

        string text = npcIdentity.FormattedGreeting;
        SpawnNpcBubble(text);
        _history.Add(new ChatMessage { role = "assistant", content = text });
    }

    void OnSendMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        chatInput.Clear();
        chatInput.InputField.ActivateInputField();

        SpawnPlayerBubble(text);
        _history.Add(new ChatMessage { role = "user", content = text });

        StartCoroutine(ScrollToBottom());
    }

    // ── Public API for external systems (AI service, etc.) ──────────

    public void ReceiveNpcReply(string text)
    {
        SpawnNpcBubble(text);
        _history.Add(new ChatMessage { role = "assistant", content = text });
        StartCoroutine(ScrollToBottom());
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
