using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that serializes badge-unlock notifications onto a single
/// <see cref="BadgeNotificationController"/>. When a badge is earned, it is enqueued; the next
/// notification only starts sliding down once the previous one has fully slid back up, so multiple
/// badges queue rather than overlap.
///
/// Auto-creates itself on first <see cref="Enqueue"/> and finds the controller via
/// <see cref="Object.FindFirstObjectByType{T}"/>; you can also place this component manually next to
/// the BadgeNotification GameObject and assign the reference in the Inspector.
/// </summary>
public class BadgeNotificationQueue : MonoBehaviour
{
    public static BadgeNotificationQueue Instance { get; private set; }

    [Tooltip("Badge notification UI to drive. Auto-found at runtime if left empty.")]
    [SerializeField] BadgeNotificationController controller;

    [Tooltip("Gap between consecutive badge notifications, in seconds.")]
    [SerializeField, Min(0f)] float gapBetweenNotifications = 0.25f;

    readonly Queue<(BadgeId id, int tier)> _queue = new();
    Coroutine _runner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Adds a badge unlock to the notification queue, creating the queue host if needed.</summary>
    public static void Enqueue(BadgeId id, int tier)
    {
        EnsureInstance();
        if (Instance == null) return;
        Instance._queue.Enqueue((id, tier));
        if (Instance._runner == null)
            Instance._runner = Instance.StartCoroutine(Instance.RunQueue());
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;

        var existing = FindFirstObjectByType<BadgeNotificationQueue>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var ctrl = FindFirstObjectByType<BadgeNotificationController>(FindObjectsInactive.Include);
        if (ctrl == null)
        {
            Debug.LogWarning("[BadgeNotificationQueue] No BadgeNotificationController found in scene; badge unlocks will not display until one is added.");
            return;
        }

        var go = new GameObject("BadgeNotificationQueue");
        var queue = go.AddComponent<BadgeNotificationQueue>();
        queue.controller = ctrl;
    }

    IEnumerator RunQueue()
    {
        while (_queue.Count > 0)
        {
            var (id, tier) = _queue.Dequeue();

            if (controller == null)
                controller = FindFirstObjectByType<BadgeNotificationController>(FindObjectsInactive.Include);

            if (controller == null)
            {
                Debug.LogWarning($"[BadgeNotificationQueue] Skipping {id} ({tier}) — no BadgeNotificationController in scene.");
                continue;
            }

            var def = BadgeDefinitions.Get(id);
            string tierName = def.IsTiered ? BadgeDefinitions.TierName(tier) : string.Empty;
            controller.ShowBadge(def.DisplayName, tierName);

            // Wait until the controller's coroutine finishes (slide-in + hold + slide-out).
            while (controller != null && controller.IsAnimating)
                yield return null;

            if (gapBetweenNotifications > 0f)
                yield return new WaitForSecondsRealtime(gapBetweenNotifications);
        }

        _runner = null;
    }
}
