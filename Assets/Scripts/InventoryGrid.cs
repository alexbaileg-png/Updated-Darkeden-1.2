using UnityEngine;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 5;
    public int columns = 6;

    [Header("Slot Prefab")]
    public GameObject inventorySlotPrefab;

    [Header("Starting Items")]
    public ItemData[] startingItems;

    private InventorySlot[] slots;

    public InventorySlot[] Slots
    {
        get { return slots; }
    }

    void Start()
    {
        BuildGrid();
        AddStartingItems();
    }

    void BuildGrid()
    {
        if (inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryGrid: inventorySlotPrefab is missing.");
            return;
        }

        slots = new InventorySlot[rows * columns];

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObject = Instantiate(inventorySlotPrefab, transform);
            slotObject.name = "InventorySlot_" + (i + 1);

            InventorySlot slot = slotObject.GetComponent<InventorySlot>();

            if (slot == null)
            {
                Debug.LogError("InventoryGrid: slot prefab is missing InventorySlot script.");
                return;
            }

            slots[i] = slot;
        }
    }

    void AddStartingItems()
    {
        if (startingItems == null)
            return;

        foreach (ItemData item in startingItems)
            AddItem(item);
    }

    public void ClearInventory()
    {
        if (slots == null)
            return;

        foreach (InventorySlot slot in slots)
        {
            if (slot != null)
                slot.ClearSlot();
        }
    }

    public bool AddItem(ItemData item)
    {
        return AddItem(item, 1);
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null || slots == null || amount <= 0)
            return false;

        int remaining = amount;

        if (item.isStackable)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot != null && slot.CanStack(item))
                {
                    remaining = slot.AddToStack(remaining);

                    if (remaining <= 0)
                        return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot != null && !slot.HasItem())
            {
                int stackAmount = item.isStackable
                    ? Mathf.Min(remaining, item.maxStackSize)
                    : 1;

                slot.SetItem(item, stackAmount);
                remaining -= stackAmount;

                if (remaining <= 0)
                    return true;
            }
        }

        Debug.Log("Inventory is full.");
        return false;
    }
}