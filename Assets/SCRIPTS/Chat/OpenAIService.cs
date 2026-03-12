using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends a chat history to the OpenAI Chat Completions API and returns Bernie's reply.
/// Reads the API key and model name from Resources/api-keys.txt (line 1 = key, line 2 = model).
/// </summary>
public class OpenAIService : MonoBehaviour
{
    const string API_URL = "https://api.openai.com/v1/chat/completions";

    const string BERNIE_SYSTEM_PROMPT =
        "You are Bernie, a friendly but shrewd pet-supply store owner in the town of Petropolis. " +
        "You welcome customers warmly, present your items with their base prices, and negotiate within reason. " +
        "You will not sell below 85% of the base price and typically accept 90–100%. " +
        "You may make 2–3 counter-offers before politely walking away. " +
        "For bundles of multiple items you can offer a 5–10% discount. " +
        "Keep replies concise — two or three sentences at most. " +
        "Speak in first person as Bernie. Never break character.";

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    class RequestBody
    {
        public string model;
        public List<ChatMessage> messages;
        public float temperature = 0.8f;
        public int max_tokens = 200;
    }

    [Serializable]
    class ResponseBody
    {
        public Choice[] choices;

        [Serializable]
        public class Choice
        {
            public ChatMessage message;
        }
    }

    string _apiKey;
    string _model;

    void Awake()
    {
        LoadCredentials();
    }

    void LoadCredentials()
    {
        TextAsset asset = Resources.Load<TextAsset>("api-keys");
        if (asset == null)
        {
            Debug.LogError("[OpenAIService] Resources/api-keys.txt not found. Create it with your API key on line 1 and model on line 2.");
            return;
        }

        string[] lines = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 1 || string.IsNullOrWhiteSpace(lines[0]) || lines[0].StartsWith("PASTE_"))
        {
            Debug.LogError("[OpenAIService] api-keys.txt is missing a valid API key on line 1.");
            return;
        }

        _apiKey = lines[0].Trim();
        _model  = lines.Length >= 2 ? lines[1].Trim() : "gpt-4o";
    }

    /// <summary>
    /// Send the full conversation history (user/assistant turns only) to the API.
    /// Bernie's system prompt is prepended automatically.
    /// <paramref name="onComplete"/> is called with the reply text, or null on error.
    /// </summary>
    public void SendChat(List<ChatMessage> history, Action<string> onComplete)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            onComplete?.Invoke(null);
            return;
        }

        StartCoroutine(PostRequest(history, onComplete));
    }

    const int MaxRetries = 5;
    const float BaseRetryDelay = 30f;

    IEnumerator PostRequest(List<ChatMessage> history, Action<string> onComplete)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage { role = "system", content = BERNIE_SYSTEM_PROMPT }
        };
        messages.AddRange(history);

        var body = new RequestBody { model = _model, messages = messages };
        string json = JsonUtility.ToJson(body);

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            using var request = new UnityWebRequest(API_URL, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ResponseBody response = JsonUtility.FromJson<ResponseBody>(request.downloadHandler.text);
                string reply = response?.choices?[0]?.message?.content?.Trim();
                onComplete?.Invoke(string.IsNullOrEmpty(reply) ? null : reply);
                yield break;
            }

            long statusCode = request.responseCode;
            bool isRateLimited = statusCode == 429 || (request.error != null && request.error.Contains("429"));

            if (isRateLimited && attempt < MaxRetries - 1)
            {
                float waitSeconds = BaseRetryDelay * Mathf.Pow(2f, attempt);
                string retryAfter = request.GetResponseHeader("Retry-After");
                if (!string.IsNullOrEmpty(retryAfter) && int.TryParse(retryAfter, out int retrySecs))
                    waitSeconds = Mathf.Max(waitSeconds, retrySecs);

                Debug.LogWarning($"[OpenAIService] Rate limited (429). Retrying in {waitSeconds:F0}s (attempt {attempt + 1}/{MaxRetries})...");
                yield return new WaitForSeconds(waitSeconds);
            }
            else
            {
                Debug.LogError($"[OpenAIService] Request failed: {request.error}\n{request.downloadHandler.text}");
                string fallback = isRateLimited
                    ? "I'm getting a lot of messages right now — try again in a minute!"
                    : null;
                onComplete?.Invoke(fallback);
                yield break;
            }
        }

        onComplete?.Invoke(null);
    }
}
