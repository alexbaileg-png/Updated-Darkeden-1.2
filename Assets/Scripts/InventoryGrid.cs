using UnityEngine;
using UnityEngine.UI;

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
        {
            AddItem(item);
        }
    }

    public bool AddItem(ItemData item)
    {
        if (item == null || slots == null)
            return false;

        foreach (InventorySlot slot in slots)
        {
            if (slot != null && !slot.HasItem())
            {
                slot.SetItem(item);
                return true;
            }
        }

        Debug.Log("Inventory is full.");
        return false;
    }
}