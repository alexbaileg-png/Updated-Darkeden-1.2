using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "ARPG/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public SkillType skillType;
    public Sprite skillIcon;

    [TextArea]
    public string description;

    [Header("Requirements")]
    public int requiredPlayerLevel = 1;

    [Header("Skill Leveling")]
    public int maxSkillLevel = 10;

    [Header("Combat Values")]
    public int basePower = 20;
    public int powerPerSkillLevel = 5;

    public float baseCooldown = 1f;
    public float cooldownReductionPerSkillLevel = 0.05f;

    public int baseManaCost = 5;
    public int manaCostIncreasePerSkillLevel = 1;
}