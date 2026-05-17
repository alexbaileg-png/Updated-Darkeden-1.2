using UnityEngine;

public enum ItemType
{
    Helmet,
    Necklace,
    Top,
    Bottom,
    Boots,
    Weapon
}

[CreateAssetMenu(fileName = "New Item", menuName = "ARPG/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;

    public ItemType itemType;

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