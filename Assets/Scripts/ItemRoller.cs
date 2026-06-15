using UnityEngine;

public static class ItemRoller
{
    public static ItemData RollItem(ItemData baseItem)
    {
        if (baseItem == null)
            return null;

        if (baseItem.isStackable ||
            baseItem.itemType == ItemType.Trophy ||
            baseItem.itemType == ItemType.Material)
        {
            return baseItem;
        }

        ItemData rolledItem = Object.Instantiate(baseItem);
        rolledItem.runtimeId = System.Guid.NewGuid().ToString();

        rolledItem.rarity = RollRarity();

        ClearRolledStats(rolledItem);
        ApplyRandomStats(rolledItem);
        ApplyGeneratedName(rolledItem);
        ApplySellValueBonus(rolledItem);

        return rolledItem;
    }

    static ItemRarity RollRarity()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 50f)
            return ItemRarity.Common;

        if (roll < 75f)
            return ItemRarity.Uncommon;

        if (roll < 90f)
            return ItemRarity.Rare;

        if (roll < 97f)
            return ItemRarity.Epic;

        if (roll < 99.5f)
            return ItemRarity.Legendary;

        return ItemRarity.Mythical;
    }

    static void ClearRolledStats(ItemData item)
    {
        item.strengthBonus = 0;
        item.dexterityBonus = 0;
        item.intelligenceBonus = 0;
        item.enduranceBonus = 0;

        item.healthBonus = 0;
        item.manaBonus = 0;

        item.meleeDamageBonus = 0;
        item.rangedDamageBonus = 0;
        item.magicalDamageBonus = 0;

        item.armorBonus = 0;
        item.resistanceBonus = 0;
        item.meleeResistanceBonus = 0;
        item.magicalResistanceBonus = 0;
        item.allResistanceBonus = 0;
    }

    static void ApplyRandomStats(ItemData item)
    {
        int rolls = GetStatRollCount(item.rarity);
        int power = GetStatPower(item.rarity);

        for (int i = 0; i < rolls; i++)
        {
            int stat = Random.Range(0, 14);
            int value = Random.Range(1, power + 1);

            switch (stat)
            {
                case 0: item.strengthBonus += value; break;
                case 1: item.dexterityBonus += value; break;
                case 2: item.intelligenceBonus += value; break;
                case 3: item.enduranceBonus += value; break;

                case 4: item.healthBonus += value * 8; break;
                case 5: item.manaBonus += value * 6; break;

                case 6: item.meleeDamageBonus += value * 2; break;
                case 7: item.rangedDamageBonus += value * 2; break;
                case 8: item.magicalDamageBonus += value * 2; break;

                case 9: item.armorBonus += value * 2; break;
                case 10: item.resistanceBonus += value * 2; break;
                case 11: item.meleeResistanceBonus += value * 2; break;
                case 12: item.magicalResistanceBonus += value * 2; break;
                case 13: item.allResistanceBonus += value; break;
            }
        }
    }

    static int GetStatRollCount(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 0;
            case ItemRarity.Uncommon: return 1;
            case ItemRarity.Rare: return 2;
            case ItemRarity.Epic: return 3;
            case ItemRarity.Legendary: return 4;
            case ItemRarity.Mythical: return 5;
        }

        return 0;
    }

    static int GetStatPower(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 1;
            case ItemRarity.Uncommon: return 2;
            case ItemRarity.Rare: return 4;
            case ItemRarity.Epic: return 7;
            case ItemRarity.Legendary: return 10;
            case ItemRarity.Mythical: return 14;
        }

        return 1;
    }

    static void ApplyGeneratedName(ItemData item)
    {
        string title = GetTitleFromHighestStat(item);

        if (string.IsNullOrEmpty(title))
            item.itemName = item.itemName;
        else
            item.itemName = title + " " + item.itemName;
    }

    static string GetTitleFromHighestStat(ItemData item)
    {
        string bestStat = "";
        int bestValue = 0;

        CheckStat("Strength", item.strengthBonus, ref bestStat, ref bestValue);
        CheckStat("Dexterity", item.dexterityBonus, ref bestStat, ref bestValue);
        CheckStat("Intelligence", item.intelligenceBonus, ref bestStat, ref bestValue);
        CheckStat("Endurance", item.enduranceBonus, ref bestStat, ref bestValue);

        CheckStat("MeleeDamage", item.meleeDamageBonus, ref bestStat, ref bestValue);
        CheckStat("RangedDamage", item.rangedDamageBonus, ref bestStat, ref bestValue);
        CheckStat("MagicDamage", item.magicalDamageBonus, ref bestStat, ref bestValue);

        CheckStat("Armor", item.armorBonus, ref bestStat, ref bestValue);
        CheckStat("Resistance", item.resistanceBonus, ref bestStat, ref bestValue);
        CheckStat("MeleeResistance", item.meleeResistanceBonus, ref bestStat, ref bestValue);
        CheckStat("MagicResistance", item.magicalResistanceBonus, ref bestStat, ref bestValue);
        CheckStat("AllResistance", item.allResistanceBonus, ref bestStat, ref bestValue);

        return GetTitle(bestStat, bestValue);
    }

    static void CheckStat(string statName, int value, ref string bestStat, ref int bestValue)
    {
        if (value > bestValue)
        {
            bestValue = value;
            bestStat = statName;
        }
    }

    static string GetTitle(string statName, int value)
    {
        if (value <= 0)
            return "";

        if (statName == "Intelligence")
        {
            if (value <= 3) return "Owl";
            if (value <= 7) return "Sage";
            if (value <= 12) return "Archmage";
            return "Oracle";
        }

        if (statName == "Strength")
        {
            if (value <= 3) return "Bear";
            if (value <= 7) return "Titan";
            if (value <= 12) return "Colossus";
            return "Goliath";
        }

        if (statName == "Dexterity")
        {
            if (value <= 3) return "Fox";
            if (value <= 7) return "Hawk";
            if (value <= 12) return "Phantom";
            return "Shadow";
        }

        if (statName == "Endurance")
        {
            if (value <= 3) return "Stout";
            if (value <= 7) return "Iron";
            if (value <= 12) return "Mountain";
            return "Immortal";
        }

        if (statName == "MeleeDamage")
        {
            if (value <= 5) return "Sharp";
            if (value <= 12) return "Slayer";
            if (value <= 20) return "Executioner";
            return "Bloodletter";
        }

        if (statName == "RangedDamage")
        {
            if (value <= 5) return "Piercing";
            if (value <= 12) return "Hunter";
            if (value <= 20) return "Deadeye";
            return "Eagle";
        }

        if (statName == "MagicDamage")
        {
            if (value <= 5) return "Mystic";
            if (value <= 12) return "Arcane";
            if (value <= 20) return "Astral";
            return "Celestial";
        }

        if (statName == "Armor")
        {
            if (value <= 5) return "Guarded";
            if (value <= 12) return "Plated";
            if (value <= 20) return "Fortress";
            return "Adamant";
        }

        if (statName == "Resistance" || statName == "MagicResistance")
        {
            if (value <= 5) return "Warded";
            if (value <= 12) return "Runed";
            if (value <= 20) return "Sanctified";
            return "Aegis";
        }

        if (statName == "MeleeResistance")
        {
            if (value <= 5) return "Braced";
            if (value <= 12) return "Hardened";
            if (value <= 20) return "Bulwark";
            return "Unbroken";
        }

        if (statName == "AllResistance")
        {
            if (value <= 3) return "Guardian";
            if (value <= 7) return "Warden";
            if (value <= 12) return "Protector";
            return "Mythguard";
        }

        return "";
    }

    static void ApplySellValueBonus(ItemData item)
    {
        float multiplier = 1f;

        switch (item.rarity)
        {
            case ItemRarity.Common: multiplier = 1f; break;
            case ItemRarity.Uncommon: multiplier = 1.5f; break;
            case ItemRarity.Rare: multiplier = 2.5f; break;
            case ItemRarity.Epic: multiplier = 5f; break;
            case ItemRarity.Legendary: multiplier = 10f; break;
            case ItemRarity.Mythical: multiplier = 20f; break;
        }

        item.sellValue = Mathf.RoundToInt(item.sellValue * multiplier);
    }
}