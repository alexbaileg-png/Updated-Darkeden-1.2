using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData currentItem;
    public Image itemIcon;

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

        RefreshSlot();
    }

    public void SetItem(ItemData newItem)
    {
        currentItem = newItem;
        RefreshSlot();
    }

    public void ClearSlot()
    {
        currentItem = null;
        RefreshSlot();
        HideTooltip();
    }

    public bool HasItem()
    {
        return currentItem != null;
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

    public void OnDrop(PointerEventData eventData)
    {
        EquipmentSlot equipmentSlot = eventData.pointerDrag.GetComponent<EquipmentSlot>();

        if (equipmentSlot == null || equipmentSlot.currentItem == null)
            return;

        if (currentItem != null)
        {
            Debug.Log("Inventory slot already has item.");
            return;
        }

        SetItem(equipmentSlot.currentItem);
        equipmentSlot.ClearSlot();

        Debug.Log("Moved equipped item back to inventory.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null)
            Debug.Log("Clicked inventory item: " + currentItem.itemName);
        else
            Debug.Log("Clicked empty inventory slot.");
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