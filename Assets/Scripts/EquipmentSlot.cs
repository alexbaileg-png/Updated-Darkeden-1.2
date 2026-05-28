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
    public Image itemIcon;

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
        return item != null && item.itemType == allowedItemType;
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
        if (itemIcon == null)
            return;

        if (currentItem != null && currentItem.itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = currentItem.itemIcon;
            itemIcon.raycastTarget = true;
        }
        else
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
            itemIcon.raycastTarget = false;
        }

        if (iconRectTransform != null)
            iconRectTransform.anchoredPosition = Vector2.zero;
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

        ItemData draggedItem = inventorySlot.currentItem;
        ItemData oldEquippedItem = currentItem;

        SetItemWithoutRecalculate(draggedItem);

        if (oldEquippedItem != null)
            inventorySlot.SetItem(oldEquippedItem, 1);
        else
            inventorySlot.ClearSlot();

        if (equipmentManager != null)
            equipmentManager.RecalculateEquipmentStats();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && ItemTooltip.Instance != null)
            ItemTooltip.Instance.ShowTooltip(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    void HideTooltip()
    {
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || itemIcon == null)
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