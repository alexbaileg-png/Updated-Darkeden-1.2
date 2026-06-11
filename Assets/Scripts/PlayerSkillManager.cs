using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerSkillManager : NetworkBehaviour
{
    [Header("Skill Points")]
    public int skillPointsPerLevel = 1;

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

    // Server-authoritative skill point count — syncs to all clients automatically
    private readonly SyncVar<int> _availableSkillPoints = new SyncVar<int>();

    public int availableSkillPoints => _availableSkillPoints.Value;

    void Awake()
    {
        InitializeSkills();
        // Starting skill unlocked in OnStartClient once GameSession/character is available
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _availableSkillPoints.OnChange += OnSkillPointsChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _availableSkillPoints.OnChange -= OnSkillPointsChanged;
    }

    void OnSkillPointsChanged(int prev, int next, bool asServer) { }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
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
        // Auto-detect the first skill for this character's class from ClassSkillConfig
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character == null)
        {
            Debug.LogWarning("[SkillManager] No character in GameSession — starting skill not unlocked.");
            return;
        }

        var classSkills = ClassSkillConfig.GetSkillsForCharacter(character);
        if (classSkills == null || classSkills.Count == 0)
        {
            Debug.LogWarning($"[SkillManager] No skills configured for class '{character.GetClassName()}'.");
            return;
        }

        SkillType startingSkill = classSkills[0];
        PlayerSkillProgress progress = GetSkillProgress(startingSkill);
        if (progress == null)
        {
            Debug.LogWarning($"[SkillManager] Starting skill {startingSkill} not found in allSkills array. " +
                             "Make sure all class skills are added to the prefab's allSkills list.");
            return;
        }

        progress.unlocked = true;
        progress.skillLevel = 1;
        Debug.Log($"[SkillManager] Starting skill unlocked: {progress.skill.skillName} for {character.GetClassName()}");
    }

    // Called by CharacterPersistenceManager to restore saved skill points
    public void LoadSkillPoints(int savedPoints)
    {
        if (!IsServerStarted) return;
        _availableSkillPoints.Value = savedPoints;
    }

    // Called by PlayerStats.LevelUp() on the server
    public void GainSkillPoint()
    {
        if (!IsServerStarted) return;
        _availableSkillPoints.Value += skillPointsPerLevel;
    }

    [ServerRpc]
    public void ServerUnlockOrLevelSkill(SkillType skillType)
    {
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null && !ClassSkillConfig.CanUseSkill(character, skillType))
        {
            Debug.LogWarning($"[SkillManager] {character.characterName} tried to unlock {skillType} but their class ({character.GetClassName()}) cannot use it.");
            return;
        }

        PlayerStats playerStats = GetComponent<PlayerStats>();
        int playerLevel = playerStats != null ? playerStats.level : 1;

        if (!TrySpendSkillPoint(skillType, playerLevel))
            return;

        PlayerSkillProgress progress = GetSkillProgress(skillType);
        if (progress != null)
            RpcSyncSkill(skillType, progress.unlocked, progress.skillLevel);
    }

    bool TrySpendSkillPoint(SkillType skillType, int playerLevel)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        if (progress == null || progress.skill == null) return false;
        if (_availableSkillPoints.Value <= 0) return false;
        if (playerLevel < progress.skill.requiredPlayerLevel) return false;

        if (!progress.unlocked)
        {
            progress.unlocked = true;
            progress.skillLevel = 1;
        }
        else
        {
            if (progress.skillLevel >= progress.skill.maxSkillLevel) return false;
            progress.skillLevel++;
        }

        _availableSkillPoints.Value--;
        return true;
    }

    [ObserversRpc]
    void RpcSyncSkill(SkillType skillType, bool unlocked, int skillLevel)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        if (progress == null) return;
        progress.unlocked = unlocked;
        progress.skillLevel = skillLevel;
    }

    public bool UnlockOrLevelSkill(SkillType skillType, int playerLevel)
    {
        ServerUnlockOrLevelSkill(skillType);
        return true;
    }

    public bool IsSkillUnlocked(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        return progress != null && progress.unlocked;
    }

    public int GetSkillLevel(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        return progress != null ? progress.skillLevel : 0;
    }

    public SkillData GetSkillData(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        return progress?.skill;
    }

    public int GetSkillPower(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        if (progress == null || progress.skill == null || progress.skillLevel <= 0) return 0;
        return progress.skill.basePower + ((progress.skillLevel - 1) * progress.skill.powerPerSkillLevel);
    }

    public float GetSkillCooldown(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        if (progress == null || progress.skill == null || progress.skillLevel <= 0) return 999f;
        float cooldown = progress.skill.baseCooldown -
                         ((progress.skillLevel - 1) * progress.skill.cooldownReductionPerSkillLevel);
        return Mathf.Max(0.1f, cooldown);
    }

    public int GetSkillManaCost(SkillType skillType)
    {
        PlayerSkillProgress progress = GetSkillProgress(skillType);
        if (progress == null || progress.skill == null || progress.skillLevel <= 0) return 0;
        return progress.skill.baseManaCost +
               ((progress.skillLevel - 1) * progress.skill.manaCostIncreasePerSkillLevel);
    }

    PlayerSkillProgress GetSkillProgress(SkillType skillType)
    {
        if (skillProgress == null) return null;
        foreach (PlayerSkillProgress progress in skillProgress)
            if (progress != null && progress.skill != null && progress.skill.skillType == skillType)
                return progress;
        return null;
    }
}
