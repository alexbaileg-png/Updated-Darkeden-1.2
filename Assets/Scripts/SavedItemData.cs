using System;
using System.Collections.Generic;

[Serializable]
public class SavedItemData
{
    public string uniqueItemId;
    public string baseItemId;

    public int slotIndex = -1;
    public string equipmentSlotId = "";

    public int quantity;

    public ItemRarity rarity;

    public int strengthBonus;
    public int dexterityBonus;
    public int intelligenceBonus;
    public int enduranceBonus;

    public int healthBonus;
    public int manaBonus;

    public int meleeDamageBonus;
    public int rangedDamageBonus;
    public int magicalDamageBonus;

    public int armorBonus;
    public int resistanceBonus;
    public int meleeResistanceBonus;
    public int magicalResistanceBonus;
    public int allResistanceBonus;

    public bool hasCrystalStat;
    public ItemBonusStat crystalStat;
    public int crystalCurrentValue;
    public int crystalMaxValue;

    // BELT STORAGE
    public List<SavedItemData> beltItems =
        new List<SavedItemData>();
}