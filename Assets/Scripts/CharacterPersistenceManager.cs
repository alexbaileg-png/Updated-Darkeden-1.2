using UnityEngine;
using System.IO;

public class CharacterPersistenceManager : MonoBehaviour
{
    public static CharacterPersistenceManager Instance;

    [Header("References")]
    public PlayerStats playerStats;
    public PlayerGold playerGold;
    public PlayerSkillManager playerSkillManager;
    public Transform playerTransform;

    // Set at runtime by PlayerStats after enabling the correct caster
    [System.NonSerialized] public ISkillCaster skillCaster;

    [Header("Save Settings")]
    public string saveFileName = "player_character.json";

    private string SavePath
    {
        get
        {
            string prefix = GameSession.Instance?.SelectedCharacter?.characterId ?? "";
            string file = string.IsNullOrEmpty(prefix) ? saveFileName : prefix + "_" + saveFileName;
            return Path.Combine(Application.persistentDataPath, file);
        }
    }

    void Awake()
    {
        Instance = this;
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

        data.hasReceivedStartingItems = playerStats.hasReceivedStartingItems;

        if (skillCaster != null)
            data.keyBindings = skillCaster.GetBindings();

        if (playerTransform != null)
        {
            data.positionX = playerTransform.position.x;
            data.positionY = playerTransform.position.y;
            data.positionZ = playerTransform.position.z;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        // Sync level back to cloud account data so character select shows correct level
        CharacterData cloudChar = GameSession.Instance?.SelectedCharacter;
        if (cloudChar != null && cloudChar.level != data.level)
        {
            cloudChar.level = data.level;
            _ = CloudDataService.SaveAccountAsync(GameSession.Instance.AccountData);
        }

        Debug.Log("Character saved to: " + SavePath);
    }

    public void LoadCharacter()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No character save found — applying class base stats for new character.");
            if (playerStats != null)
                playerStats.ApplyClassBaseStats();
            return;
        }

        string json = File.ReadAllText(SavePath);
        CharacterSaveData data = JsonUtility.FromJson<CharacterSaveData>(json);

        if (playerStats != null)
        {
            playerStats.LoadFromSave(
                data.level, data.currentXP, data.xpToNextLevel, data.availableStatPoints,
                data.baseStrength, data.baseDexterity, data.baseIntelligence, data.baseEndurance
            );
            playerStats.hasReceivedStartingItems = data.hasReceivedStartingItems;
        }

        if (playerSkillManager != null)
        {
            playerSkillManager.LoadSkillPoints(data.availableSkillPoints);

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

        if (skillCaster != null && data.keyBindings != null)
            skillCaster.LoadBindings(data.keyBindings);

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