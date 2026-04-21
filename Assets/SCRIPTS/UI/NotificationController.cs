using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable slide-down notification. Place on any child of a notifications canvas.
/// Call <see cref="Show"/> to slide down, hold, then slide back up.
///
/// Auto-trigger modes:
///   - OnSceneStart: fires as soon as this object's Start runs (i.e. when the World scene loads).
///   - AfterPetChosen: fires when <see cref="GlobalData.StoredPetSkin"/> becomes > 0 and we
///     haven't shown for that pet index yet. Handles both "World loads after PetChoose" and
///     "World unfreezes after additive PetChoose".
///
/// Positioning: the object should start off-screen in the editor. If
/// <see cref="useInitialPositionAsOffScreen"/> is true (default), the script reads the current
/// anchoredPosition.y in Awake and uses it as offScreenY automatically.
/// </summary>
public class NotificationController : MonoBehaviour
{
    public enum AutoTrigger { None, OnSceneStart, AfterPetChosen }

    [Header("Auto-trigger (optional)")]
    [Tooltip("OnSceneStart = show when World loads. AfterPetChosen = show when returning to World after choosing a pet.")]
    [SerializeField] AutoTrigger autoTriggerEvent = AutoTrigger.None;

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

    [Tooltip("Delay before showing after trigger fires.")]
    [SerializeField, Min(0f)] float triggerDelay = 1f;

    RectTransform _rect;
    Coroutine _activeRoutine;

    Text _legacyText;
    TMP_Text _tmpText;
    bool _textResolved;

    static bool _startInfoShownThisSession;
    static int _lastPetSkinShown = -1;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (useInitialPositionAsOffScreen && _rect != null)
            offScreenY = _rect.anchoredPosition.y;

        SetAnchoredY(offScreenY);

        // Global 2x boost to on-screen hold time for all notifications.
        displayDuration *= 2f;
    }

    void Start()
    {
        if (autoTriggerEvent == AutoTrigger.OnSceneStart && !_startInfoShownThisSession)
        {
            _startInfoShownThisSession = true;
            TriggerDelayedShow();
        }
    }

    void OnEnable()
    {
        if (autoTriggerEvent == AutoTrigger.AfterPetChosen)
            TryShowForPetChosen();
    }

    void TryShowForPetChosen()
    {
        int current = GlobalData.StoredPetSkin;
        if (current > 0 && current != _lastPetSkinShown)
        {
            _lastPetSkinShown = current;
            TriggerDelayedShow();
        }
    }

    void TriggerDelayedShow()
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(DelayedShow(triggerDelay));
    }

    IEnumerator DelayedShow(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return RunSlideSequence(displayDuration);
    }

    /// <summary>Immediately starts the slide-down, hold, slide-up sequence.</summary>
    public void Show()
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(RunSlideSequence(displayDuration));
    }

    /// <summary>
    /// Replaces the notification text and shows it for <paramref name="holdSeconds"/> seconds
    /// (slides down, holds, slides up). Pass a negative value to use the configured
    /// <see cref="displayDuration"/>.
    /// </summary>
    public void ShowMessage(string message, float holdSeconds = -1f)
    {
        SetText(message);

        float hold = holdSeconds < 0f ? displayDuration : holdSeconds;

        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(RunSlideSequence(hold));
    }

    void SetText(string message)
    {
        if (!_textResolved)
        {
            _tmpText = GetComponentInChildren<TMP_Text>(true);
            _legacyText = GetComponentInChildren<Text>(true);
            _textResolved = true;
        }

        if (_tmpText != null) _tmpText.text = message;
        if (_legacyText != null) _legacyText.text = message;
    }

    IEnumerator RunSlideSequence(float holdSeconds)
    {
        yield return SlideTo(onScreenY);
        yield return new WaitForSeconds(holdSeconds);
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
