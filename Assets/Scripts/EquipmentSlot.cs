using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlot : MonoBehaviour,
    IDropHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public ItemType allowedItemType;
    public ItemData currentItem;

    [Header("UI")]
    public Image itemIcon;
    public Image rarityBorder;

    [Header("Rarity Borders")]
    public Sprite commonBorder;
    public Sprite uncommonBorder;
    public Sprite rareBorder;
    public Sprite epicBorder;
    public Sprite legendaryBorder;
    public Sprite mythicalBorder;

    [Header("Tooltip Override")]
    public ItemTooltip tooltipOverride;

    public EquipmentManager equipmentManager;

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform iconRectTransform;
    private Vector2 originalIconPosition;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (itemIcon != null)
            iconRectTransform = itemIcon.GetComponent<RectTransform>();

        if (equipmentManager == null)
            equipmentManager = FindObjectOfType<EquipmentManager>();

        RefreshSlot();
    }

    public bool CanEquip(ItemData item)
    {
        if (item == null) return false;
        if (item.itemType == allowedItemType) return true;
        // Vampire equivalents share the same slot
        if (allowedItemType == ItemType.Gloves && item.itemType == ItemType.Armguard) return true;
        if (allowedItemType == ItemType.Belt   && item.itemType == ItemType.Sash)     return true;
        return false;
    }

    public void EquipItem(ItemData item)
    {
        currentItem = item;
        RefreshSlot();

        if (equipmentManager != null)
            equipmentManager.RecalculateEquipmentStats();
    }

    public void SetItemWithoutRecalculate(ItemData item)
    {
        currentItem = item;
        RefreshSlot();
    }

    public void ClearSlot()
    {
        currentItem = null;
        RefreshSlot();
        HideTooltip();

        if (equipmentManager != null)
            equipmentManager.RecalculateEquipmentStats();
    }

    public void RefreshSlot()
    {
        bool hasItem = currentItem != null;

        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem && currentItem.itemIcon != null;
            itemIcon.sprite = hasItem ? currentItem.itemIcon : null;
            itemIcon.raycastTarget = true;
        }

        if (rarityBorder != null)
        {
            rarityBorder.enabled = hasItem;
            rarityBorder.raycastTarget = false;

            if (hasItem)
            {
                rarityBorder.sprite = GetBorderSprite(currentItem.rarity);
                rarityBorder.color = Color.white;
            }
        }

        if (iconRectTransform != null)
            iconRectTransform.anchoredPosition = Vector2.zero;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (equipmentManager == null)
                equipmentManager = FindObjectOfType<EquipmentManager>();

            if (equipmentManager != null)
                equipmentManager.TryUnequipToInventory(this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        InventorySlot inventorySlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (inventorySlot == null || inventorySlot.currentItem == null)
            return;

        if (!CanEquip(inventorySlot.currentItem))
            return;

        // Faction restriction check
        if (equipmentManager != null && !equipmentManager.TryEquipFromInventory(inventorySlot))
            return;

        ItemData draggedItem = inventorySlot.currentItem;
        ItemData oldEquippedItem = currentItem;

        SetItemWithoutRecalculate(draggedItem);

        if (oldEquippedItem != null)
            inventorySlot.SetItem(oldEquippedItem, 1);
        else
            inventorySlot.ClearSlot();

        if (equipmentManager != null)
            equipmentManager.RecalculateEquipmentStats();

        if (InventoryPersistenceManager.Instance != null)
            InventoryPersistenceManager.Instance.SaveAfterChange();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        ItemTooltip tooltip =
            tooltipOverride != null
                ? tooltipOverride
                : ItemTooltip.Instance;

        if (tooltip != null)
            tooltip.ShowTooltip(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    void HideTooltip()
    {
        ItemTooltip tooltip =
            tooltipOverride != null
                ? tooltipOverride
                : ItemTooltip.Instance;

        if (tooltip != null)
            tooltip.HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || itemIcon == null || iconRectTransform == null)
            return;

        HideTooltip();

        originalIconPosition = iconRectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem == null || itemIcon == null || parentCanvas == null)
            return;

        iconRectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemIcon == null)
            return;

        canvasGroup.blocksRaycasts = true;

        if (iconRectTransform != null)
            iconRectTransform.anchoredPosition = originalIconPosition;
    }
}