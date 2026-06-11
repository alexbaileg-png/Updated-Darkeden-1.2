using System.Collections.Generic;
using UnityEngine;

public class ItemEnhancementManager : MonoBehaviour
{
    public static ItemEnhancementManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public bool UseEnhancementItem(ItemData gearItem, ItemData enhancementItem)
    {
        if (gearItem == null || enhancementItem == null)
            return false;

        switch (enhancementItem.itemType)
        {
            case ItemType.WeaponCrystal:
            case ItemType.ArmorCrystal:
                return ApplyCrystal(gearItem, enhancementItem);

            case ItemType.EnchantStone:
                return ApplyEnchantStone(gearItem);

            case ItemType.RefiningStone:
                return ApplyRefiningStone(gearItem);
        }

        return false;
    }

    public bool ApplyCrystal(ItemData gearItem, ItemData crystalItem)
    {
        if (gearItem == null || crystalItem == null)
            return false;

        bool isWeaponCrystal = crystalItem.itemType == ItemType.WeaponCrystal;
        bool isArmorCrystal = crystalItem.itemType == ItemType.ArmorCrystal;

        bool validWeapon = gearItem.itemType == ItemType.Weapon;

        bool validArmor =
            gearItem.itemType == ItemType.Helmet ||
            gearItem.itemType == ItemType.Top ||
            gearItem.itemType == ItemType.Bottom ||
            gearItem.itemType == ItemType.Boots ||
            gearItem.itemType == ItemType.Gloves ||
            gearItem.itemType == ItemType.Belt ||
            gearItem.itemType == ItemType.Necklace;

        if (isWeaponCrystal && !validWeapon)
        {
            Debug.Log("Weapon crystals can only be used on weapons.");
            return false;
        }

        if (isArmorCrystal && !validArmor)
        {
            Debug.Log("Armor crystals can only be used on armor.");
            return false;
        }

        ItemBonusStat randomStat = GetRandomCrystalStat(isWeaponCrystal);

        gearItem.hasCrystalStat = true;
        gearItem.crystalStat = randomStat;

        int maxRoll = GetCrystalMaxRoll(gearItem.rarity);
        gearItem.crystalMaxValue = maxRoll;

        gearItem.crystalCurrentValue = Random.Range(
            Mathf.CeilToInt(maxRoll * 0.3f),
            Mathf.CeilToInt(maxRoll * 0.5f) + 1
        );

        Debug.Log("Applied crystal stat: " + gearItem.crystalStat + " +" + gearItem.crystalCurrentValue + "/" + gearItem.crystalMaxValue);

        return true;
    }

    public bool ApplyEnchantStone(ItemData gearItem)
    {
        if (gearItem == null)
            return false;

        if (!gearItem.hasCrystalStat || gearItem.crystalStat == ItemBonusStat.None)
        {
            Debug.Log("Enchant Stone failed: item has no crystal stat.");
            return false;
        }

        if (gearItem.crystalCurrentValue >= gearItem.crystalMaxValue)
        {
            Debug.Log("Enchant Stone failed: crystal stat is already maxed.");
            return false;
        }

        gearItem.crystalCurrentValue += 1;

        if (gearItem.crystalCurrentValue > gearItem.crystalMaxValue)
            gearItem.crystalCurrentValue = gearItem.crystalMaxValue;

        Debug.Log("Crystal stat increased to +" + gearItem.crystalCurrentValue + "/" + gearItem.crystalMaxValue);

        return true;
    }

    public bool ApplyRefiningStone(ItemData gearItem)
    {
        if (gearItem == null)
            return false;

        if (!IsGearItem(gearItem))
        {
            Debug.Log("Refining Stone failed: target is not gear.");
            return false;
        }

        List<ItemBonusStat> availableStats = GetExistingBaseStats(gearItem);

        if (availableStats.Count <= 0)
        {
            Debug.Log("Refining Stone failed: item has no base stats to improve.");
            return false;
        }

        ItemBonusStat statToImprove = availableStats[Random.Range(0, availableStats.Count)];

        IncreaseBaseStat(gearItem, statToImprove, 1);

        Debug.Log("Refined " + gearItem.itemName + ": " + statToImprove + " +1");

        return true;
    }

    bool IsGearItem(ItemData item)
    {
        return
            item.itemType == ItemType.Helmet ||
            item.itemType == ItemType.Necklace ||
            item.itemType == ItemType.Top ||
            item.itemType == ItemType.Bottom ||
            item.itemType == ItemType.Boots ||
            item.itemType == ItemType.Gloves ||
            item.itemType == ItemType.Belt ||
            item.itemType == ItemType.Weapon;
    }

    List<ItemBonusStat> GetExistingBaseStats(ItemData item)
    {
        List<ItemBonusStat> stats = new List<ItemBonusStat>();

        if (item.strengthBonus > 0) stats.Add(ItemBonusStat.Strength);
        if (item.dexterityBonus > 0) stats.Add(ItemBonusStat.Dexterity);
        if (item.intelligenceBonus > 0) stats.Add(ItemBonusStat.Intelligence);
        if (item.enduranceBonus > 0) stats.Add(ItemBonusStat.Endurance);

        if (item.healthBonus > 0) stats.Add(ItemBonusStat.Health);
        if (item.manaBonus > 0) stats.Add(ItemBonusStat.Mana);

        if (item.meleeDamageBonus > 0) stats.Add(ItemBonusStat.MeleeDamage);
        if (item.rangedDamageBonus > 0) stats.Add(ItemBonusStat.RangedDamage);
        if (item.magicalDamageBonus > 0) stats.Add(ItemBonusStat.MagicDamage);

        if (item.armorBonus > 0) stats.Add(ItemBonusStat.Armor);
        if (item.resistanceBonus > 0) stats.Add(ItemBonusStat.Resistance);
        if (item.meleeResistanceBonus > 0) stats.Add(ItemBonusStat.MeleeResistance);
        if (item.magicalResistanceBonus > 0) stats.Add(ItemBonusStat.MagicResistance);
        if (item.allResistanceBonus > 0) stats.Add(ItemBonusStat.AllResistance);

        return stats;
    }

    void IncreaseBaseStat(ItemData item, ItemBonusStat stat, int amount)
    {
        switch (stat)
        {
            case ItemBonusStat.Strength: item.strengthBonus += amount; break;
            case ItemBonusStat.Dexterity: item.dexterityBonus += amount; break;
            case ItemBonusStat.Intelligence: item.intelligenceBonus += amount; break;
            case ItemBonusStat.Endurance: item.enduranceBonus += amount; break;

            case ItemBonusStat.Health: item.healthBonus += amount; break;
            case ItemBonusStat.Mana: item.manaBonus += amount; break;

            case ItemBonusStat.MeleeDamage: item.meleeDamageBonus += amount; break;
            case ItemBonusStat.RangedDamage: item.rangedDamageBonus += amount; break;
            case ItemBonusStat.MagicDamage: item.magicalDamageBonus += amount; break;

            case ItemBonusStat.Armor: item.armorBonus += amount; break;
            case ItemBonusStat.Resistance: item.resistanceBonus += amount; break;
            case ItemBonusStat.MeleeResistance: item.meleeResistanceBonus += amount; break;
            case ItemBonusStat.MagicResistance: item.magicalResistanceBonus += amount; break;
            case ItemBonusStat.AllResistance: item.allResistanceBonus += amount; break;
        }
    }

    ItemBonusStat GetRandomCrystalStat(bool weaponCrystal)
    {
        if (weaponCrystal)
        {
            ItemBonusStat[] stats =
            {
                ItemBonusStat.Strength,
                ItemBonusStat.Dexterity,
                ItemBonusStat.Intelligence,
                ItemBonusStat.MeleeDamage,
                ItemBonusStat.RangedDamage,
                ItemBonusStat.MagicDamage
            };

            return stats[Random.Range(0, stats.Length)];
        }
        else
        {
            ItemBonusStat[] stats =
            {
                ItemBonusStat.Strength,
                ItemBonusStat.Dexterity,
                ItemBonusStat.Intelligence,
                ItemBonusStat.Endurance,
                ItemBonusStat.Health,
                ItemBonusStat.Mana,
                ItemBonusStat.Armor,
                ItemBonusStat.Resistance,
                ItemBonusStat.AllResistance
            };

            return stats[Random.Range(0, stats.Length)];
        }
    }

    int GetCrystalMaxRoll(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 5;
            case ItemRarity.Uncommon: return 8;
            case ItemRarity.Rare: return 12;
            case ItemRarity.Epic: return 16;
            case ItemRarity.Legendary: return 20;
            case ItemRarity.Mythical: return 30;
        }

        return 5;
    }
}