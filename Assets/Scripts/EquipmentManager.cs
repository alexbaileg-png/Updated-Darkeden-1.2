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
    public EquipmentSlot leftWeaponSlot;
    public EquipmentSlot rightWeaponSlot;

    void Start()
    {
        RecalculateEquipmentStats();
    }

    public bool TryEquipFromInventory(InventorySlot inventorySlot)
    {
        if (inventorySlot == null || inventorySlot.currentItem == null)
            return false;

        ItemData itemToEquip = inventorySlot.currentItem;

        if (itemToEquip.isStackable)
            return false;

        EquipmentSlot targetSlot = GetSlotForItem(itemToEquip);

        if (targetSlot == null)
            return false;

        if (!targetSlot.CanEquip(itemToEquip))
            return false;

        ItemData oldEquippedItem = targetSlot.currentItem;

        targetSlot.SetItemWithoutRecalculate(itemToEquip);

        if (oldEquippedItem != null)
            inventorySlot.SetItem(oldEquippedItem, 1);
        else
            inventorySlot.ClearSlot();

        RecalculateEquipmentStats();

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

        return true;
    }

    EquipmentSlot GetSlotForItem(ItemData item)
    {
        if (item == null)
            return null;

        switch (item.itemType)
        {
            case ItemType.Helmet:
                return helmetSlot;

            case ItemType.Necklace:
                return necklaceSlot;

            case ItemType.Top:
                return topSlot;

            case ItemType.Bottom:
                return bottomSlot;

            case ItemType.Boots:
                return bootsSlot;

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

        AddSlotBonuses(helmetSlot);
        AddSlotBonuses(necklaceSlot);
        AddSlotBonuses(topSlot);
        AddSlotBonuses(bottomSlot);
        AddSlotBonuses(bootsSlot);
        AddSlotBonuses(leftWeaponSlot);
        AddSlotBonuses(rightWeaponSlot);

        playerStats.RecalculateStats();
    }

    void AddSlotBonuses(EquipmentSlot slot)
    {
        if (slot == null || slot.currentItem == null)
            return;

        playerStats.AddGearBonuses(slot.currentItem);
    }
}