using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends a chat history to the OpenAI Chat Completions API and returns Bernie's reply.
/// Credentials (first match wins): environment variables OPENAI_API_KEY / OPENAI_MODEL,
/// then a ".env" file at the project root (or next to the player data folder in builds).
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
        _apiKey = TrimOrNull(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        _model  = TrimOrNull(Environment.GetEnvironmentVariable("OPENAI_MODEL"));

        string envPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", ".env");
        if (File.Exists(envPath))
        {
            try
            {
                foreach (var pair in ParseEnvFile(envPath))
                {
                    if (string.IsNullOrEmpty(_apiKey) && pair.Key.Equals("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
                        _apiKey = pair.Value;
                    if (string.IsNullOrEmpty(_model) && pair.Key.Equals("OPENAI_MODEL", StringComparison.OrdinalIgnoreCase))
                        _model = pair.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAIService] Could not read .env: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(_model))
            _model = "gpt-4o";

        if (string.IsNullOrEmpty(_apiKey))
            Debug.LogError("[OpenAIService] No API key. Set OPENAI_API_KEY in the environment or in a .env file at the project root (see .env.example).");
        else if (_apiKey.StartsWith("PASTE_", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("[OpenAIService] Replace the placeholder OPENAI_API_KEY in .env with your real key.");
            _apiKey = null;
        }
    }

    static string TrimOrNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    static IEnumerable<KeyValuePair<string, string>> ParseEnvFile(string path)
    {
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            string key = line.Substring(0, eq).Trim();
            string val = line.Substring(eq + 1).Trim();
            if (val.Length >= 2 &&
                ((val[0] == '"' && val[^1] == '"') || (val[0] == '\'' && val[^1] == '\'')))
                val = val.Substring(1, val.Length - 2);

            if (key.Length > 0)
                yield return new KeyValuePair<string, string>(key, val);
        }
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
