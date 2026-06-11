using System;

public enum PlayerFaction { Vampire, Slayer }
public enum VampireBloodline { BloodKnight, Shadow, Sorcerer, Dominator }
public enum SlayerClass { Swordmaster, Soldier, Enchanter, Healer }

[Serializable]
public class CharacterData
{
    public string characterId;
    public string characterName;
    public PlayerFaction faction;

    // Only one of these is used depending on faction
    public VampireBloodline vampireBloodline;
    public SlayerClass slayerClass;

    public int level = 1;
    public int currentXP = 0;
    public long lastPlayedUtc = 0;

    public string GetClassName()
    {
        if (faction == PlayerFaction.Vampire)
            return vampireBloodline.ToString();
        return slayerClass.ToString();
    }

    public string GetFactionDisplay() => faction == PlayerFaction.Vampire ? "Vampire" : "Slayer";
}

[Serializable]
public class AccountData
{
    public CharacterData[] characters = new CharacterData[5];
}
