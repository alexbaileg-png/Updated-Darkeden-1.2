using UnityEngine;

/// <summary>
/// Defines base stats for each faction/class combination.
/// Called once when a new character enters the game world for the first time.
/// </summary>
public static class ClassBaseStats
{
    public struct BaseStats
    {
        public int strength;
        public int dexterity;
        public int intelligence;
        public int endurance;
        public int baseHealth;
        public int baseMana;
    }

    public static BaseStats GetStats(CharacterData character)
    {
        if (character == null) return Default();

        if (character.faction == PlayerFaction.Vampire)
            return GetVampireStats(character.vampireBloodline);
        else
            return GetSlayerStats(character.slayerClass);
    }

    static BaseStats GetVampireStats(VampireBloodline bloodline)
    {
        switch (bloodline)
        {
            case VampireBloodline.BloodKnight:
                return new BaseStats
                {
                    strength     = 18,
                    dexterity    = 10,
                    intelligence = 8,
                    endurance    = 18,
                    baseHealth   = 220,
                    baseMana     = 80
                };

            case VampireBloodline.Shadow:
                return new BaseStats
                {
                    strength     = 12,
                    dexterity    = 20,
                    intelligence = 10,
                    endurance    = 12,
                    baseHealth   = 140,
                    baseMana     = 100
                };

            case VampireBloodline.Sorcerer:
                return new BaseStats
                {
                    strength     = 8,
                    dexterity    = 10,
                    intelligence = 22,
                    endurance    = 10,
                    baseHealth   = 120,
                    baseMana     = 200
                };

            case VampireBloodline.Dominator:
                return new BaseStats
                {
                    strength     = 8,
                    dexterity    = 12,
                    intelligence = 18,
                    endurance    = 14,
                    baseHealth   = 140,
                    baseMana     = 160
                };

            default: return Default();
        }
    }

    static BaseStats GetSlayerStats(SlayerClass slayerClass)
    {
        switch (slayerClass)
        {
            case SlayerClass.Swordmaster:
                return new BaseStats
                {
                    strength     = 16,
                    dexterity    = 16,
                    intelligence = 8,
                    endurance    = 14,
                    baseHealth   = 180,
                    baseMana     = 80
                };

            case SlayerClass.Soldier:
                return new BaseStats
                {
                    strength     = 10,
                    dexterity    = 18,
                    intelligence = 10,
                    endurance    = 16,
                    baseHealth   = 180,
                    baseMana     = 80
                };

            case SlayerClass.Enchanter:
                return new BaseStats
                {
                    strength     = 8,
                    dexterity    = 10,
                    intelligence = 22,
                    endurance    = 14,
                    baseHealth   = 140,
                    baseMana     = 200
                };

            case SlayerClass.Healer:
                return new BaseStats
                {
                    strength     = 8,
                    dexterity    = 12,
                    intelligence = 20,
                    endurance    = 18,
                    baseHealth   = 160,
                    baseMana     = 180
                };

            default: return Default();
        }
    }

    static BaseStats Default()
    {
        return new BaseStats
        {
            strength     = 10,
            dexterity    = 10,
            intelligence = 10,
            endurance    = 10,
            baseHealth   = 150,
            baseMana     = 100
        };
    }
}
