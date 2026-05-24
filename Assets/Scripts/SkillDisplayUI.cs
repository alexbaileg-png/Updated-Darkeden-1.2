using UnityEngine;
using UnityEngine.UI;

public class SkillDisplayUI : MonoBehaviour
{
    [Header("Player Skills")]
    public PlayerProjectileAttack playerSkills;

    [Header("Skill Display")]
    public Image skillIcon;

    [Header("Icons")]
    public Sprite holyBoltIcon;
    public Sprite holyRainIcon;
    public Sprite holyCircleHealIcon;
    public Sprite healingOrbitIcon;

    void Update()
    {
        UpdateSkillIcon();
    }

    void UpdateSkillIcon()
    {
        if (playerSkills == null || skillIcon == null)
            return;

        skillIcon.sprite = GetIcon(playerSkills.currentSelectedSkill);
    }

    Sprite GetIcon(SkillType skill)
    {
        if (skill == SkillType.HolyBolt)
            return holyBoltIcon;

        if (skill == SkillType.HolyRain)
            return holyRainIcon;

        if (skill == SkillType.HolyCircleHeal)
            return holyCircleHealIcon;

        if (skill == SkillType.HealingOrbit)
            return healingOrbitIcon;

        return null;
    }
}