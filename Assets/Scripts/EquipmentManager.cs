using System.Collections.Generic;
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
    // Slayer only
    public EquipmentSlot glovesSlot;
    public EquipmentSlot beltSlot;
    public EquipmentSlot leftWeaponSlot;
    public EquipmentSlot rightWeaponSlot;

    [Header("Attachment Points (drag bones from the character rig)")]
    public Transform helmetAttach;
    public Transform necklaceAttach;
    public Transform topAttach;
    public Transform bottomAttach;
    public Transform bootsAttach;
    public Transform glovesAttach;
    public Transform beltAttach;
    public Transform leftWeaponAttach;
    public Transform rightWeaponAttach;

    // Tracks the live visual GameObject for each slot so it can be destroyed on unequip.
    readonly Dictionary<EquipmentSlot, GameObject> _spawnedVisuals = new Dictionary<EquipmentSlot, GameObject>();

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

    Transform GetAttachPoint(EquipmentSlot slot)
    {
        if (slot == helmetSlot)      return helmetAttach;
        if (slot == necklaceSlot)    return necklaceAttach;
        if (slot == topSlot)         return topAttach;
        if (slot == bottomSlot)      return bottomAttach;
        if (slot == bootsSlot)       return bootsAttach;
        if (slot == glovesSlot)      return glovesAttach;
        if (slot == beltSlot)        return beltAttach;
        if (slot == leftWeaponSlot)  return leftWeaponAttach;
        if (slot == rightWeaponSlot) return rightWeaponAttach;
        return null;
    }

    void SpawnVisual(EquipmentSlot slot, ItemData item)
    {
        DestroyVisual(slot);
        if (item == null || item.equippedVisualPrefab == null) return;
        Transform attach = GetAttachPoint(slot);
        if (attach == null) return;

        GameObject visual = Instantiate(item.equippedVisualPrefab, attach);

        WeaponFollowBone follow = visual.AddComponent<WeaponFollowBone>();
        follow.bone           = attach;
        follow.positionOffset = item.equippedVisualPrefab.transform.localPosition;
        follow.rotationOffset = item.equippedVisualPrefab.transform.localEulerAngles;

        _spawnedVisuals[slot] = visual;
    }

    void DestroyVisual(EquipmentSlot slot)
    {
        if (_spawnedVisuals.TryGetValue(slot, out GameObject existing) && existing != null)
            Destroy(existing);
        _spawnedVisuals.Remove(slot);
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
            case ItemType.Gloves:    return glovesSlot    != null ? glovesSlot.currentItem    : null;
            case ItemType.Belt:      return beltSlot      != null ? beltSlot.currentItem      : null;


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
        SpawnVisual(targetSlot, itemToEquip);

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
        DestroyVisual(equipmentSlot);

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
            case ItemType.Gloves:   return glovesSlot;
            case ItemType.Belt:     return beltSlot;


            case ItemType.Weapon:
                // Only swords can dual-wield — try to fill the empty hand first
                if (item.weaponType == WeaponType.Sword)
                {
                    if (rightWeaponSlot != null && rightWeaponSlot.currentItem == null)
                        return rightWeaponSlot;
                    if (leftWeaponSlot  != null && leftWeaponSlot.currentItem  == null)
                        return leftWeaponSlot;
                    // Both occupied — replace right hand
                    return rightWeaponSlot;
                }

                // All other weapon types: right hand only, always
                // Also unequip the left hand if it has something (only swords go there)
                if (leftWeaponSlot != null && leftWeaponSlot.currentItem != null)
                {
                    // Move the left-hand item back to inventory before equipping
                    TryUnequipToInventory(leftWeaponSlot);
                }
                return rightWeaponSlot;
        }

        return null;
    }

    public GameObject GetSpawnedVisual(EquipmentSlot slot)
    {
        _spawnedVisuals.TryGetValue(slot, out GameObject go);
        return go;
    }

    public void SpawnAllVisuals()
    {
        foreach (EquipmentSlot slot in EquipmentSlots)
        {
            if (slot != null && slot.currentItem != null)
                SpawnVisual(slot, slot.currentItem);
        }
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