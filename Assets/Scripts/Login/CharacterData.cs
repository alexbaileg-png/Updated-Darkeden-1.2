using System;

public enum PlayerFaction { Vampire, Slayer }
public enum PlayerGender { Male, Female }
public enum SlayerClass { Swordmaster, Soldier, Enchanter, Healer }

[Serializable]
public class CharacterData
{
    public string characterId;
    public string characterName;
    public PlayerFaction faction;
    public PlayerGender gender;
    public SlayerClass slayerClass;

    public int level = 1;
    public int currentXP = 0;
    public long lastPlayedUtc = 0;

    public string GetClassName()
    {
        if (faction == PlayerFaction.Vampire)
            return "Vampire";
        return slayerClass.ToString();
    }

    public string GetFactionDisplay() => faction == PlayerFaction.Vampire ? "Vampire" : "Slayer";
}

[Serializable]
public class AccountData
{
    public CharacterData[] characters = new CharacterData[5];
}
