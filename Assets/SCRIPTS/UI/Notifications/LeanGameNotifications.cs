using System.Collections;
using System.Collections.Generic;
using Lean.Gui;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Wires <see cref="GameNotifications"/> to the Lean GUI "06 Notification" setup: only sets copy and
/// triggers <see cref="LeanPulse.PulseAll"/>; motion and timing come from Lean Transition assets on the object.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class LeanGameNotificationBridge : MonoBehaviour
{
    [SerializeField] Text notificationBodyText;
    [SerializeField] string leanPulseObjectName = "Notification (LeanPulse)";
    [SerializeField, Tooltip("Minimum gap before showing the next queued message. Lean's own chain is ~7s (in + delay + out).")]
    float minSecondsBetweenQueuedMessages = 7f;

    readonly Queue<(string message, float gapAfterSeconds)> _queue = new();
    Coroutine _runner;
    Coroutine _scheduledEntryRoutine;

    void Awake()
    {
        if (notificationBodyText == null)
        {
            var root = GameObject.Find(leanPulseObjectName);
            if (root != null)
                notificationBodyText = root.transform.Find("Panel/Text")?.GetComponent<Text>();
        }

        if (notificationBodyText == null)
        {
            Debug.LogError("[LeanGameNotificationBridge] No Text found. Expected a child path Panel/Text under " + leanPulseObjectName + ".", this);
            enabled = false;
            return;
        }

        GameNotifications.Register(this);
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        if (SceneManager.GetActiveScene().name != "World")
            return;

        if (_scheduledEntryRoutine != null)
            StopCoroutine(_scheduledEntryRoutine);
        _scheduledEntryRoutine = StartCoroutine(RunScheduledEntryToasts());
    }

    void OnDisable()
    {
        if (_scheduledEntryRoutine != null)
        {
            StopCoroutine(_scheduledEntryRoutine);
            _scheduledEntryRoutine = null;
        }
    }

    IEnumerator RunScheduledEntryToasts()
    {
        yield return null;

        bool pendingWelcome = GlobalData.PollWelcomeNotificationDue(out float welcomeAt);
        bool pendingPet = GlobalData.PollPetStatsNotificationDue(out float petAt);

        while (pendingWelcome || pendingPet)
        {
            float now = Time.realtimeSinceStartup;
            if (pendingWelcome && now >= welcomeAt)
            {
                GameNotifications.Show(WorldOnboardingNotifications.WelcomeMessage, 6f);
                GlobalData.ClearWelcomeNotificationSchedule();
                pendingWelcome = false;
            }

            if (pendingPet && now >= petAt)
            {
                GameNotifications.Show(PetStatisticsHintText.BuildSliderOneLiner(), 7f);
                PlayerPrefs.SetInt(WorldOnboardingNotifications.PrefsPetStatsSlider, 1);
                PlayerPrefs.SetInt(WorldOnboardingNotifications.PrefsPetStatsHint, 1);
                PlayerPrefs.Save();
                GlobalData.ClearPetStatsNotificationSchedule();
                pendingPet = false;
            }

            yield return null;
        }

        _scheduledEntryRoutine = null;
    }

    void OnDestroy()
    {
        GameNotifications.Unregister(this);
    }

    public void Enqueue(string message, float gapAfterSeconds)
    {
        _queue.Enqueue((message, gapAfterSeconds));
        if (_runner == null)
            _runner = StartCoroutine(RunQueue());
    }

    IEnumerator RunQueue()
    {
        while (_queue.Count > 0)
        {
            var item = _queue.Dequeue();
            notificationBodyText.text = item.message;
            LeanPulse.PulseAll(leanPulseObjectName);
            float wait = Mathf.Max(0.1f, minSecondsBetweenQueuedMessages, item.gapAfterSeconds);
            yield return new WaitForSecondsRealtime(wait);
        }

        _runner = null;
    }
}

/// <summary>
/// Global entry point for messages shown with the Lean GUI notification example (LeanPulse + transitions).
/// </summary>
public static class GameNotifications
{
    static LeanGameNotificationBridge _bridge;

    public static void Register(LeanGameNotificationBridge bridge)
    {
        _bridge = bridge;
    }

    public static void Unregister(LeanGameNotificationBridge bridge)
    {
        if (_bridge == bridge)
            _bridge = null;
    }

    public static void Show(string message, float visibleDurationSeconds = 5f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_bridge == null)
        {
            Debug.LogWarning("[GameNotifications] No LeanGameNotificationBridge in scene; message not shown: \"" + message + "\"");
            return;
        }

        float gap = Mathf.Max(visibleDurationSeconds, 6.5f);
        _bridge.Enqueue(message.Trim(), gap);
    }
}
