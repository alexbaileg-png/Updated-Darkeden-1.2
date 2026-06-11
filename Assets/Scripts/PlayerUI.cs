using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerStats playerStats;

    [Header("XP UI")]
    public Slider xpBar;

    public TMP_Text levelText;
    public TMP_Text xpText;

    void Update()
    {
        if (playerStats == null)
            playerStats = PlayerStats.LocalInstance;

        if (playerStats == null)
            return;

        UpdateXPUI();
    }

    void UpdateXPUI()
    {
        if (xpBar != null)
        {
            xpBar.maxValue = playerStats.xpToNextLevel;
            xpBar.value = playerStats.currentXP;
        }

        if (levelText != null)
        {
            levelText.text = "Level " + playerStats.level;
        }

        if (xpText != null)
        {
            xpText.text =
                playerStats.currentXP +
                " / " +
                playerStats.xpToNextLevel +
                " XP";
        }
    }
}