using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class VendorBuySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Item")]
    public ItemData itemForSale;
    public int price = 10;

    [Header("UI")]
    public Image itemIcon;
    public Image rarityBorder;
    public TMP_Text priceText;

    [Header("Rarity Borders")]
    public Sprite commonBorder;
    public Sprite uncommonBorder;
    public Sprite rareBorder;
    public Sprite epicBorder;
    public Sprite legendaryBorder;

    [Header("Vendor")]
    public VendorBuyUI vendorBuyUI;

    public void Setup(ItemData item, int itemPrice, VendorBuyUI buyUI)
    {
        itemForSale = item;
        price = itemPrice;
        vendorBuyUI = buyUI;

        RefreshSlot();
    }

    public void RefreshSlot()
    {
        bool hasItem = itemForSale != null;

        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem && itemForSale.itemIcon != null;
            itemIcon.sprite = hasItem ? itemForSale.itemIcon : null;
            itemIcon.raycastTarget = true;
        }

        if (rarityBorder != null)
        {
            rarityBorder.enabled = hasItem;
            rarityBorder.raycastTarget = false;

            if (hasItem)
            {
                rarityBorder.sprite = GetBorderSprite(itemForSale.rarity);
                rarityBorder.color = Color.white;
            }
        }

        if (priceText != null)
            priceText.text = hasItem ? price + "g" : "";
    }

    Sprite GetBorderSprite(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return commonBorder;
            case ItemRarity.Uncommon:
                return uncommonBorder;
            case ItemRarity.Rare:
                return rareBorder;
            case ItemRarity.Epic:
                return epicBorder;
            case ItemRarity.Legendary:
                return legendaryBorder;
        }

        return commonBorder;
    }

    public void BuyItem()
    {
        if (vendorBuyUI != null)
            vendorBuyUI.BuyItem(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemForSale == null)
            return;

        if (VendorHoverInfoUI.Instance != null)
            VendorHoverInfoUI.Instance.ShowItem(itemForSale, price);
    }

    public void OnPointerExit(PointerEventData eventData)
{
    // Do nothing.
    // The panel stays open until another item replaces it
    // or the BuyPanel closes.
}
}