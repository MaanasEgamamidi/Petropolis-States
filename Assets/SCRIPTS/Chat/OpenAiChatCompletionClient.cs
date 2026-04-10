using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Thin wrapper around the OpenAI Chat Completions endpoint.
/// Runs as a coroutine so it can be yielded from any MonoBehaviour.
/// </summary>
public static class OpenAiChatCompletionClient
{
    [Serializable]
    struct MessageDto
    {
        public string role;
        public string content;
    }

    [Serializable]
    struct RequestBody
    {
        public string model;
        public List<MessageDto> messages;
    }

    /// <summary>
    /// Builds the messages array from a system prompt, an NPC greeting, and the
    /// conversation history visible in the chat window.
    /// </summary>
    /// <param name="systemPrompt">NpcIdentity context (may be empty).</param>
    /// <param name="history">ChatManager.History — first entry is typically the NPC greeting.</param>
    static List<MessageDto> BuildMessages(string systemPrompt, IReadOnlyList<ChatManager.ChatMessage> history)
    {
        var msgs = new List<MessageDto>();

        // Fold the initial assistant greeting into the system prompt so the API
        // sees a valid alternation (system → user → assistant → …).
        string systemContent = systemPrompt ?? "";
        int startIndex = 0;

        if (history.Count > 0 && history[0].role == "assistant")
        {
            string greeting = history[0].content;
            systemContent = string.IsNullOrEmpty(systemContent)
                ? $"You already opened the conversation with: \"{greeting}\""
                : $"{systemContent}\n\nYou already opened the conversation with: \"{greeting}\"";
            startIndex = 1;
        }

        if (!string.IsNullOrEmpty(systemContent))
            msgs.Add(new MessageDto { role = "system", content = systemContent });

        for (int i = startIndex; i < history.Count; i++)
            msgs.Add(new MessageDto { role = history[i].role, content = history[i].content });

        return msgs;
    }

    /// <summary>
    /// Sends a Chat Completions request and invokes <paramref name="onComplete"/>
    /// with either (reply, null) on success or (null, error) on failure.
    /// </summary>
    public static IEnumerator SendChatRequest(
        string systemPrompt,
        IReadOnlyList<ChatManager.ChatMessage> history,
        Action<string, string> onComplete)
    {
        string apiKey = EnvFileLoader.Get("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            onComplete?.Invoke(null, "OPENAI_API_KEY is not set in .env");
            yield break;
        }

        string model   = EnvFileLoader.Get("OPENAI_MODEL", "gpt-4o-mini");
        string baseUrl = EnvFileLoader.Get("OPENAI_BASE_URL", "https://api.openai.com/v1");
        string url     = $"{baseUrl.TrimEnd('/')}/chat/completions";

        var body = new RequestBody
        {
            model    = model,
            messages = BuildMessages(systemPrompt, history)
        };

        string json = JsonConvert.SerializeObject(body);
        byte[] payload = Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler   = new UploadHandlerRaw(payload);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string detail = request.downloadHandler?.text ?? request.error;
            Debug.LogError($"[OpenAI] {request.responseCode} — {detail}");
            onComplete?.Invoke(null, $"API error ({request.responseCode})");
            yield break;
        }

        try
        {
            var root = JObject.Parse(request.downloadHandler.text);
            string reply = root["choices"]?[0]?["message"]?["content"]?.ToString();
            if (string.IsNullOrEmpty(reply))
            {
                Debug.LogError("[OpenAI] Empty reply from API");
                onComplete?.Invoke(null, "Received an empty reply.");
            }
            else
            {
                onComplete?.Invoke(reply.Trim(), null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenAI] Failed to parse response: {ex.Message}");
            onComplete?.Invoke(null, "Failed to parse API response.");
        }
    }
}
