using UnityEngine;

/// <summary>
/// Glues <see cref="ChatManager"/> to the OpenAI Chat Completions API.
/// Attach to the same GameObject that has <see cref="ChatManager"/> and <see cref="NpcIdentity"/>.
/// </summary>
[RequireComponent(typeof(ChatManager))]
public class NpcOpenAiChatBridge : MonoBehaviour
{
    ChatManager  _chat;
    NpcIdentity  _identity;
    bool         _requestInFlight;

    void Awake()
    {
        _chat     = GetComponent<ChatManager>();
        _identity = GetComponent<NpcIdentity>();
    }

    void OnEnable()  => _chat.OnUserMessageCommitted += OnUserMessage;
    void OnDisable() => _chat.OnUserMessageCommitted -= OnUserMessage;

    void OnUserMessage()
    {
        if (_requestInFlight) return;
        _requestInFlight = true;
        _chat.SetInteractionLocked(true);

        string systemPrompt = _identity != null ? _identity.BuildSystemPrompt() : "";

        StartCoroutine(OpenAiChatCompletionClient.SendChatRequest(
            systemPrompt,
            _chat.History,
            OnReplyReceived));
    }

    void OnReplyReceived(string reply, string error)
    {
        _requestInFlight = false;
        _chat.SetInteractionLocked(false);

        if (error != null)
        {
            _chat.ReceiveNpcReply($"[Error: {error}]");
            return;
        }

        _chat.ReceiveNpcReply(reply);
    }
}
