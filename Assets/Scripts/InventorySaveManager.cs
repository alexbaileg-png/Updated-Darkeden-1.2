using UnityEngine;

public class InventorySaveManager : MonoBehaviour
{
    public static InventorySaveManager Instance;

    [Header("References")]
    public InventoryGrid inventoryGrid;
    public EquipmentManager equipmentManager;

    void Awake()
    {
        Instance = this;
    }

    public InventorySaveData CreateSaveData()
    {
        InventorySaveData saveData = new InventorySaveData();

        SaveInventorySlots(saveData);
        SaveEquipmentSlots(saveData);

        return saveData;
    }

    void SaveInventorySlots(InventorySaveData saveData)
    {
        if (inventoryGrid == null || inventoryGrid.Slots == null)
            return;

        for (int i = 0; i < inventoryGrid.Slots.Length; i++)
        {
            InventorySlot slot = inventoryGrid.Slots[i];

            if (slot == null || slot.currentItem == null)
                continue;

            SavedItemData savedItem = ConvertToSavedItem(slot.currentItem, slot.quantity);
            savedItem.slotIndex = i;

            saveData.inventoryItems.Add(savedItem);
        }
    }

    void SaveEquipmentSlots(InventorySaveData saveData)
    {
        if (equipmentManager == null)
            return;

        SaveEquipmentSlot(saveData, equipmentManager.helmetSlot, "Helmet");
        SaveEquipmentSlot(saveData, equipmentManager.necklaceSlot, "Necklace");
        SaveEquipmentSlot(saveData, equipmentManager.topSlot, "Top");
        SaveEquipmentSlot(saveData, equipmentManager.bottomSlot, "Bottom");
        SaveEquipmentSlot(saveData, equipmentManager.bootsSlot, "Boots");
        SaveEquipmentSlot(saveData, equipmentManager.glovesSlot, "Gloves");
        SaveEquipmentSlot(saveData, equipmentManager.beltSlot, "Belt");
        SaveEquipmentSlot(saveData, equipmentManager.leftWeaponSlot, "LeftWeapon");
        SaveEquipmentSlot(saveData, equipmentManager.rightWeaponSlot, "RightWeapon");
    }

    void SaveEquipmentSlot(InventorySaveData saveData, EquipmentSlot slot, string slotId)
    {
        if (slot == null || slot.currentItem == null)
            return;

        SavedItemData savedItem = ConvertToSavedItem(slot.currentItem, 1);
        savedItem.equipmentSlotId = slotId;

        saveData.equippedItems.Add(savedItem);
    }

    SavedItemData ConvertToSavedItem(ItemData item, int quantity)
    {
        SavedItemData save = new SavedItemData();

        save.uniqueItemId = System.Guid.NewGuid().ToString();
        save.baseItemId   = item.itemId;
        save.displayName  = item.itemName;
        save.quantity     = quantity;
        save.rarity       = item.rarity;
        save.sellValue    = item.sellValue;

        save.strengthBonus = item.strengthBonus;
        save.dexterityBonus = item.dexterityBonus;
        save.intelligenceBonus = item.intelligenceBonus;
        save.enduranceBonus = item.enduranceBonus;

        save.healthBonus = item.healthBonus;
        save.manaBonus = item.manaBonus;

        save.meleeDamageBonus = item.meleeDamageBonus;
        save.rangedDamageBonus = item.rangedDamageBonus;
        save.magicalDamageBonus = item.magicalDamageBonus;

        save.armorBonus = item.armorBonus;
        save.resistanceBonus = item.resistanceBonus;
        save.meleeResistanceBonus = item.meleeResistanceBonus;
        save.magicalResistanceBonus = item.magicalResistanceBonus;
        save.allResistanceBonus = item.allResistanceBonus;

        save.hasCrystalStat = item.hasCrystalStat;
        save.crystalStat = item.crystalStat;
        save.crystalCurrentValue = item.crystalCurrentValue;
        save.crystalMaxValue = item.crystalMaxValue;

        if (item.beltSlots != null)
{
    for (int i = 0; i < item.beltSlots.Length; i++)
    {
        SavedItemData beltItem = item.beltSlots[i];

        if (beltItem == null)
            continue;

        beltItem.slotIndex = i;
        save.beltItems.Add(beltItem);
    }
}

        return save;
    }

    public void LoadSaveData(InventorySaveData saveData)
    {
        if (saveData == null)
            return;

        if (inventoryGrid != null)
            inventoryGrid.ClearInventory();

        if (equipmentManager != null)
            equipmentManager.ClearEquipment();

        LoadInventory(saveData);
        LoadEquipment(saveData);

        if (equipmentManager != null)
            equipmentManager.RecalculateEquipmentStats();
    }

    void LoadInventory(InventorySaveData saveData)
    {
        if (inventoryGrid == null || inventoryGrid.Slots == null)
            return;

        foreach (SavedItemData savedItem in saveData.inventoryItems)
        {
            if (savedItem.slotIndex < 0 || savedItem.slotIndex >= inventoryGrid.Slots.Length)
                continue;

            ItemData runtimeItem = CreateRuntimeItem(savedItem);

            if (runtimeItem != null)
                inventoryGrid.Slots[savedItem.slotIndex].SetItem(runtimeItem, savedItem.quantity);
        }
    }

    void LoadEquipment(InventorySaveData saveData)
    {
        if (equipmentManager == null)
            return;

        foreach (SavedItemData savedItem in saveData.equippedItems)
        {
            EquipmentSlot slot = GetEquipmentSlotById(savedItem.equipmentSlotId);

            if (slot == null)
                continue;

            ItemData runtimeItem = CreateRuntimeItem(savedItem);

            if (runtimeItem != null)
                slot.SetItemWithoutRecalculate(runtimeItem);
        }

        equipmentManager.SpawnAllVisuals();
    }

    EquipmentSlot GetEquipmentSlotById(string slotId)
    {
        if (equipmentManager == null)
            return null;

        switch (slotId)
        {
            case "Helmet": return equipmentManager.helmetSlot;
            case "Necklace": return equipmentManager.necklaceSlot;
            case "Top": return equipmentManager.topSlot;
            case "Bottom": return equipmentManager.bottomSlot;
            case "Boots": return equipmentManager.bootsSlot;
            case "Gloves": return equipmentManager.glovesSlot;
            case "Belt": return equipmentManager.beltSlot;
            case "LeftWeapon": return equipmentManager.leftWeaponSlot;
            case "RightWeapon": return equipmentManager.rightWeaponSlot;
        }

        return null;
    }

    ItemData CreateRuntimeItem(SavedItemData savedItem)
    {
        if (savedItem == null || ItemDatabase.Instance == null)
            return null;

        ItemData template = ItemDatabase.Instance.GetItemById(savedItem.baseItemId);

        if (template == null)
        {
            Debug.LogError("Missing template item: " + savedItem.baseItemId);
            return null;
        }

        ItemData runtimeItem = ScriptableObject.Instantiate(template);

        // Restore rolled name if one was saved, otherwise keep template name
        if (!string.IsNullOrEmpty(savedItem.displayName))
            runtimeItem.itemName = savedItem.displayName;

        runtimeItem.rarity    = savedItem.rarity;
        runtimeItem.sellValue = savedItem.sellValue > 0 ? savedItem.sellValue : template.sellValue;

        runtimeItem.strengthBonus = savedItem.strengthBonus;
        runtimeItem.dexterityBonus = savedItem.dexterityBonus;
        runtimeItem.intelligenceBonus = savedItem.intelligenceBonus;
        runtimeItem.enduranceBonus = savedItem.enduranceBonus;

        runtimeItem.healthBonus = savedItem.healthBonus;
        runtimeItem.manaBonus = savedItem.manaBonus;

        runtimeItem.meleeDamageBonus = savedItem.meleeDamageBonus;
        runtimeItem.rangedDamageBonus = savedItem.rangedDamageBonus;
        runtimeItem.magicalDamageBonus = savedItem.magicalDamageBonus;

        runtimeItem.armorBonus = savedItem.armorBonus;
        runtimeItem.resistanceBonus = savedItem.resistanceBonus;
        runtimeItem.meleeResistanceBonus = savedItem.meleeResistanceBonus;
        runtimeItem.magicalResistanceBonus = savedItem.magicalResistanceBonus;
        runtimeItem.allResistanceBonus = savedItem.allResistanceBonus;

        runtimeItem.hasCrystalStat = savedItem.hasCrystalStat;
        runtimeItem.crystalStat = savedItem.crystalStat;
        runtimeItem.crystalCurrentValue = savedItem.crystalCurrentValue;
        runtimeItem.crystalMaxValue = savedItem.crystalMaxValue;

        runtimeItem.beltSlots = new SavedItemData[4];

if (savedItem.beltItems != null && savedItem.beltItems.Count > 0)
{
    foreach (SavedItemData beltItem in savedItem.beltItems)
    {
        if (beltItem == null)
            continue;

        if (beltItem.slotIndex < 0 || beltItem.slotIndex >= runtimeItem.beltSlots.Length)
            continue;

        runtimeItem.beltSlots[beltItem.slotIndex] = beltItem;
    }
}

        return runtimeItem;
    }
}