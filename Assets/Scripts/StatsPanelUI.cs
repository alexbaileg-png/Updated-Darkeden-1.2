using UnityEngine;
using TMPro;

public class StatsPanelUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerStats playerStats;

    [Header("Main Stats Labels")]
    public TMP_Text strengthValue;
    public TMP_Text dexterityValue;
    public TMP_Text intelligenceValue;
    public TMP_Text enduranceValue;

    [Header("Derived Stats")]
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
            return;

        strengthValue.text = playerStats.strength.ToString();
        dexterityValue.text = playerStats.dexterity.ToString();
        intelligenceValue.text = playerStats.intelligence.ToString();
        enduranceValue.text = playerStats.endurance.ToString();

        armorValue.text = Mathf.RoundToInt(playerStats.armor).ToString();
        resistanceValue.text = Mathf.RoundToInt(playerStats.resistance).ToString();

        healthValue.text =
            playerStats.currentHealth + " / " + playerStats.maxHealth;

        manaValue.text =
            playerStats.currentMana + " / " + playerStats.maxMana;
    }
}