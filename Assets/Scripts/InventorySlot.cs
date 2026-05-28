using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public static InventorySlot SelectedSlot;

    [Header("Item Data")]
    public ItemData currentItem;
    public int quantity = 0;

    [Header("UI")]
    public Image itemIcon;
    public Image rarityBorder;
    public Image selectedHighlight;
    public TMP_Text quantityText;

    [Header("Rarity Borders")]
    public Sprite commonBorder;
    public Sprite uncommonBorder;
    public Sprite rareBorder;
    public Sprite epicBorder;
    public Sprite legendaryBorder;

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform iconRectTransform;
    private Vector2 originalIconPosition;

    private bool isDraggingItem = false;
    private bool mouseOverSlot = false;

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

    void Update()
    {
        if (currentItem == null)
            return;

        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (!shiftHeld)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (!IsMouseOverIcon())
            return;

        TryShiftEquip();
    }

    bool IsMouseOverIcon()
    {
        if (iconRectTransform == null)
            return false;

        Camera uiCamera = null;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = parentCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            iconRectTransform,
            Input.mousePosition,
            uiCamera
        );
    }

    void TryShiftEquip()
    {
        if (currentItem == null)
            return;

        EquipmentManager equipmentManager = FindObjectOfType<EquipmentManager>();

        if (equipmentManager == null)
        {
            Debug.LogWarning("Shift equip failed: No active EquipmentManager found.");
            return;
        }

        bool equipped = equipmentManager.TryEquipFromInventory(this);

        if (!equipped)
        {
            Debug.LogWarning(
                "Shift equip failed for: " +
                currentItem.itemName +
                " | ItemType: " +
                currentItem.itemType
            );
        }
    }

    public void SetItem(ItemData newItem, int amount = 1)
    {
        currentItem = newItem;
        quantity = Mathf.Max(1, amount);
        RefreshSlot();
    }

    public void ClearSlot()
    {
        if (SelectedSlot == this)
            SelectedSlot = null;

        currentItem = null;
        quantity = 0;

        RefreshSlot();
        HideTooltip();
    }

    public bool HasItem()
    {
        return currentItem != null;
    }

    public bool CanStack(ItemData item)
    {
        return currentItem == item &&
               currentItem != null &&
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

    public bool RemoveQuantity(int amount)
    {
        if (currentItem == null)
            return false;

        quantity -= amount;

        if (quantity <= 0)
            ClearSlot();
        else
            RefreshSlot();

        return true;
    }

    public void RefreshSlot()
    {
        bool hasItem = currentItem != null;

        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem && currentItem.itemIcon != null;
            itemIcon.sprite = hasItem ? currentItem.itemIcon : null;
            itemIcon.raycastTarget = hasItem;
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

        if (selectedHighlight != null)
        {
            selectedHighlight.enabled = SelectedSlot == this && hasItem;
            selectedHighlight.raycastTarget = false;
        }

        if (quantityText != null)
        {
            bool showQuantity =
                hasItem &&
                currentItem.isStackable &&
                quantity > 1;

            quantityText.gameObject.SetActive(showQuantity);
            quantityText.text = showQuantity ? quantity.ToString() : "";
            quantityText.raycastTarget = false;
        }

        if (iconRectTransform != null)
            iconRectTransform.anchoredPosition = Vector2.zero;
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

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        EquipmentSlot equipmentSlot =
            eventData.pointerDrag.GetComponent<EquipmentSlot>();

        if (equipmentSlot == null || equipmentSlot.currentItem == null)
            return;

        if (currentItem != null)
            return;

        SetItem(equipmentSlot.currentItem, 1);
        equipmentSlot.ClearSlot();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOverSlot = true;

        if (currentItem != null && ItemTooltip.Instance != null)
            ItemTooltip.Instance.ShowTooltip(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOverSlot = false;
        HideTooltip();
    }

    void HideTooltip()
    {
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDraggingItem = false;

        if (currentItem == null || itemIcon == null || iconRectTransform == null)
            return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;

        if (!IsPointerOverIcon(eventData))
            return;

        HideTooltip();

        originalIconPosition = iconRectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        isDraggingItem = true;
    }

    bool IsPointerOverIcon(PointerEventData eventData)
    {
        if (iconRectTransform == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            iconRectTransform,
            eventData.position,
            eventData.pressEventCamera
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingItem)
            return;

        if (currentItem == null || itemIcon == null || parentCanvas == null)
            return;

        iconRectTransform.anchoredPosition +=
            eventData.delta / parentCanvas.scaleFactor;
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
}