using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Player")]
    public PlayerStats playerStats;

    [Header("Equipment Slots")]
    public EquipmentSlot helmetSlot;
    public EquipmentSlot necklaceSlot;
    public EquipmentSlot topSlot;
    public EquipmentSlot bottomSlot;
    public EquipmentSlot bootsSlot;
    public EquipmentSlot leftWeaponSlot;
    public EquipmentSlot rightWeaponSlot;

    void Start()
    {
        RecalculateEquipmentStats();
    }

    public void RecalculateEquipmentStats()
    {
        if (playerStats == null)
        {
            Debug.LogError("EquipmentManager: PlayerStats is missing.");
            return;
        }

        playerStats.ClearGearBonuses();

        AddSlotBonuses(helmetSlot);
        AddSlotBonuses(necklaceSlot);
        AddSlotBonuses(topSlot);
        AddSlotBonuses(bottomSlot);
        AddSlotBonuses(bootsSlot);
        AddSlotBonuses(leftWeaponSlot);
        AddSlotBonuses(rightWeaponSlot);

        playerStats.RecalculateStats();

        Debug.Log("Equipment stats recalculated.");
    }

    void AddSlotBonuses(EquipmentSlot slot)
    {
        if (slot == null)
            return;

        if (slot.currentItem == null)
            return;

        playerStats.AddGearBonuses(slot.currentItem);
    }
}