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

        if (itemNameText != null)
            itemNameText.text = item.itemName;

        if (itemTypeText != null)
            itemTypeText.text = item.itemType.ToString();

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

    public void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    string BuildStats(ItemData item)
    {
        string stats = "";

        AddStat(ref stats, "Strength", item.strengthBonus);
        AddStat(ref stats, "Dexterity", item.dexterityBonus);
        AddStat(ref stats, "Intelligence", item.intelligenceBonus);
        AddStat(ref stats, "Endurance", item.enduranceBonus);

        AddStat(ref stats, "Armor", item.armorBonus);
        AddStat(ref stats, "Resistance", item.resistanceBonus);

        AddStat(ref stats, "Health", item.healthBonus);
        AddStat(ref stats, "Mana", item.manaBonus);

        if (string.IsNullOrEmpty(stats))
            stats = "No bonuses";

        return stats;
    }

    void AddStat(ref string text, string statName, int value)
    {
        if (value == 0)
            return;

        text += statName + ": +" + value + "\n";
    }
}