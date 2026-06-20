using UnityEngine;

public enum FactionRestriction { Any, SlayerOnly, VampireOnly }
public enum WeaponType { None, Sword, Cross, Gun }

public enum ItemType
{
    // ── Shared slots ──────────────────────────────────────────────────────────
    Helmet,
    Necklace,
    Top,
    Bottom,
    Boots,
    Weapon,
    Trophy,
    Material,
    WeaponCrystal,
    ArmorCrystal,
    EnchantStone,
    RefiningStone,
    HealthVial,
    ManaVial,

    // ── Slayer only ───────────────────────────────────────────────────────────
    Gloves,
    Belt,

    // ── Vampire only ──────────────────────────────────────────────────────────
    Armguard,
    Sash,
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
    AllResistance,
    LifeSteal
}

[CreateAssetMenu(fileName = "New Item", menuName = "ARPG/Item")]
public class ItemData : ScriptableObject
{
    [Header("Save ID")]
    public string itemId;

    // Unique key assigned at runtime to each rolled instance — not saved to disk
    [System.NonSerialized] public string runtimeId;

    [Header("Faction")]
    public FactionRestriction factionRestriction = FactionRestriction.Any;

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

    [Header("Equipped Visual")]
    public GameObject equippedVisualPrefab; // spawned on the player's rig when this item is equipped

    [Header("Core Stat Bonuses")]
    public int strengthBonus;
    public int dexterityBonus;
    public int intelligenceBonus;
    public int enduranceBonus;

    [Header("Resource Bonuses")]
    public int healthBonus;
    public int manaBonus;

    [Header("Weapon")]
    public WeaponType weaponType = WeaponType.None;
    public int weaponMinDamage = 0;
    public int weaponMaxDamage = 0;

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

    [Header("Life Steal")]
    public float lifeStealBonus; // percentage of damage dealt returned as healing (e.g. 5 = 5%)

    [Header("Crystal Bonus")]
    public bool hasCrystalStat = false;
    public ItemBonusStat crystalStat = ItemBonusStat.None;
    public int crystalCurrentValue = 0;
    public int crystalMaxValue = 0;
    
    [Header("Belt Storage")]
public SavedItemData[] beltSlots =
    new SavedItemData[4];
}