using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Player")]
    public PlayerStats playerStats;

    [Header("Inventory")]
    public InventoryGrid inventoryGrid;

    [Header("Equipment Slots")]
    public EquipmentSlot helmetSlot;
    public EquipmentSlot necklaceSlot;
    public EquipmentSlot topSlot;
    public EquipmentSlot bottomSlot;
    public EquipmentSlot bootsSlot;
    public EquipmentSlot glovesSlot;
    public EquipmentSlot beltSlot;
    public EquipmentSlot leftWeaponSlot;
    public EquipmentSlot rightWeaponSlot;

    public EquipmentSlot[] EquipmentSlots
    {
        get
        {
            return new EquipmentSlot[]
            {
                helmetSlot,
                necklaceSlot,
                topSlot,
                bottomSlot,
                bootsSlot,
                glovesSlot,
                beltSlot,
                leftWeaponSlot,
                rightWeaponSlot
            };
        }
    }

    void Start()
    {
        RecalculateEquipmentStats();
    }

    public void ClearEquipment()
    {
        foreach (EquipmentSlot slot in EquipmentSlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }

        RecalculateEquipmentStats();
    }

    public ItemData GetEquippedItemForType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Helmet: return helmetSlot != null ? helmetSlot.currentItem : null;
            case ItemType.Necklace: return necklaceSlot != null ? necklaceSlot.currentItem : null;
            case ItemType.Top: return topSlot != null ? topSlot.currentItem : null;
            case ItemType.Bottom: return bottomSlot != null ? bottomSlot.currentItem : null;
            case ItemType.Boots: return bootsSlot != null ? bootsSlot.currentItem : null;
            case ItemType.Gloves: return glovesSlot != null ? glovesSlot.currentItem : null;
            case ItemType.Belt: return beltSlot != null ? beltSlot.currentItem : null;

            case ItemType.Weapon:
                if (rightWeaponSlot != null && rightWeaponSlot.currentItem != null)
                    return rightWeaponSlot.currentItem;

                if (leftWeaponSlot != null && leftWeaponSlot.currentItem != null)
                    return leftWeaponSlot.currentItem;

                return null;
        }

        return null;
    }

    public bool TryEquipFromInventory(InventorySlot inventorySlot)
    {
        if (inventorySlot == null || inventorySlot.currentItem == null)
            return false;

        ItemData itemToEquip = inventorySlot.currentItem;

        if (itemToEquip.isStackable)
            return false;

        EquipmentSlot targetSlot = GetSlotForItem(itemToEquip);

        if (targetSlot == null || !targetSlot.CanEquip(itemToEquip))
            return false;

        ItemData oldEquippedItem = targetSlot.currentItem;

        targetSlot.SetItemWithoutRecalculate(itemToEquip);

        if (oldEquippedItem != null)
            inventorySlot.SetItem(oldEquippedItem, 1);
        else
            inventorySlot.ClearSlot();

        RecalculateEquipmentStats();

        if (InventoryPersistenceManager.Instance != null)
            InventoryPersistenceManager.Instance.SaveAfterChange();

        return true;
    }

    public bool TryUnequipToInventory(EquipmentSlot equipmentSlot)
    {
        if (equipmentSlot == null || equipmentSlot.currentItem == null)
            return false;

        if (inventoryGrid == null)
            inventoryGrid = FindObjectOfType<InventoryGrid>();

        if (inventoryGrid == null)
            return false;

        ItemData itemToUnequip = equipmentSlot.currentItem;

        bool added = inventoryGrid.AddItem(itemToUnequip, 1);

        if (!added)
            return false;

        equipmentSlot.ClearSlot();

        RecalculateEquipmentStats();

        if (InventoryPersistenceManager.Instance != null)
            InventoryPersistenceManager.Instance.SaveAfterChange();

        return true;
    }

    EquipmentSlot GetSlotForItem(ItemData item)
    {
        if (item == null)
            return null;

        switch (item.itemType)
        {
            case ItemType.Helmet: return helmetSlot;
            case ItemType.Necklace: return necklaceSlot;
            case ItemType.Top: return topSlot;
            case ItemType.Bottom: return bottomSlot;
            case ItemType.Boots: return bootsSlot;
            case ItemType.Gloves: return glovesSlot;
            case ItemType.Belt: return beltSlot;

            case ItemType.Weapon:
                if (rightWeaponSlot != null && rightWeaponSlot.currentItem == null)
                    return rightWeaponSlot;

                if (leftWeaponSlot != null && leftWeaponSlot.currentItem == null)
                    return leftWeaponSlot;

                return rightWeaponSlot;
        }

        return null;
    }

    public void RecalculateEquipmentStats()
    {
        if (playerStats == null)
            return;

        playerStats.ClearGearBonuses();

        foreach (EquipmentSlot slot in EquipmentSlots)
            AddSlotBonuses(slot);

        playerStats.RecalculateStats();
    }

    void AddSlotBonuses(EquipmentSlot slot)
    {
        if (slot == null || slot.currentItem == null)
            return;

        playerStats.AddGearBonuses(slot.currentItem);
    }
}