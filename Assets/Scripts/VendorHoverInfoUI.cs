using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VendorHoverInfoUI : MonoBehaviour
{
    public static VendorHoverInfoUI Instance;

    [Header("Root")]
    public GameObject rootPanel;

    [Header("UI")]
    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemStatsText;
    public TMP_Text descriptionText;
    public TMP_Text priceText;

    [Header("Icon")]
    public Image itemIcon;

    void Awake()
    {
        Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void ShowItem(ItemData item, int price)
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

        if (priceText != null)
            priceText.text = "Price: " + price + " Gold";

        if (itemIcon != null)
        {
            itemIcon.enabled = item.itemIcon != null;
            itemIcon.sprite = item.itemIcon;
        }

        if (itemStatsText != null)
            itemStatsText.text = BuildStats(item);
    }

    string BuildStats(ItemData item)
    {
        string stats = "";

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

        if (string.IsNullOrEmpty(stats))
            stats = "No bonuses";

        return stats;
    }

    void AddStat(ref string text, string statName, int value)
    {
        if (value == 0)
            return;

        text += statName + " +" + value + "\n";
    }

    public void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}