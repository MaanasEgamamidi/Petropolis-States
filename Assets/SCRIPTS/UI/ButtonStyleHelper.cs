using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared UI tweaks for <see cref="Button"/> components.</summary>
public static class ButtonStyleHelper
{
    const float DisabledAlpha = 0.48f;

    /// <summary>Attaches a <see cref="CanvasGroup"/> on the button root so alpha can dim image and text together.</summary>
    public static CanvasGroup EnsureDimCanvasGroup(Button button)
    {
        if (button == null) return null;
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    /// <summary>Sets interactable state and a darker overall tint when disabled (via CanvasGroup alpha).</summary>
    public static void ApplyInteractableWithDim(Button button, CanvasGroup dimGroup, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
        if (dimGroup != null)
            dimGroup.alpha = interactable ? 1f : DisabledAlpha;
    }
}
