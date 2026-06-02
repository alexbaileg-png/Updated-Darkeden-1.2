using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class VialHotbarSlot : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Slot")]
    public int hotkeyNumber = 1;

    [Header("Item")]
    public ItemData currentItem;
    public int quantity;

    [Header("UI")]
    public Image itemIcon;
    public TMP_Text quantityText;
    public TMP_Text hotkeyText;

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform iconRectTransform;
    private Vector2 originalIconPosition;
    private bool isDraggingItem = false;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (itemIcon != null)
            iconRectTransform = itemIcon.GetComponent<RectTransform>();

        RefreshSlot();
    }

    public void SetItem(ItemData item, int amount)
    {
        currentItem = item;
        quantity = Mathf.Max(1, amount);
        RefreshSlot();
    }

    public void ClearSlot()
    {
        currentItem = null;
        quantity = 0;
        RefreshSlot();
    }

    public bool HasItem()
    {
        return currentItem != null && quantity > 0;
    }

    public bool CanStack(ItemData item)
    {
        return currentItem != null &&
               item != null &&
               SameItem(currentItem, item) &&
               currentItem.isStackable &&
               quantity < currentItem.maxStackSize;
    }

    public int AddToStack(int amount)
    {
        if (currentItem == null || !currentItem.isStackable)
            return amount;

        int spaceLeft = currentItem.maxStackSize - quantity;
        int amountToAdd = Mathf.Min(spaceLeft, amount);

        quantity += amountToAdd;
        RefreshSlot();

        return amount - amountToAdd;
    }

    public void Use(PlayerStats playerStats)
    {
        if (playerStats == null || currentItem == null || quantity <= 0)
            return;

        if (currentItem.itemType == ItemType.HealthVial)
            playerStats.Heal(currentItem.restoreAmount);
        else if (currentItem.itemType == ItemType.ManaVial)
            playerStats.RestoreMana(currentItem.restoreAmount);
        else
            return;

        quantity--;

        if (quantity <= 0)
            ClearSlot();
        else
            RefreshSlot();

        SaveToBeltAndInventory();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        InventorySlot inventorySlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (inventorySlot != null && inventorySlot.currentItem != null)
        {
            HandleInventoryDrop(inventorySlot);
            return;
        }

        VialHotbarSlot otherVialSlot = eventData.pointerDrag.GetComponent<VialHotbarSlot>();

        if (otherVialSlot != null && otherVialSlot != this && otherVialSlot.currentItem != null)
        {
            HandleVialSlotDrop(otherVialSlot);
            return;
        }
    }

    void HandleInventoryDrop(InventorySlot inventorySlot)
    {
        ItemData item = inventorySlot.currentItem;

        if (!IsValidVial(item))
            return;

        if (currentItem != null && !SameItem(currentItem, item))
            return;

        if (currentItem == null)
        {
            SetItem(item, inventorySlot.quantity);
            inventorySlot.ClearSlot();
        }
        else
        {
            int remaining = AddToStack(inventorySlot.quantity);

            if (remaining <= 0)
                inventorySlot.ClearSlot();
            else
            {
                inventorySlot.quantity = remaining;
                inventorySlot.RefreshSlot();
            }
        }

        SaveToBeltAndInventory();
    }

    void HandleVialSlotDrop(VialHotbarSlot otherSlot)
    {
        if (otherSlot == null || otherSlot.currentItem == null)
            return;

        if (!IsValidVial(otherSlot.currentItem))
            return;

        if (currentItem != null && !SameItem(currentItem, otherSlot.currentItem))
            return;

        if (currentItem == null)
        {
            SetItem(otherSlot.currentItem, otherSlot.quantity);
            otherSlot.ClearSlot();
        }
        else
        {
            int remaining = AddToStack(otherSlot.quantity);

            if (remaining <= 0)
                otherSlot.ClearSlot();
            else
            {
                otherSlot.quantity = remaining;
                otherSlot.RefreshSlot();
            }
        }

        SaveToBeltAndInventory();
    }

    bool IsValidVial(ItemData item)
    {
        return item != null &&
               (item.itemType == ItemType.HealthVial ||
                item.itemType == ItemType.ManaVial);
    }

    bool SameItem(ItemData a, ItemData b)
    {
        if (a == null || b == null)
            return false;

        return !string.IsNullOrEmpty(a.itemId) &&
               !string.IsNullOrEmpty(b.itemId) &&
               a.itemId == b.itemId;
    }

    public void RefreshSlot()
    {
        bool hasItem = currentItem != null && quantity > 0;

        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem && currentItem.itemIcon != null;
            itemIcon.sprite = hasItem ? currentItem.itemIcon : null;
            itemIcon.raycastTarget = false;
        }

        if (quantityText != null)
        {
            quantityText.text = hasItem && quantity > 1 ? quantity.ToString() : "";
            quantityText.raycastTarget = false;
        }

        if (hotkeyText != null)
        {
            hotkeyText.text = "F" + hotkeyNumber;
            hotkeyText.raycastTarget = false;
        }

        if (iconRectTransform != null)
            iconRectTransform.anchoredPosition = Vector2.zero;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && ItemTooltip.Instance != null)
            ItemTooltip.Instance.ShowTooltip(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDraggingItem = false;

        if (currentItem == null || itemIcon == null || iconRectTransform == null)
            return;

        originalIconPosition = iconRectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;

        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.HideTooltip();

        isDraggingItem = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingItem)
            return;

        if (currentItem == null || itemIcon == null || parentCanvas == null)
            return;

        iconRectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingItem)
            return;

        isDraggingItem = false;
        canvasGroup.blocksRaycasts = true;

        if (iconRectTransform != null)
            iconRectTransform.anchoredPosition = originalIconPosition;
    }

    void SaveToBeltAndInventory()
    {
        if (VialHotbar.Instance != null)
            VialHotbar.Instance.SaveSlotsToBelt();

        if (InventoryPersistenceManager.Instance != null)
            InventoryPersistenceManager.Instance.SaveAfterChange();
    }
}