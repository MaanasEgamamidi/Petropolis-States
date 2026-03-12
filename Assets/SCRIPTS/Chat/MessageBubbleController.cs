using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to each message bubble root. The BernieChatManager calls SetContent
/// to populate the bubble; ContentSizeFitter handles the rest.
/// </summary>
public class MessageBubbleController : MonoBehaviour
{
    [SerializeField] TMP_Text senderNameLabel;
    [SerializeField] TMP_Text messageTextLabel;

    public void SetContent(string sender, string message)
    {
        if (senderNameLabel != null) senderNameLabel.text = sender;
        if (messageTextLabel != null) messageTextLabel.text = message;

        // Force layout rebuild so the bubble sizes immediately
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetMessage(string message)
    {
        if (messageTextLabel != null) messageTextLabel.text = message;
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
