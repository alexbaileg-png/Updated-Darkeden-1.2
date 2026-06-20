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
                SkillType.SingleShot,
                SkillType.TripleShot,
                SkillType.AutomaticFire,
                SkillType.OrbitalStrike,
            }
        },
        { "Enchanter", new List<SkillType>
            {
                SkillType.AuraBlast,
                SkillType.RunicTrap,
                SkillType.Disenchant,
                SkillType.RunicNova,
            }
        },

        // ── Vampire ───────────────────────────────────────────────────────────
        { "Vampire", new List<SkillType>
            {
                SkillType.DarkBolt,
                SkillType.BloodDrain,
                SkillType.BloodFog,
                SkillType.VoidBurst,
                SkillType.BloodyTalons,
                SkillType.BloodArmor,
                SkillType.CrimsonCyclone,
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
