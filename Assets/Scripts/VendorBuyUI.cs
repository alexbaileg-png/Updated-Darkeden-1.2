using UnityEngine;
using TMPro;

[System.Serializable]
public class VendorStockItem
{
    public ItemData item;
    public int price = 10;
}

public class VendorBuyUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerGold playerGold;
    public InventoryGrid inventoryGrid;

    [Header("Vendor Stock")]
    public VendorStockItem[] stockItems;

    [Header("UI")]
    public Transform buyGridParent;
    public GameObject buySlotPrefab;
    public TMP_Text goldText;
    public TMP_Text messageText;

    void OnEnable()
    {
        RefreshGoldText();
        BuildBuyGrid();
    }

    public void BuildBuyGrid()
    {
        if (buyGridParent == null || buySlotPrefab == null)
            return;

        foreach (Transform child in buyGridParent)
            Destroy(child.gameObject);

        if (stockItems == null)
            return;

        foreach (VendorStockItem stock in stockItems)
        {
            if (stock == null || stock.item == null)
                continue;

            GameObject slotObject = Instantiate(buySlotPrefab, buyGridParent);

            VendorBuySlot slot = slotObject.GetComponent<VendorBuySlot>();

            if (slot != null)
                slot.Setup(stock.item, stock.price, this);
        }
    }

    public void BuyItem(VendorBuySlot slot)
    {
        if (slot == null || slot.itemForSale == null)
            return;

        if (playerGold == null || inventoryGrid == null)
            return;

        if (playerGold.gold < slot.price)
        {
            SetMessage("Not enough gold.");
            return;
        }

        bool added = inventoryGrid.AddItem(slot.itemForSale, 1);

        if (!added)
        {
            SetMessage("Inventory full.");
            return;
        }

        playerGold.SpendGold(slot.price);
        RefreshGoldText();

        SetMessage("Bought " + slot.itemForSale.itemName + ".");
    }

    void RefreshGoldText()
    {
        if (goldText != null && playerGold != null)
            goldText.text = "Gold: " + playerGold.gold;
    }

    void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}