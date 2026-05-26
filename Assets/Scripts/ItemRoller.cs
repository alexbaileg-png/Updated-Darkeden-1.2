using UnityEngine;

public static class ItemRoller
{
    public static ItemData RollItem(ItemData baseItem)
    {
        if (baseItem == null)
            return null;

        // Do NOT randomize stackable/trophy/material items
        if (baseItem.isStackable ||
            baseItem.itemType == ItemType.Trophy ||
            baseItem.itemType == ItemType.Material)
        {
            return baseItem;
        }

        ItemData rolledItem = Object.Instantiate(baseItem);

        rolledItem.rarity = RollRarity();
        ApplyRandomStats(rolledItem);
        ApplyRarityName(rolledItem);
        ApplySellValueBonus(rolledItem);

        return rolledItem;
    }

    static ItemRarity RollRarity()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 55f)
            return ItemRarity.Common;

        if (roll < 80f)
            return ItemRarity.Uncommon;

        if (roll < 94f)
            return ItemRarity.Rare;

        if (roll < 99f)
            return ItemRarity.Epic;

        return ItemRarity.Legendary;
    }

    static void ApplyRandomStats(ItemData item)
    {
        int rolls = GetStatRollCount(item.rarity);
        int power = GetStatPower(item.rarity);

        for (int i = 0; i < rolls; i++)
        {
            int stat = Random.Range(0, 8);
            int value = Random.Range(1, power + 1);

            switch (stat)
            {
                case 0:
                    item.strengthBonus += value;
                    break;
                case 1:
                    item.dexterityBonus += value;
                    break;
                case 2:
                    item.intelligenceBonus += value;
                    break;
                case 3:
                    item.enduranceBonus += value;
                    break;
                case 4:
                    item.armorBonus += value * 2;
                    break;
                case 5:
                    item.resistanceBonus += value * 2;
                    break;
                case 6:
                    item.healthBonus += value * 8;
                    break;
                case 7:
                    item.manaBonus += value * 6;
                    break;
            }
        }
    }

    static int GetStatRollCount(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return 0;

            case ItemRarity.Uncommon:
                return 1;

            case ItemRarity.Rare:
                return 2;

            case ItemRarity.Epic:
                return 3;

            case ItemRarity.Legendary:
                return 4;
        }

        return 0;
    }

    static int GetStatPower(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return 1;

            case ItemRarity.Uncommon:
                return 2;

            case ItemRarity.Rare:
                return 4;

            case ItemRarity.Epic:
                return 7;

            case ItemRarity.Legendary:
                return 10;
        }

        return 1;
    }

    static void ApplyRarityName(ItemData item)
    {
        if (item.rarity == ItemRarity.Common)
            return;

        item.itemName = item.rarity + " " + item.itemName;
    }

    static void ApplySellValueBonus(ItemData item)
    {
        float multiplier = 1f;

        switch (item.rarity)
        {
            case ItemRarity.Common:
                multiplier = 1f;
                break;
            case ItemRarity.Uncommon:
                multiplier = 1.5f;
                break;
            case ItemRarity.Rare:
                multiplier = 2.5f;
                break;
            case ItemRarity.Epic:
                multiplier = 5f;
                break;
            case ItemRarity.Legendary:
                multiplier = 10f;
                break;
        }

        item.sellValue = Mathf.RoundToInt(item.sellValue * multiplier);
    }
}