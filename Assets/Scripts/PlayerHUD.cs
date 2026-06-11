using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Player")]
    public PlayerStats playerStats;

    [Header("Health UI")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("Mana UI")]
    public Slider manaSlider;
    public TMP_Text manaText;

    void Update()
    {
        if (playerStats == null)
            playerStats = PlayerStats.LocalInstance;

        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (playerStats == null)
            return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = playerStats.maxHealth;
            healthSlider.value = playerStats.currentHealth;
        }

        if (healthText != null)
            healthText.text = playerStats.currentHealth + " / " + playerStats.maxHealth;

        // Vampires don't use mana — hide the mana bar entirely
        bool isVampire = playerStats.faction == PlayerFaction.Vampire;

        if (manaSlider != null)
        {
            manaSlider.gameObject.SetActive(!isVampire);
            if (!isVampire)
            {
                manaSlider.maxValue = playerStats.maxMana;
                manaSlider.value = playerStats.currentMana;
            }
        }

        if (manaText != null)
        {
            manaText.gameObject.SetActive(!isVampire);
            if (!isVampire)
                manaText.text = playerStats.currentMana + " / " + playerStats.maxMana;
        }
    }

}