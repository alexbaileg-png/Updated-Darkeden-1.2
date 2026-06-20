using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    [Header("Behavior")]
    public bool registerAsGlobalTooltip = true;

    [Header("UI")]
    public GameObject tooltipRoot;

    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemStatsText;
    public TMP_Text itemDescriptionText;
    public TMP_Text sellValueText;

    private EquipmentManager equipmentManager;

    void Awake()
    {
        if (registerAsGlobalTooltip)
            Instance = this;

        if (tooltipRoot == null)
            tooltipRoot = gameObject;

        equipmentManager = FindObjectOfType<EquipmentManager>();

        HideTooltip();
    }

    public void ShowTooltip(ItemData item)
    {
        if (item == null)
            return;

        if (tooltipRoot == null)
            tooltipRoot = gameObject;

        if (equipmentManager == null)
            equipmentManager = FindObjectOfType<EquipmentManager>();

        tooltipRoot.SetActive(true);

        Color rarityColor = ItemRarityColors.GetColor(item.rarity);

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.color = rarityColor;
        }

        if (itemTypeText != null)
            itemTypeText.text = item.rarity + " " + item.itemType;

        if (itemDescriptionText != null)
            itemDescriptionText.text = item.description;

        if (itemStatsText != null)
            itemStatsText.text = BuildStatsText(item);

        if (sellValueText != null)
            sellValueText.text = "Sell Value: " + item.sellValue;
    }

    string BuildStatsText(ItemData item)
    {
        ItemData equippedItem = null;

        if (equipmentManager != null)
            equippedItem = equipmentManager.GetEquippedItemForType(item.itemType);

        string stats = "";

        // Weapon damage range shown at the top before stat bonuses
        if (item.weaponType != WeaponType.None && item.weaponMaxDamage > 0)
        {
            string dmgLabel = item.weaponType switch
            {
                WeaponType.Sword => "Physical Damage",
                WeaponType.Cross => "Magic Damage",
                WeaponType.Gun   => "Ranged Damage",
                _                => "Damage",
            };
            int equippedMin = (equippedItem != null && equippedItem.weaponType == item.weaponType) ? equippedItem.weaponMinDamage : 0;
            int equippedMax = (equippedItem != null && equippedItem.weaponType == item.weaponType) ? equippedItem.weaponMaxDamage : 0;
            stats += dmgLabel + ": " + item.weaponMinDamage + " - " + item.weaponMaxDamage;
            if (equippedMax > 0)
            {
                int diffMin = item.weaponMinDamage - equippedMin;
                int diffMax = item.weaponMaxDamage - equippedMax;
                if (diffMax > 0)      stats += "  <color=green>(+" + diffMax + ")</color>";
                else if (diffMax < 0) stats += "  <color=red>(" + diffMax + ")</color>";
            }
            stats += "\n";
        }

        AddComparedStat(ref stats, "Strength", item.strengthBonus, equippedItem != null ? equippedItem.strengthBonus : 0);
        AddComparedStat(ref stats, "Dexterity", item.dexterityBonus, equippedItem != null ? equippedItem.dexterityBonus : 0);
        AddComparedStat(ref stats, "Intelligence", item.intelligenceBonus, equippedItem != null ? equippedItem.intelligenceBonus : 0);
        AddComparedStat(ref stats, "Endurance", item.enduranceBonus, equippedItem != null ? equippedItem.enduranceBonus : 0);

        AddComparedStat(ref stats, "Health", item.healthBonus, equippedItem != null ? equippedItem.healthBonus : 0);
        AddComparedStat(ref stats, "Mana", item.manaBonus, equippedItem != null ? equippedItem.manaBonus : 0);

        AddComparedStat(ref stats, "Melee Damage", item.meleeDamageBonus, equippedItem != null ? equippedItem.meleeDamageBonus : 0);
        AddComparedStat(ref stats, "Ranged Damage", item.rangedDamageBonus, equippedItem != null ? equippedItem.rangedDamageBonus : 0);
        AddComparedStat(ref stats, "Magic Damage", item.magicalDamageBonus, equippedItem != null ? equippedItem.magicalDamageBonus : 0);

        AddComparedStat(ref stats, "Armor", item.armorBonus, equippedItem != null ? equippedItem.armorBonus : 0);
        AddComparedStat(ref stats, "Resistance", item.resistanceBonus, equippedItem != null ? equippedItem.resistanceBonus : 0);
        AddComparedStat(ref stats, "Melee Resistance", item.meleeResistanceBonus, equippedItem != null ? equippedItem.meleeResistanceBonus : 0);
        AddComparedStat(ref stats, "Magic Resistance", item.magicalResistanceBonus, equippedItem != null ? equippedItem.magicalResistanceBonus : 0);
        AddComparedStat(ref stats, "All Resistance", item.allResistanceBonus, equippedItem != null ? equippedItem.allResistanceBonus : 0);

        if (item.hasCrystalStat && item.crystalStat != ItemBonusStat.None)
        {
            stats += "\n<color=#66CCFF>Crystal Bonus:</color>\n";
            stats += GetStatDisplayName(item.crystalStat) + " +" + item.crystalCurrentValue + " / " + item.crystalMaxValue + "\n";
        }

        if (string.IsNullOrEmpty(stats))
            stats = "No bonuses";

        return stats;
    }

    void AddComparedStat(ref string text, string statName, int newValue, int equippedValue)
    {
        if (newValue == 0 && equippedValue == 0)
            return;

        int difference = newValue - equippedValue;

        text += statName + " +" + newValue;

        if (difference > 0)
            text += "  <color=green>(+" + difference + ")</color>";
        else if (difference < 0)
            text += "  <color=red>(" + difference + ")</color>";

        text += "\n";
    }

    string GetStatDisplayName(ItemBonusStat stat)
    {
        switch (stat)
        {
            case ItemBonusStat.Strength: return "Strength";
            case ItemBonusStat.Dexterity: return "Dexterity";
            case ItemBonusStat.Intelligence: return "Intelligence";
            case ItemBonusStat.Endurance: return "Endurance";
            case ItemBonusStat.Health: return "Health";
            case ItemBonusStat.Mana: return "Mana";
            case ItemBonusStat.MeleeDamage: return "Melee Damage";
            case ItemBonusStat.RangedDamage: return "Ranged Damage";
            case ItemBonusStat.MagicDamage: return "Magic Damage";
            case ItemBonusStat.Armor: return "Armor";
            case ItemBonusStat.Resistance: return "Resistance";
            case ItemBonusStat.MeleeResistance: return "Melee Resistance";
            case ItemBonusStat.MagicResistance: return "Magic Resistance";
            case ItemBonusStat.AllResistance: return "All Resistance";
        }

        return stat.ToString();
    }

    public void HideTooltip()
    {
        if (tooltipRoot == null)
            tooltipRoot = gameObject;

        tooltipRoot.SetActive(false);
    }
}