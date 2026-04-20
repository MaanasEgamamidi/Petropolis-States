using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row in the inventory panel. Bound once to a specific item name at spawn;
/// <see cref="Refresh"/> re-reads the current owned quantity from <see cref="GlobalData"/>.
/// The Use button calls <see cref="GlobalData.TryUseItem"/>, which decrements the count by one
/// and applies the per-use <see cref="GlobalData.ItemEffect"/> to the pet's stats.
/// </summary>
public class InventoryRowView : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text amountText;
    [SerializeField] TMP_Text effectText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Button useButton;

    string _itemName;

    /// <summary>Fires when this row's Use button was clicked (for optional parent-side audio / notifications).</summary>
    public System.Action<string> OnUseClicked;

    public string ItemName => _itemName;

    public void Bind(string itemName)
    {
        _itemName = itemName;

        if (nameText != null)
            nameText.text = itemName;

        if (costText != null && GlobalData.Inventory.TryGetValue(itemName, out float[] entry))
            costText.text = $"${entry[GlobalData.InventoryPriceIndex]:0}";
        else if (costText != null)
            costText.text = "—";

        if (effectText != null)
            effectText.text = FormatEffect(itemName);

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(HandleUseClicked);
        }

        Refresh();
    }

    public void Refresh()
    {
        int qty = GlobalData.GetItemQuantity(_itemName);
        if (amountText != null)
            amountText.text = qty.ToString();
        if (useButton != null)
            useButton.interactable = qty > 0;
    }

    void HandleUseClicked()
    {
        if (string.IsNullOrEmpty(_itemName)) return;
        if (GlobalData.TryUseItem(_itemName))
            OnUseClicked?.Invoke(_itemName);
    }

    static string FormatEffect(string itemName)
    {
        if (!GlobalData.ItemEffects.TryGetValue(itemName, out GlobalData.ItemEffect effect))
            return "—";
        return $"+{effect.Amount:0} {effect.Stat}";
    }
}
