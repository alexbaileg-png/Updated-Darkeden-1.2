using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour,
    IPointerDownHandler,
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
    public Sprite mythicalBorder;

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

    void Update()
    {
        if (currentItem == null)
            return;

        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (!IsMouseOverIcon())
            return;

        TryShiftEquip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        SelectedSlot = this;

        foreach (InventorySlot slot in FindObjectsOfType<InventorySlot>())
            slot.RefreshSlot();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        InventorySlot draggedSlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (draggedSlot != null && draggedSlot != this && draggedSlot.currentItem != null)
        {
            HandleInventorySlotDrop(draggedSlot);
            return;
        }

        VialHotbarSlot vialSlot = eventData.pointerDrag.GetComponent<VialHotbarSlot>();

        if (vialSlot != null && vialSlot.currentItem != null)
        {
            HandleVialSlotDrop(vialSlot);
            return;
        }

        EquipmentSlot equipmentSlot = eventData.pointerDrag.GetComponent<EquipmentSlot>();

        if (equipmentSlot != null && equipmentSlot.currentItem != null)
        {
            HandleEquipmentSlotDrop(equipmentSlot);
            return;
        }
    }

    void HandleInventorySlotDrop(InventorySlot draggedSlot)
    {
        ItemData draggedItem = draggedSlot.currentItem;

        bool isEnhancementItem =
            draggedItem.itemType == ItemType.WeaponCrystal ||
            draggedItem.itemType == ItemType.ArmorCrystal ||
            draggedItem.itemType == ItemType.EnchantStone ||
            draggedItem.itemType == ItemType.RefiningStone;

        if (isEnhancementItem && currentItem != null)
        {
            TryApplyEnhancementFromSlot(draggedSlot);
            return;
        }

        if (TryStackFromInventorySlot(draggedSlot))
            return;

        if (currentItem != null)
        {
            ItemData oldItem = currentItem;
            int oldQuantity = quantity;

            SetItem(draggedSlot.currentItem, draggedSlot.quantity);
            draggedSlot.SetItem(oldItem, oldQuantity);
        }
        else
        {
            SetItem(draggedSlot.currentItem, draggedSlot.quantity);
            draggedSlot.ClearSlot();
        }

        SaveInventoryChange();
    }

    void HandleVialSlotDrop(VialHotbarSlot vialSlot)
    {
        if (vialSlot == null || vialSlot.currentItem == null)
            return;

        if (TryStackFromVialSlot(vialSlot))
            return;

        if (currentItem != null)
            return;

        SetItem(vialSlot.currentItem, vialSlot.quantity);
        vialSlot.ClearSlot();

        VialHotbar hotbar = vialSlot.GetComponentInParent<VialHotbar>();
        if (hotbar != null)
            hotbar.SaveSlotsToBelt();

        SaveInventoryChange();
    }

    void HandleEquipmentSlotDrop(EquipmentSlot equipmentSlot)
    {
        if (equipmentSlot == null || equipmentSlot.currentItem == null)
            return;

        if (currentItem != null)
            return;

        SetItem(equipmentSlot.currentItem, 1);
        equipmentSlot.ClearSlot();

        SaveInventoryChange();
    }

    bool TryStackFromInventorySlot(InventorySlot draggedSlot)
    {
        if (draggedSlot == null || draggedSlot.currentItem == null)
            return false;

        if (currentItem == null)
            return false;

        if (!SameItem(currentItem, draggedSlot.currentItem))
            return false;

        if (!currentItem.isStackable)
            return false;

        int remaining = AddToStack(draggedSlot.quantity);

        if (remaining <= 0)
            draggedSlot.ClearSlot();
        else
        {
            draggedSlot.quantity = remaining;
            draggedSlot.RefreshSlot();
        }

        SaveInventoryChange();
        return true;
    }

    bool TryStackFromVialSlot(VialHotbarSlot vialSlot)
    {
        if (vialSlot == null || vialSlot.currentItem == null)
            return false;

        if (currentItem == null)
            return false;

        if (!SameItem(currentItem, vialSlot.currentItem))
            return false;

        if (!currentItem.isStackable)
            return false;

        int remaining = AddToStack(vialSlot.quantity);

        if (remaining <= 0)
            vialSlot.ClearSlot();
        else
        {
            vialSlot.quantity = remaining;
            vialSlot.RefreshSlot();
        }

        VialHotbar hotbar = vialSlot.GetComponentInParent<VialHotbar>();
        if (hotbar != null)
            hotbar.SaveSlotsToBelt();

        SaveInventoryChange();
        return true;
    }

    void TryApplyEnhancementFromSlot(InventorySlot enhancementSlot)
    {
        if (enhancementSlot == null || enhancementSlot.currentItem == null)
            return;

        if (currentItem == null)
            return;

        ItemEnhancementManager manager = FindObjectOfType<ItemEnhancementManager>();

        if (manager == null)
        {
            Debug.LogWarning("No ItemEnhancementManager found.");
            return;
        }

        bool success = manager.UseEnhancementItem(currentItem, enhancementSlot.currentItem);

        if (!success)
            return;

        ItemData usedItem = enhancementSlot.currentItem;

        enhancementSlot.RemoveQuantity(1);
        RefreshSlot();

        SaveInventoryChange();

        Debug.Log("Applied " + usedItem.itemName + " to " + currentItem.itemName);
    }

    bool SameItem(ItemData a, ItemData b)
    {
        if (a == null || b == null)
            return false;

        return !string.IsNullOrEmpty(a.itemId) &&
               !string.IsNullOrEmpty(b.itemId) &&
               a.itemId == b.itemId;
    }

    void TryShiftEquip()
    {
        EquipmentManager equipmentManager = FindObjectOfType<EquipmentManager>();

        if (equipmentManager == null)
            return;

        equipmentManager.TryEquipFromInventory(this);
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
            itemIcon.raycastTarget = false;
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
            bool showQuantity = hasItem && currentItem.isStackable && quantity > 1;
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
            case ItemRarity.Common: return commonBorder;
            case ItemRarity.Uncommon: return uncommonBorder;
            case ItemRarity.Rare: return rareBorder;
            case ItemRarity.Epic: return epicBorder;
            case ItemRarity.Legendary: return legendaryBorder;
            case ItemRarity.Mythical: return mythicalBorder;
        }

        return commonBorder;
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
        isDraggingItem = false;

        if (currentItem == null || itemIcon == null || iconRectTransform == null)
            return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;

        originalIconPosition = iconRectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        HideTooltip();

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

    void SaveInventoryChange()
    {
        if (InventoryPersistenceManager.Instance != null)
            InventoryPersistenceManager.Instance.SaveAfterChange();
    }
}