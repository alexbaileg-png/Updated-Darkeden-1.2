using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LootBagSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image itemIcon;
    public Image rarityBorder;
    public TMP_Text itemNameText;
    public TMP_Text quantityText;

    public Sprite commonBorder;
    public Sprite uncommonBorder;
    public Sprite rareBorder;
    public Sprite epicBorder;
    public Sprite legendaryBorder;
    public Sprite mythicalBorder;

    private LootBagUI lootBagUI;
    private int index;
    private ItemData item;

    public void Setup(LootBagUI ui, LootBag bag, int slotIndex, ItemData itemData, int quantity)
    {
        lootBagUI = ui;
        index = slotIndex;
        item = itemData;

        if (itemIcon != null)
        {
            itemIcon.enabled = item != null && item.itemIcon != null;
            itemIcon.sprite = item != null ? item.itemIcon : null;
            itemIcon.raycastTarget = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = item != null ? item.itemName : "";
            itemNameText.color = item != null ? ItemRarityColors.GetColor(item.rarity) : Color.white;
            itemNameText.raycastTarget = false;
        }

        if (quantityText != null)
        {
            quantityText.text = quantity > 1 ? "x" + quantity : "";
            quantityText.raycastTarget = false;
        }

        if (rarityBorder != null)
        {
            rarityBorder.enabled = item != null;

            if (item != null)
            {
                rarityBorder.sprite = GetBorderSprite(item.rarity);
                rarityBorder.color = Color.white;
            }

            rarityBorder.raycastTarget = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (lootBagUI == null || item == null)
            return;

        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (shiftHeld)
            lootBagUI.TakeEntireStack(index);
        else
            lootBagUI.TakeSlot(index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lootBagUI != null && item != null)
            lootBagUI.ShowHoverInfo(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Do nothing.
        // Prevents hover info panel flicker when the panel overlaps nearby slots.
    }

    Sprite GetBorderSprite(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonBorder;
            case ItemRarity.Uncommon: return uncommonBorder;
            case ItemRarity.Rare: return rareBorder;
            case ItemRarity.Epic: return epicBorder;
            case ItemRarity.Legendary: return legendaryBorder;
            case ItemRarity.Mythical: return mythicalBorder;
        }

        return commonBorder;
    }
}