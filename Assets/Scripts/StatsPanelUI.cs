using UnityEngine;
using TMPro;

public class StatsPanelUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerStats playerStats;

    [Header("Available Points")]
    public TMP_Text availablePointsText;

    [Header("Main Stat Values")]
    public TMP_Text strengthValue;
    public TMP_Text dexterityValue;
    public TMP_Text intelligenceValue;
    public TMP_Text enduranceValue;

    [Header("Derived Stat Values")]
    public TMP_Text armorValue;
    public TMP_Text resistanceValue;
    public TMP_Text healthValue;
    public TMP_Text manaValue;

    void OnEnable()
    {
        UpdateStatsPanel();
    }

    void Update()
    {
        UpdateStatsPanel();
    }

    public void UpdateStatsPanel()
    {
        if (playerStats == null)
            playerStats = PlayerStats.LocalInstance;

        if (playerStats == null)
            return;

        bool isVampire = playerStats.faction == PlayerFaction.Vampire;
        if (manaValue != null) manaValue.transform.parent.gameObject.SetActive(!isVampire);

        if (availablePointsText != null)
            availablePointsText.text = "Points: " + playerStats.availableStatPoints;

        if (strengthValue != null)
            strengthValue.text = playerStats.strength.ToString();

        if (dexterityValue != null)
            dexterityValue.text = playerStats.dexterity.ToString();

        if (intelligenceValue != null)
            intelligenceValue.text = playerStats.intelligence.ToString();

        if (enduranceValue != null)
            enduranceValue.text = playerStats.endurance.ToString();

        if (armorValue != null)
            armorValue.text = Mathf.RoundToInt(playerStats.armor).ToString();

        if (resistanceValue != null)
            resistanceValue.text = Mathf.RoundToInt(playerStats.resistance).ToString();

        if (healthValue != null)
            healthValue.text = playerStats.currentHealth + " / " + playerStats.maxHealth;

        if (manaValue != null)
            manaValue.text = playerStats.currentMana + " / " + playerStats.maxMana;
    }

    public void AddStrength() => playerStats?.ServerSpendStatPoint("Strength");
    public void AddDexterity() => playerStats?.ServerSpendStatPoint("Dexterity");
    public void AddIntelligence() => playerStats?.ServerSpendStatPoint("Intelligence");
    public void AddEndurance() => playerStats?.ServerSpendStatPoint("Endurance");
}