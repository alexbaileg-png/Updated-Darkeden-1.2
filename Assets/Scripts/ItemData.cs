using UnityEngine;

public enum ItemType
{
    Helmet,
    Necklace,
    Top,
    Bottom,
    Boots,
    Gloves,
    Belt,
    Weapon,
    Trophy,
    Material,
    WeaponCrystal,
    ArmorCrystal,
    EnchantStone,
    RefiningStone,
    HealthVial,
    ManaVial
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythical
}

public enum ItemBonusStat
{
    None,
    Strength,
    Dexterity,
    Intelligence,
    Endurance,
    Health,
    Mana,
    MeleeDamage,
    RangedDamage,
    MagicDamage,
    Armor,
    Resistance,
    MeleeResistance,
    MagicResistance,
    AllResistance
}

[CreateAssetMenu(fileName = "New Item", menuName = "ARPG/Item")]
public class ItemData : ScriptableObject
{
    [Header("Save ID")]
    public string itemId;

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

    [Header("Consumable")]
    public int restoreAmount = 0;

    [Header("Economy")]
    public int sellValue = 0;

    [Header("World Loot")]
    public GameObject worldLootPrefab;

    [Header("Core Stat Bonuses")]
    public int strengthBonus;
    public int dexterityBonus;
    public int intelligenceBonus;
    public int enduranceBonus;

    [Header("Resource Bonuses")]
    public int healthBonus;
    public int manaBonus;

    [Header("Offensive Bonuses")]
    public int meleeDamageBonus;
    public int rangedDamageBonus;
    public int magicalDamageBonus;

    [Header("Defensive Bonuses")]
    public int armorBonus;
    public int resistanceBonus;
    public int meleeResistanceBonus;
    public int magicalResistanceBonus;
    public int allResistanceBonus;

    [Header("Crystal Bonus")]
    public bool hasCrystalStat = false;
    public ItemBonusStat crystalStat = ItemBonusStat.None;
    public int crystalCurrentValue = 0;
    public int crystalMaxValue = 0;
    
    [Header("Belt Storage")]
public SavedItemData[] beltSlots =
    new SavedItemData[4];
}