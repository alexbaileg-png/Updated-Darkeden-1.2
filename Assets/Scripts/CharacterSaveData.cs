using System;
using System.Collections.Generic;

[Serializable]
public class CharacterSaveData
{
    public int level;
    public int currentXP;
    public int xpToNextLevel;
    public int availableStatPoints;

    public int baseStrength;
    public int baseDexterity;
    public int baseIntelligence;
    public int baseEndurance;

    public int currentHealth;
    public int currentMana;

    public int availableSkillPoints;
    public List<SavedSkillData> savedSkills = new List<SavedSkillData>();

    public int gold;

    public float positionX;
    public float positionY;
    public float positionZ;
}

[Serializable]
public class SavedSkillData
{
    public string skillType;
    public bool unlocked;
    public int skillLevel;
}