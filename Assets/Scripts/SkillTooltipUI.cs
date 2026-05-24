using UnityEngine;
using TMPro;

public class SkillTooltipUI : MonoBehaviour
{
    public static SkillTooltipUI Instance;

    [Header("UI")]
    public GameObject tooltipRoot;
    public TMP_Text tooltipText;

    void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    public void ShowTooltip(SkillData skillData, PlayerSkillManager skillManager, PlayerStats playerStats)
    {
        if (tooltipRoot == null || tooltipText == null || skillData == null)
            return;

        int level = 0;
        int power = skillData.basePower;
        float cooldown = skillData.baseCooldown;
        int manaCost = skillData.baseManaCost;
        bool unlocked = false;

        if (skillManager != null)
        {
            level = skillManager.GetSkillLevel(skillData.skillType);
            power = skillManager.GetSkillPower(skillData.skillType);
            cooldown = skillManager.GetSkillCooldown(skillData.skillType);
            manaCost = skillManager.GetSkillManaCost(skillData.skillType);
            unlocked = skillManager.IsSkillUnlocked(skillData.skillType);
        }

        if (level <= 0)
            level = 0;

        string text = "";

        text += skillData.skillName + "\n";
        text += unlocked ? "Unlocked\n" : "Locked\n";
        text += "Level: " + level + " / " + skillData.maxSkillLevel + "\n";
        text += "Requires Level: " + skillData.requiredPlayerLevel + "\n\n";

        text += "Power: " + power + "\n";
        text += "Mana Cost: " + manaCost + "\n";
        text += "Cooldown: " + cooldown.ToString("0.00") + "s\n\n";

        text += skillData.description;

        tooltipText.text = text;
        tooltipRoot.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }
}