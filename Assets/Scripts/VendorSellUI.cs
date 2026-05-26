using UnityEngine;
using TMPro;

public class VendorSellUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerGold playerGold;

    [Header("UI")]
    public TMP_Text goldText;
    public TMP_Text selectedItemText;

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (goldText != null && playerGold != null)
            goldText.text = "Gold: " + playerGold.gold;

        if (selectedItemText != null)
        {
            InventorySlot slot = InventorySlot.SelectedSlot;

            if (slot != null && slot.currentItem != null)
            {
                selectedItemText.text =
                    slot.currentItem.itemName +
                    " x" +
                    slot.quantity +
                    "\nSell: " +
                    slot.currentItem.sellValue;
            }
            else
            {
                selectedItemText.text = "No item selected";
            }
        }
    }

    public void SellSelectedItem()
    {
        InventorySlot slot = InventorySlot.SelectedSlot;

        if (slot == null || slot.currentItem == null || playerGold == null)
            return;

        int value = slot.currentItem.sellValue;

        if (value <= 0)
            return;

        playerGold.AddGold(value);
        slot.RemoveQuantity(1);

        Debug.Log("Sold item for " + value + " gold.");
    }

    public void SellFullStack()
    {
        InventorySlot slot = InventorySlot.SelectedSlot;

        if (slot == null || slot.currentItem == null || playerGold == null)
            return;

        int value = slot.currentItem.sellValue;

        if (value <= 0)
            return;

        int totalValue = value * slot.quantity;

        playerGold.AddGold(totalValue);
        slot.ClearSlot();

        Debug.Log("Sold stack for " + totalValue + " gold.");
    }
}