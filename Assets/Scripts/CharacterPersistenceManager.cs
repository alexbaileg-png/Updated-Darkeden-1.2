using UnityEngine;
using System.IO;
using System.Collections;

public class CharacterPersistenceManager : MonoBehaviour
{
    public static CharacterPersistenceManager Instance;

    [Header("References")]
    public PlayerStats playerStats;
    public PlayerGold playerGold;
    public PlayerSkillManager playerSkillManager;
    public Transform playerTransform;

    [Header("Save Settings")]
    public string saveFileName = "player_character.json";

    private string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, saveFileName); }
    }

    void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        LoadCharacter();
    }

    void OnApplicationQuit()
    {
        SaveCharacter();
    }

    public void SaveAfterChange()
    {
        SaveCharacter();
    }

    public void SaveCharacter()
    {
        if (playerStats == null)
            return;

        CharacterSaveData data = new CharacterSaveData();

        data.level = playerStats.level;
        data.currentXP = playerStats.currentXP;
        data.xpToNextLevel = playerStats.xpToNextLevel;
        data.availableStatPoints = playerStats.availableStatPoints;

        data.baseStrength = playerStats.baseStrength;
        data.baseDexterity = playerStats.baseDexterity;
        data.baseIntelligence = playerStats.baseIntelligence;
        data.baseEndurance = playerStats.baseEndurance;

        data.currentHealth = playerStats.currentHealth;
        data.currentMana = playerStats.currentMana;

        if (playerSkillManager != null)
        {
            data.availableSkillPoints = playerSkillManager.availableSkillPoints;

            if (playerSkillManager.skillProgress != null)
            {
                foreach (PlayerSkillManager.PlayerSkillProgress progress in playerSkillManager.skillProgress)
                {
                    if (progress == null || progress.skill == null)
                        continue;

                    SavedSkillData savedSkill = new SavedSkillData();
                    savedSkill.skillType = progress.skill.skillType.ToString();
                    savedSkill.unlocked = progress.unlocked;
                    savedSkill.skillLevel = progress.skillLevel;

                    data.savedSkills.Add(savedSkill);
                }
            }
        }

        if (playerGold != null)
            data.gold = playerGold.gold;

        if (playerTransform != null)
        {
            data.positionX = playerTransform.position.x;
            data.positionY = playerTransform.position.y;
            data.positionZ = playerTransform.position.z;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Character saved to: " + SavePath);
    }

    public void LoadCharacter()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No character save found yet.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        CharacterSaveData data = JsonUtility.FromJson<CharacterSaveData>(json);

        if (playerStats != null)
        {
            playerStats.level = data.level;
            playerStats.currentXP = data.currentXP;
            playerStats.xpToNextLevel = data.xpToNextLevel;
            playerStats.availableStatPoints = data.availableStatPoints;

            playerStats.baseStrength = data.baseStrength;
            playerStats.baseDexterity = data.baseDexterity;
            playerStats.baseIntelligence = data.baseIntelligence;
            playerStats.baseEndurance = data.baseEndurance;

            playerStats.RecalculateStats();

            playerStats.currentHealth = playerStats.maxHealth;
            playerStats.currentMana = playerStats.maxMana;
        }

        if (playerSkillManager != null)
        {
            playerSkillManager.availableSkillPoints = data.availableSkillPoints;

            if (playerSkillManager.skillProgress != null && data.savedSkills != null)
            {
                foreach (SavedSkillData savedSkill in data.savedSkills)
                {
                    foreach (PlayerSkillManager.PlayerSkillProgress progress in playerSkillManager.skillProgress)
                    {
                        if (progress == null || progress.skill == null)
                            continue;

                        if (progress.skill.skillType.ToString() == savedSkill.skillType)
                        {
                            progress.unlocked = savedSkill.unlocked;
                            progress.skillLevel = savedSkill.skillLevel;
                            break;
                        }
                    }
                }
            }
        }

        if (playerGold != null)
            playerGold.gold = data.gold;

        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(
                data.positionX,
                data.positionY,
                data.positionZ
            );
        }

        Debug.Log("Character loaded from: " + SavePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Character save deleted.");
        }
    }
}