using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to each message bubble root. The ChatManager calls SetContent
/// to populate the bubble; ContentSizeFitter handles the rest.
/// </summary>
public class MessageBubbleController : MonoBehaviour
{
    [SerializeField] TMP_Text senderNameLabel;
    [SerializeField] TMP_Text messageTextLabel;

    [Tooltip("Maximum bubble width as a fraction of the scroll view width")]
    [SerializeField, Range(0.3f, 1f)] float maxWidthRatio = 0.85f;

    public void SetContent(string sender, string message)
    {
        if (senderNameLabel != null) senderNameLabel.text = sender;
        if (messageTextLabel != null) messageTextLabel.text = message;

        ClampBubbleWidth();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetMessage(string message)
    {
        if (messageTextLabel != null) messageTextLabel.text = message;

        ClampBubbleWidth();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    void ClampBubbleWidth()
    {
        var root = GetComponent<RectTransform>();
        var container = root.parent as RectTransform;
        if (container == null) return;

        // Scroll content often reports width 0 on the same frame the bubble is spawned;
        // clamping to that collapses the bubble and TMP stacks one character per line.
        float usableWidth = ResolveMessageAreaWidth(container);
        float maxWidth = Mathf.Max(usableWidth * maxWidthRatio, 80f);

        var bubbleBody = root.childCount > 0 ? root.GetChild(0).GetComponent<RectTransform>() : null;
        if (bubbleBody == null) return;

        var csf = bubbleBody.GetComponent<ContentSizeFitter>();
        if (csf != null)
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleBody);

        if (bubbleBody.rect.width > maxWidth)
        {
            if (csf != null)
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bubbleBody.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
        }
    }

    static float ResolveMessageAreaWidth(RectTransform container)
    {
        Canvas.ForceUpdateCanvases();

        for (RectTransform r = container; r != null; r = r.parent as RectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(r);
            float w = r.rect.width;
            if (w > 1f)
                return w;
        }

        return 400f;
    }
}
