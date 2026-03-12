using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main controller for the Bernie chat UI.
/// Wire up references in the Inspector, then press Play.
/// </summary>
public class BernieChatManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] ChatInputField chatInput;
    [SerializeField] Button         sendButton;
    [SerializeField] ScrollRect     scrollRect;
    [SerializeField] RectTransform  messageContainer;
    [SerializeField] OpenAIService  openAIService;

    [Header("Bubble Prefabs")]
    [SerializeField] GameObject playerBubblePrefab;
    [SerializeField] GameObject bernieBubblePrefab;

    [Header("Bernie Greeting")]
    [SerializeField] string bernieGreeting = "Welcome! I'm Bernie. Looking for something special today?";

    readonly List<OpenAIService.ChatMessage> _history = new();

    bool _waitingForReply;

    void Awake()
    {
        // Button click reads text directly at the moment of click
        sendButton.onClick.AddListener(() => OnSendMessage(chatInput.Text.Trim()));

        // Keyboard submit passes the text through the event
        chatInput.OnSubmitMessage += OnSendMessage;
    }

    void Start()
    {
        StartCoroutine(ShowBernieGreeting());
    }

    IEnumerator ShowBernieGreeting()
    {
        yield return null;
        SpawnBernieBubble(bernieGreeting);
        _history.Add(new OpenAIService.ChatMessage { role = "assistant", content = bernieGreeting });
    }

    void OnSendMessage(string text)
    {
        if (string.IsNullOrEmpty(text) || _waitingForReply) return;

        chatInput.Clear();
        chatInput.InputField.ActivateInputField();

        SpawnPlayerBubble(text);
        _history.Add(new OpenAIService.ChatMessage { role = "user", content = text });

        _waitingForReply = true;
        sendButton.interactable = false;

        var typingBubble = SpawnBernieBubble("...");
        openAIService.SendChat(_history, reply =>
        {
            _waitingForReply = false;
            sendButton.interactable = true;

            string finalText = reply ?? "Hmm, let me think about that...";
            UpdateBubbleText(typingBubble, finalText);
            _history.Add(new OpenAIService.ChatMessage { role = "assistant", content = finalText });

            StartCoroutine(ScrollToBottom());
        });

        StartCoroutine(ScrollToBottom());
    }

    // ── Bubble Spawning ───────────────────────────────────────────────

    void SpawnPlayerBubble(string text)
    {
        string playerName = string.IsNullOrEmpty(GlobalData.StoredName) ? "You" : GlobalData.StoredName;
        var go = Instantiate(playerBubblePrefab, messageContainer);
        go.GetComponent<MessageBubbleController>()?.SetContent(playerName, text);
        StartCoroutine(ScrollToBottom());
    }

    GameObject SpawnBernieBubble(string text)
    {
        var go = Instantiate(bernieBubblePrefab, messageContainer);
        go.GetComponent<MessageBubbleController>()?.SetContent("Bernie", text);
        StartCoroutine(ScrollToBottom());
        return go;
    }

    static void UpdateBubbleText(GameObject bubble, string message)
    {
        bubble.GetComponent<MessageBubbleController>()?.SetMessage(message);
    }

    // ── Scroll ────────────────────────────────────────────────────────

    IEnumerator ScrollToBottom()
    {
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
