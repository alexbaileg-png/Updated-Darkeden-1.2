using System.Collections.Generic;

/// <summary>
/// Defines which skills are available to each faction/class.
/// Add new skills here as they are created.
/// </summary>
public static class ClassSkillConfig
{
    private static readonly Dictionary<string, List<SkillType>> _classSkills
        = new Dictionary<string, List<SkillType>>
    {
        // ── Slayer Classes ────────────────────────────────────────────────────
        { "Healer", new List<SkillType>
            {
                SkillType.HolyBolt,
                SkillType.HolyRain,
                SkillType.HolyCircleHeal,
                SkillType.HealingOrbit
            }
        },
        { "Swordmaster", new List<SkillType>
            {
                SkillType.JudgmentRush,
                SkillType.Consecration,
                SkillType.AegisOfFaith,
                SkillType.RadiantFlurry,
            }
        },
        { "Soldier", new List<SkillType>
            {
                // Add Soldier skills here as they are created
            }
        },
        { "Enchanter", new List<SkillType>
            {
                // Add Enchanter skills here as they are created
            }
        },

        // ── Vampire Bloodlines ────────────────────────────────────────────────
        { "BloodKnight", new List<SkillType>
            {
                // Add Blood Knight skills here as they are created
            }
        },
        { "Shadow", new List<SkillType>
            {
                // Add Shadow skills here as they are created
            }
        },
        { "Sorcerer", new List<SkillType>
            {
                // Add Sorcerer skills here as they are created
            }
        },
        { "Dominator", new List<SkillType>
            {
                SkillType.DarkBolt,
                SkillType.BloodDrain,
                SkillType.BloodFog,
                SkillType.VoidBurst,
            }
        },
    };

    public static List<SkillType> GetSkillsForCharacter(CharacterData character)
    {
        if (character == null) return new List<SkillType>();

        string key = character.GetClassName(); // returns e.g. "Healer", "BloodKnight"
        if (_classSkills.TryGetValue(key, out List<SkillType> skills))
            return skills;

        return new List<SkillType>();
    }

    public static bool CanUseSkill(CharacterData character, SkillType skill)
    {
        List<SkillType> skills = GetSkillsForCharacter(character);
        return skills.Contains(skill);
    }
}
