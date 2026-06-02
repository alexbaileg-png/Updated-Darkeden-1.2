using System;
using System.Collections.Generic;

[Serializable]
public class InventorySaveData
{
    // INVENTORY SLOTS
    public List<SavedItemData> inventoryItems =
        new List<SavedItemData>();

    // EQUIPPED ITEMS
    public List<SavedItemData> equippedItems =
        new List<SavedItemData>();
}