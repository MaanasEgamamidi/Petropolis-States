using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using itsmakingthings_daynightcycle;

/// <summary>
/// Attach to the BedZone trigger collider in the World scene.
/// When the player (with a pet) is inside the trigger and presses Interact (E / gamepad X),
/// and the in-game time is between 21:00 (9 PM) and 6:00 (6 AM), the world fades to black,
/// the <see cref="DayNightCycle"/> clock is snapped to 6:00 AM, and the world fades back in.
/// Uses the existing <c>FadeCanvas/Fade</c> CanvasGroup in the World scene for the fade overlay.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BedZone : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Header("Sleep window (24h)")]
    [Tooltip("Sleeping is allowed when in-game hour is >= this value. Default 21 (9 PM).")]
    [SerializeField] [Range(0f, 24f)] float sleepStartHour = 21f;
    [Tooltip("…or < this value. Default 6 (6 AM). Wakes up at this hour.")]
    [SerializeField] [Range(0f, 24f)] float sleepEndHour = 6f;

    [Header("Fade")]
    [Tooltip("Total fade time in real seconds (fade-out + hold + fade-in).")]
    [SerializeField] [Min(0.1f)] float fadeDuration = 1.2f;
    [Tooltip("Real-seconds the screen stays fully black after the clock jumps to 6 AM.")]
    [SerializeField] [Min(0f)] float blackHoldDuration = 0.15f;
    [Tooltip("Optional explicit reference to the fade CanvasGroup (the black full-screen image). " +
             "If left empty, BedZone looks up FadeCanvas/Fade in the active scene at runtime.")]
    [SerializeField] CanvasGroup fadeCanvasGroup;

    bool _playerInZone;
    bool _sleeping;

    void Awake()
    {
        if (gameObject.isStatic)
            gameObject.isStatic = false;

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var col = GetComponent<Collider>();
        if (col != null)
        {
            if (!col.isTrigger)
                col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("[BedZone] No Collider on this GameObject.");
        }
    }

    void Update()
    {
        if (_sleeping || !_playerInZone)
            return;

        if (GlobalData.StoredPetSkin <= 0)
            return;

        if (WorldPauseController.IsPaused)
            return;

        if (NpcProximityChatOpener.IsWorldNpcChatOpen || WorldPauseController.IsFocusedOnTextInput())
            return;

        if (!GameplayInput.InteractPressedThisFrame())
            return;

        if (!IsWithinSleepWindow(GetCurrentTimeOfDay()))
            return;

        StartCoroutine(SleepRoutine());
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        var root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerInZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerInZone = false;
    }

    /// <summary>Returns the current hour (0–24). Prefers the live <see cref="DayNightCycle"/>; falls back to <see cref="GlobalData.CurrentTimeOfDay"/>.</summary>
    float GetCurrentTimeOfDay()
    {
        var cycle = FindDayNightCycle();
        if (cycle != null)
            return cycle.TimeOfDay;
        return GlobalData.CurrentTimeOfDay;
    }

    bool IsWithinSleepWindow(float hour)
    {
        // Window wraps across midnight when start > end (21 → 6). Treat as [start, 24) ∪ [0, end).
        if (sleepStartHour <= sleepEndHour)
            return hour >= sleepStartHour && hour < sleepEndHour;
        return hour >= sleepStartHour || hour < sleepEndHour;
    }

    IEnumerator SleepRoutine()
    {
        _sleeping = true;

        CanvasGroup cg = ResolveFadeCanvasGroup();
        GameObject cgOwner = cg != null ? cg.gameObject : null;
        bool wasActive = cgOwner != null && cgOwner.activeSelf;

        if (cg != null)
        {
            if (!wasActive)
                cgOwner.SetActive(true);
            cg.alpha = 0f;
            cg.blocksRaycasts = true;
            cg.interactable = false;
        }

        float half = Mathf.Max(0.01f, fadeDuration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (cg != null)
                cg.alpha = Mathf.Clamp01(t / half);
            yield return null;
        }
        if (cg != null)
            cg.alpha = 1f;

        var cycle = FindDayNightCycle();
        if (cycle != null)
            cycle.SetTimeInstantly(sleepEndHour);
        GlobalData.SetTimeOfDay(sleepEndHour);

        if (blackHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blackHoldDuration);

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (cg != null)
                cg.alpha = 1f - Mathf.Clamp01(t / half);
            yield return null;
        }
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            if (!wasActive && cgOwner != null)
                cgOwner.SetActive(false);
        }

        _sleeping = false;
    }

    CanvasGroup ResolveFadeCanvasGroup()
    {
        if (fadeCanvasGroup != null)
            return fadeCanvasGroup;

        // Look up FadeCanvas/Fade in any loaded scene. Include inactive objects, since
        // the Fade child is inactive by default in the World scene.
        foreach (var go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || go.name != "FadeCanvas")
                continue;

            var childCg = go.GetComponentInChildren<CanvasGroup>(true);
            if (childCg != null)
            {
                fadeCanvasGroup = childCg;
                return fadeCanvasGroup;
            }

            // Fallback: add our own fade image under the existing FadeCanvas.
            fadeCanvasGroup = BuildFallbackFadeGroup(go.transform);
            return fadeCanvasGroup;
        }

        return null;
    }

    static CanvasGroup BuildFallbackFadeGroup(Transform canvasParent)
    {
        var go = new GameObject("Fade");
        go.transform.SetParent(canvasParent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        return cg;
    }

    static DayNightCycle FindDayNightCycle()
    {
        return Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
    }
}
