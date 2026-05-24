using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    [Header("UI")]
    public GameObject tooltipRoot;

    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemStatsText;
    public TMP_Text sellValueText;

    void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    public void ShowTooltip(ItemData item)
    {
        if (item == null)
            return;

        tooltipRoot.SetActive(true);

        Color rarityColor = ItemRarityColors.GetColor(item.rarity);

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.color = rarityColor;
        }

        if (itemTypeText != null)
        {
            itemTypeText.text = item.itemType.ToString();
        }

        string stats = "";

        if (item.strengthBonus != 0)
            stats += "Strength +" + item.strengthBonus + "\n";

        if (item.dexterityBonus != 0)
            stats += "Dexterity +" + item.dexterityBonus + "\n";

        if (item.intelligenceBonus != 0)
            stats += "Intelligence +" + item.intelligenceBonus + "\n";

        if (item.enduranceBonus != 0)
            stats += "Endurance +" + item.enduranceBonus + "\n";

        if (item.armorBonus != 0)
            stats += "Armor +" + item.armorBonus + "\n";

        if (item.resistanceBonus != 0)
            stats += "Resistance +" + item.resistanceBonus + "\n";

        if (item.healthBonus != 0)
            stats += "Health +" + item.healthBonus + "\n";

        if (item.manaBonus != 0)
            stats += "Mana +" + item.manaBonus + "\n";

        if (itemStatsText != null)
            itemStatsText.text = stats;

        if (sellValueText != null)
            sellValueText.text = "Sell Value: " + item.sellValue;
    }

    public void HideTooltip()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }
}