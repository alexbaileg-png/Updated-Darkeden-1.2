using UnityEngine;

public enum ItemType
{
    Helmet,
    Necklace,
    Top,
    Bottom,
    Boots,
    Weapon,
    Trophy,
    Material
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "New Item", menuName = "ARPG/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;

    [TextArea]
    public string description;

    public Sprite itemIcon;

    public ItemType itemType;

    public ItemRarity rarity = ItemRarity.Common;

    [Header("Stacking")]
    public bool isStackable = false;
    public int maxStackSize = 99;

    [Header("Economy")]
    public int sellValue = 0;

    [Header("World Loot")]
    public GameObject worldLootPrefab;

    [Header("Stat Bonuses")]
    public int strengthBonus;
    public int dexterityBonus;
    public int intelligenceBonus;
    public int enduranceBonus;

    public int armorBonus;
    public int resistanceBonus;

    public int healthBonus;
    public int manaBonus;
}