using UnityEngine;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("Skill Points")]
    public int availableSkillPoints = 0;
    public int skillPointsPerLevel = 1;

    [Header("Starting Skill")]
    public SkillType freeStartingSkill = SkillType.HolyBolt;

    [Header("Skills")]
    public SkillData[] allSkills;

    [System.Serializable]
    public class PlayerSkillProgress
    {
        public SkillData skill;
        public bool unlocked;
        public int skillLevel;
    }

    public PlayerSkillProgress[] skillProgress;

    void Start()
    {
        InitializeSkills();
        UnlockStartingSkill();
    }

    public void InitializeSkills()
    {
        if (allSkills == null)
            return;

        skillProgress = new PlayerSkillProgress[allSkills.Length];

        for (int i = 0; i < allSkills.Length; i++)
        {
            skillProgress[i] = new PlayerSkillProgress();
            skillProgress[i].skill = allSkills[i];
            skillProgress[i].unlocked = false;
            skillProgress[i].skillLevel = 0;
        }
    }

    void UnlockStartingSkill()
    {
        PlayerSkillProgress progress = GetSkillProgress(freeStartingSkill);

        if (progress == null)
            return;

        progress.unlocked = true;
        progress.skillLevel = 1;

        Debug.Log("Starting skill unlocked: " + progress.skill.skillName);
    }

    public void GainSkillPoint()
    {
        availableSkillPoints += skillPointsPerLevel;
    }

    public bool IsSkillUnlocked(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        return progress != null && progress.unlocked;
    }

    public int GetSkillLevel(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);

        if (progress == null)
            return 0;

        return progress.skillLevel;
    }

    public SkillData GetSkillData(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);

        if (progress == null)
            return null;

        return progress.skill;
    }

    public bool UnlockOrLevelSkill(SkillType skillType, int playerLevel)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);

        if (progress == null || progress.skill == null)
            return false;

        if (availableSkillPoints <= 0)
            return false;

        if (playerLevel < progress.skill.requiredPlayerLevel)
            return false;

        if (!progress.unlocked)
        {
            progress.unlocked = true;
            progress.skillLevel = 1;
            availableSkillPoints--;
            return true;
        }

        if (progress.skillLevel >= progress.skill.maxSkillLevel)
            return false;

        progress.skillLevel++;
        availableSkillPoints--;
        return true;
    }

    public int GetSkillPower(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);

        if (progress == null || progress.skill == null || progress.skillLevel <= 0)
            return 0;

        return progress.skill.basePower + ((progress.skillLevel - 1) * progress.skill.powerPerSkillLevel);
    }

    public float GetSkillCooldown(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);

        if (progress == null || progress.skill == null || progress.skillLevel <= 0)
            return 999f;

        float cooldown = progress.skill.baseCooldown -
                         ((progress.skillLevel - 1) * progress.skill.cooldownReductionPerSkillLevel);

        return Mathf.Max(0.1f, cooldown);
    }

    public int GetSkillManaCost(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);

        if (progress == null || progress.skill == null || progress.skillLevel <= 0)
            return 0;

        return progress.skill.baseManaCost +
               ((progress.skillLevel - 1) * progress.skill.manaCostIncreasePerSkillLevel);
    }

    PlayerSkillProgress GetSkillProgress(SkillType skillType)
    {
        if (skillProgress == null)
            return null;

        foreach (PlayerSkillProgress progress in skillProgress)
        {
            if (progress != null &&
                progress.skill != null &&
                progress.skill.skillType == skillType)
            {
                return progress;
            }
        }

        return null;
    }
}