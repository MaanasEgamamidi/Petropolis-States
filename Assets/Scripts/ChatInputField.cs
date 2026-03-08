using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attaches to a TMP_InputField and makes it expand vertically as the user types,
/// clamped between a minimum and maximum height — like iMessage.
/// Enter sends; Shift+Enter inserts a newline (handled by TMP_InputField.lineType = MultiLineSubmit).
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class ChatInputField : MonoBehaviour
{
    [Header("Height Limits")]
    [SerializeField] float minHeight = 48f;
    [SerializeField] float maxHeight = 160f;

    /// <summary>Fired when the user presses Enter (or clicks Send). Carries the trimmed text.</summary>
    public event Action<string> OnSubmitMessage;

    TMP_InputField _input;
    RectTransform  _rect;
    LayoutElement  _layoutElement;

    void Awake()
    {
        _input         = GetComponent<TMP_InputField>();
        _rect          = GetComponent<RectTransform>();
        _layoutElement = GetComponent<LayoutElement>();

        // MultiLineSubmit: Enter submits, Shift+Enter inserts a newline
        _input.lineType = TMP_InputField.LineType.MultiLineSubmit;

        _input.onValueChanged.AddListener(_ => RefreshHeight());
        _input.onSubmit.AddListener(OnTMPSubmit);
    }

    void Start()
    {
        SetHeight(minHeight);
        _input.ActivateInputField();
    }

    void OnTMPSubmit(string submittedText)
    {
        string trimmed = submittedText.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        // Fire before TMP clears itself so subscribers can act on the text
        OnSubmitMessage?.Invoke(trimmed);

        // Re-activate so the user can keep typing after sending
        _input.ActivateInputField();
    }

    public void RefreshHeight()
    {
        if (_input.textComponent == null) return;

        _input.textComponent.ForceMeshUpdate();
        float preferred = _input.textComponent.preferredHeight + 24f;
        SetHeight(Mathf.Clamp(preferred, minHeight, maxHeight));
    }

    void SetHeight(float h)
    {
        Vector2 sd = _rect.sizeDelta;
        sd.y = h;
        _rect.sizeDelta = sd;

        if (_layoutElement != null)
        {
            _layoutElement.preferredHeight = h;
            _layoutElement.minHeight       = h;
        }
    }

    /// <summary>Clear text and reset to minimum height.</summary>
    public void Clear()
    {
        _input.SetTextWithoutNotify(string.Empty);
        SetHeight(minHeight);
    }

    public string Text => _input.text;
    public TMP_InputField InputField => _input;
}
