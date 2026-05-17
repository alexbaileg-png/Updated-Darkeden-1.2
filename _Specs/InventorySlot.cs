using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot Data")]
    public ItemData currentItem;

    [Header("UI")]
    public Image itemIcon;

    void Start()
    {
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
        }
        else
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            Debug.Log("Clicked inventory item: " + currentItem.itemName);
        }
        else
        {
            Debug.Log("Clicked empty inventory slot.");
        }
    }
}