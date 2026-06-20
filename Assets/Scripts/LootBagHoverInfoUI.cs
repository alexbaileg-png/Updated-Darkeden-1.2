using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LootBagHoverInfoUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;

    [Header("UI")]
    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemStatsText;
    public TMP_Text descriptionText;
    public TMP_Text valueText;
    public Image itemIcon;

    void Awake()
    {
        Hide();
    }

    public void ShowItem(ItemData item)
    {
        if (item == null)
            return;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        Color rarityColor = ItemRarityColors.GetColor(item.rarity);

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.color = rarityColor;
        }

        if (itemTypeText != null)
            itemTypeText.text = item.rarity + " " + item.itemType;

        if (descriptionText != null)
            descriptionText.text = item.description;

        if (valueText != null)
            valueText.text = "Value: " + item.sellValue + " Gold";

        if (itemIcon != null)
        {
            itemIcon.enabled = item.itemIcon != null;
            itemIcon.sprite = item.itemIcon;
        }

        if (itemStatsText != null)
            itemStatsText.text = BuildStats(item);
    }

    public void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    string BuildStats(ItemData item)
    {
        string stats = "";

        if (item.weaponType != WeaponType.None && item.weaponMaxDamage > 0)
        {
            string label = item.weaponType switch
            {
                WeaponType.Sword => "Physical Damage",
                WeaponType.Cross => "Magic Damage",
                WeaponType.Gun   => "Ranged Damage",
                _                => "Damage",
            };
            stats += label + ": " + item.weaponMinDamage + " - " + item.weaponMaxDamage + "\n";
        }

        AddStat(ref stats, "Strength", item.strengthBonus);
        AddStat(ref stats, "Dexterity", item.dexterityBonus);
        AddStat(ref stats, "Intelligence", item.intelligenceBonus);
        AddStat(ref stats, "Endurance", item.enduranceBonus);
        AddStat(ref stats, "Health", item.healthBonus);
        AddStat(ref stats, "Mana", item.manaBonus);
        AddStat(ref stats, "Melee Damage", item.meleeDamageBonus);
        AddStat(ref stats, "Ranged Damage", item.rangedDamageBonus);
        AddStat(ref stats, "Magic Damage", item.magicalDamageBonus);
        AddStat(ref stats, "Armor", item.armorBonus);
        AddStat(ref stats, "Resistance", item.resistanceBonus);
        AddStat(ref stats, "Melee Resistance", item.meleeResistanceBonus);
        AddStat(ref stats, "Magic Resistance", item.magicalResistanceBonus);
        AddStat(ref stats, "All Resistance", item.allResistanceBonus);

        return string.IsNullOrEmpty(stats) ? "No bonuses" : stats;
    }

    void AddStat(ref string text, string name, int value)
    {
        if (value != 0)
            text += name + " +" + value + "\n";
    }
}