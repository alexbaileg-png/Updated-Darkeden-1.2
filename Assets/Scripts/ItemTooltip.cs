using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    public GameObject tooltipRoot;
    public TMP_Text tooltipText;

    void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    public void ShowTooltip(ItemData item)
    {
        if (item == null || tooltipRoot == null || tooltipText == null)
            return;

        tooltipText.text = BuildTooltipText(item);
        tooltipRoot.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }

    string BuildTooltipText(ItemData item)
    {
        string text = item.itemName + "\n";
        text += item.itemType.ToString() + "\n\n";

        if (item.strengthBonus != 0)
            text += "Strength +" + item.strengthBonus + "\n";

        if (item.dexterityBonus != 0)
            text += "Dexterity +" + item.dexterityBonus + "\n";

        if (item.intelligenceBonus != 0)
            text += "Intelligence +" + item.intelligenceBonus + "\n";

        if (item.enduranceBonus != 0)
            text += "Endurance +" + item.enduranceBonus + "\n";

        if (item.armorBonus != 0)
            text += "Armor +" + item.armorBonus + "\n";

        if (item.resistanceBonus != 0)
            text += "Resistance +" + item.resistanceBonus + "\n";

        if (item.healthBonus != 0)
            text += "Health +" + item.healthBonus + "\n";

        if (item.manaBonus != 0)
            text += "Mana +" + item.manaBonus + "\n";

        return text;
    }
}