using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slide-down notification specialised for badge unlocks. Displays the line
/// "You just achieved &lt;name&gt; (&lt;tier&gt; Tier)!". Mirrors the slide animation from
/// <see cref="NotificationController"/> but exposes <see cref="ShowBadge"/> for runtime data.
///
/// Place as a child of the Notifications canvas, off-screen above the viewport.
/// <see cref="BadgeNotificationQueue"/> drives the show calls.
/// </summary>
public class BadgeNotificationController : MonoBehaviour
{
    [Header("Slide Settings")]
    [Tooltip("Anchored Y position when visible on screen.")]
    [SerializeField] float onScreenY = -80f;

    [Tooltip("Anchored Y position when hidden above the viewport.")]
    [SerializeField] float offScreenY = 150f;

    [Tooltip("When true, the object's initial anchoredPosition.y is used as offScreenY.")]
    [SerializeField] bool useInitialPositionAsOffScreen = true;

    [Tooltip("Duration of the slide-in and slide-out animations in seconds.")]
    [SerializeField, Min(0.05f)] float slideDuration = 0.4f;

    [Tooltip("How long the notification stays visible before sliding back up.")]
    [SerializeField, Min(0f)] float displayDuration = 3f;

    [Header("Text Slots")]
    [Tooltip("TMP text that renders the full sentence. {NAME} is replaced with the badge name, {TIER} with 'Bronze'/'Silver'/'Gold' (omitted for single-tier badges).")]
    [SerializeField] TMP_Text bodyText;

    [Tooltip("Optional uGUI Text when this notification does not use TextMeshPro.")]
    [SerializeField] Text legacyBodyText;

    [Tooltip("Template used when the badge is tiered. Default: 'You just achieved {NAME} ({TIER} Tier)!'")]
    [SerializeField, TextArea(1, 2)] string tieredTemplate = "You just achieved {NAME} ({TIER} Tier)!";

    [Tooltip("Template used when the badge is a single-tier achievement. Default: 'You just achieved {NAME}!'")]
    [SerializeField, TextArea(1, 2)] string singleTierTemplate = "You just achieved {NAME}!";

    RectTransform _rect;
    Coroutine _activeRoutine;

    public bool IsAnimating => _activeRoutine != null;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (useInitialPositionAsOffScreen && _rect != null)
            offScreenY = _rect.anchoredPosition.y;

        SetAnchoredY(offScreenY);

        if (bodyText == null)
            bodyText = GetComponentInChildren<TMP_Text>(true);
        if (legacyBodyText == null)
            legacyBodyText = GetComponentInChildren<Text>(true);
    }

    /// <summary>
    /// Fills the placeholders and runs the slide-in/hold/slide-out sequence.
    /// </summary>
    public void ShowBadge(string badgeName, string tierName)
    {
        bool hasTier = !string.IsNullOrEmpty(tierName);
        string tpl = hasTier ? tieredTemplate : singleTierTemplate;
        string line = tpl
            .Replace("{NAME}", badgeName ?? string.Empty)
            .Replace("{TIER}", tierName ?? string.Empty);
        if (bodyText != null)
            bodyText.text = line;
        if (legacyBodyText != null)
            legacyBodyText.text = line;

        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(RunSlideSequence());
    }

    IEnumerator RunSlideSequence()
    {
        yield return SlideTo(onScreenY);
        yield return new WaitForSeconds(displayDuration);
        yield return SlideTo(offScreenY);
        _activeRoutine = null;
    }

    IEnumerator SlideTo(float targetY)
    {
        float startY = _rect.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            SetAnchoredY(Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        SetAnchoredY(targetY);
    }

    void SetAnchoredY(float y)
    {
        if (_rect == null) return;
        Vector2 pos = _rect.anchoredPosition;
        pos.y = y;
        _rect.anchoredPosition = pos;
    }
}
