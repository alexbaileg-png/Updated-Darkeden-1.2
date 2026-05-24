using UnityEngine;

public static class ItemRarityColors
{
    public static Color GetColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;

            case ItemRarity.Uncommon:
                return Color.green;

            case ItemRarity.Rare:
                return new Color(0.2f, 0.45f, 1f);

            case ItemRarity.Epic:
                return new Color(0.7f, 0.3f, 1f);

            case ItemRarity.Legendary:
                return new Color(1f, 0.55f, 0f);
        }

        return Color.white;
    }
}