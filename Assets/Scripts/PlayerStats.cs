using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;

public class PlayerStats : NetworkBehaviour, IDamageable
{
    // Static reference to the local player's stats — used by UI scripts
    public static PlayerStats LocalInstance;

    [Header("Faction")]
    public PlayerFaction faction = PlayerFaction.Slayer;
    [HideInInspector] public bool hasReceivedStartingItems = false;

    [Header("Level")]
    public int level = 1;
    public int xpToNextLevel = 100;

    [Header("Assignable Stat Points")]
    public int statPointsPerLevel = 3;

    [Header("Base Core Stats")]
    public int baseStrength = 10;
    public int baseDexterity = 10;
    public int baseIntelligence = 10;
    public int baseEndurance = 10;

    [Header("Final Core Stats")]
    public int strength;
    public int dexterity;
    public int intelligence;
    public int endurance;

    [Header("Gear Core Bonuses")]
    public int gearStrength, gearDexterity, gearIntelligence, gearEndurance;

    [Header("Gear Resource Bonuses")]
    public int gearHealth, gearMana;

    [Header("Gear Offensive Bonuses")]
    public int gearMeleeDamage, gearRangedDamage, gearMagicDamage;

    [Header("Gear Defensive Bonuses")]
    public int gearArmor, gearResistance, gearMeleeResistance, gearMagicResistance, gearAllResistance;

    [Header("Resources")]
    public int maxHealth;
    public int maxMana;

    [Header("Derived Combat")]
    public float meleeDamageBonus;
    public float rangedDamageBonus;
    public float magicalSkillPower;
    public float magicalArmorPenetration;
    public float buffEffectivenessBonus;
    public float buffDurationBonus;
    public float armor;
    public float resistance;
    public float meleeResistance;
    public float magicResistance;
    public float criticalChance;

    // Synced values — all clients see these
    private readonly SyncVar<int> _currentHealth = new SyncVar<int>();
    private readonly SyncVar<int> _currentMana = new SyncVar<int>();
    private readonly SyncVar<int> _level = new SyncVar<int>();
    private readonly SyncVar<int> _currentXP = new SyncVar<int>();
    private readonly SyncVar<int> _xpToNextLevel = new SyncVar<int>();
    private readonly SyncVar<int> _availableStatPoints = new SyncVar<int>();

    // Public accessors so UI reads the synced values
    public int currentHealth => _currentHealth.Value;
    public int currentMana => _currentMana.Value;
    public int currentXP => _currentXP.Value;
    public int availableStatPoints => _availableStatPoints.Value;

    // Synced so late-joining clients see dead players correctly
    private readonly SyncVar<bool> _isDead = new SyncVar<bool>();
    public bool isDead => _isDead.Value;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _level.OnChange += OnLevelChanged;
        _isDead.OnChange += OnDeadChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _level.OnChange -= OnLevelChanged;
        _isDead.OnChange -= OnDeadChanged;
    }

    void OnDeadChanged(bool prev, bool next, bool asServer)
    {
        NetworkPlayerController netCtrl = GetComponent<NetworkPlayerController>();
        if (netCtrl != null)
        {
            if (next) netCtrl.OnPlayerDied();
            else      netCtrl.OnPlayerRespawned(transform.position);
        }

        // Show/hide death screen only for the player who owns this object
        if (IsOwner)
        {
            DeathScreenUI deathScreen = FindObjectOfType<DeathScreenUI>(true);
            if (deathScreen != null)
            {
                if (next) deathScreen.Show();
                else      deathScreen.Hide();
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null)
            faction = character.faction;

        RecalculateStats();
        _currentHealth.Value = maxHealth;
        _currentMana.Value = maxMana;
        _level.Value = level;
        _xpToNextLevel.Value = xpToNextLevel;
        _availableStatPoints.Value = 0;
        _currentXP.Value = 0;
        EnemyAI.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        EnemyAI.UnregisterPlayer(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;

        LocalInstance = this;

        CharacterData localChar = GameSession.Instance?.SelectedCharacter;
        if (localChar != null) faction = localChar.faction;

        WireUpSceneUI();
    }

    void WireUpSceneUI()
    {
        DeathScreenUI deathScreen = FindObjectOfType<DeathScreenUI>(true);
        if (deathScreen != null) deathScreen.Hide();

        PlayerHUD hud = FindObjectOfType<PlayerHUD>(true);
        if (hud != null) hud.playerStats = this;

        PlayerUI playerUI = FindObjectOfType<PlayerUI>(true);
        if (playerUI != null) playerUI.playerStats = this;

        StatsPanelUI statsPanel = FindObjectOfType<StatsPanelUI>(true);
        if (statsPanel != null) statsPanel.playerStats = this;

        InventoryGrid grid = FindObjectOfType<InventoryGrid>(true);

        PlayerLootPickup lootPickup = GetComponent<PlayerLootPickup>();
        if (lootPickup != null && grid != null)
            lootPickup.inventoryGrid = grid;

        EquipmentManager equipManager = GetComponent<EquipmentManager>();
        if (equipManager != null)
        {
            equipManager.playerStats = this;
            equipManager.inventoryGrid = grid;

            foreach (EquipmentSlot slot in FindObjectsOfType<EquipmentSlot>(true))
            {
                slot.equipmentManager = equipManager;

                switch (slot.allowedItemType)
                {
                    case ItemType.Helmet:    equipManager.helmetSlot = slot;      break;
                    case ItemType.Necklace:  equipManager.necklaceSlot = slot;    break;
                    case ItemType.Top:       equipManager.topSlot = slot;         break;
                    case ItemType.Bottom:    equipManager.bottomSlot = slot;      break;
                    case ItemType.Boots:     equipManager.bootsSlot = slot;       break;
                    case ItemType.Gloves:    equipManager.glovesSlot   = slot;    break;
                    case ItemType.Belt:      equipManager.beltSlot     = slot;    break;
                    case ItemType.Weapon:
                        if (equipManager.rightWeaponSlot == null)
                            equipManager.rightWeaponSlot = slot;
                        else
                            equipManager.leftWeaponSlot = slot;
                        break;
                }
            }

            equipManager.RecalculateEquipmentStats();
        }

        PlayerSkillManager skillMgr = GetComponent<PlayerSkillManager>();
        SkillTreeUI skillTreeUI = FindObjectOfType<SkillTreeUI>(true);
        if (skillTreeUI != null && skillMgr != null)
            skillTreeUI.WirePlayer(skillMgr, this);

        ISkillCaster skillCaster = EnableClassSkillCaster();

        SkillSelectorUI skillSelector = FindObjectOfType<SkillSelectorUI>(true);
        if (skillSelector != null) skillSelector.SetSkillCaster(skillCaster);

        SkillDisplayUI skillDisplay = FindObjectOfType<SkillDisplayUI>(true);
        if (skillDisplay != null) skillDisplay.SetSkillCaster(skillCaster);

        SkillCooldownUI skillCooldown = FindObjectOfType<SkillCooldownUI>(true);
        if (skillCooldown != null) skillCooldown.SetSkillCaster(skillCaster);

        LootBagSelectorUI lootSelector = FindObjectOfType<LootBagSelectorUI>(true);
        if (lootSelector != null) lootSelector.playerLootPickup = GetComponent<PlayerLootPickup>();

        foreach (SkillGridButton sgb in FindObjectsOfType<SkillGridButton>(true))
        {
            sgb.skillManager = skillMgr;
            sgb.playerStats = this;
        }

        VialHotbar vialHotbar = FindObjectOfType<VialHotbar>(true);
        if (vialHotbar != null)
        {
            vialHotbar.playerStats = this;
            if (equipManager != null)
                vialHotbar.equipmentManager = equipManager;
        }

        InventorySaveManager inventorySaveManager = FindObjectOfType<InventorySaveManager>(true);
        if (inventorySaveManager != null)
        {
            inventorySaveManager.equipmentManager = equipManager;
            if (grid != null) inventorySaveManager.inventoryGrid = grid;
        }

        CharacterPersistenceManager charPersistence = FindObjectOfType<CharacterPersistenceManager>(true);
        if (charPersistence != null)
        {
            charPersistence.playerStats = this;
            charPersistence.playerTransform = transform;
            PlayerSkillManager skillManager = GetComponent<PlayerSkillManager>();
            if (skillManager != null) charPersistence.playerSkillManager = skillManager;
            PlayerGold gold = GetComponent<PlayerGold>();
            if (gold != null) charPersistence.playerGold = gold;

            charPersistence.LoadCharacter();
        }

        InventoryPersistenceManager invPersistence = FindObjectOfType<InventoryPersistenceManager>(true);
        if (invPersistence != null)
            invPersistence.LoadInventory();

        if (!hasReceivedStartingItems && grid != null)
        {
            grid.AddStartingItems(faction);
            hasReceivedStartingItems = true;
            if (charPersistence != null)
                charPersistence.SaveCharacter();
        }
    }

    ISkillCaster EnableClassSkillCaster()
    {
        string className = GameSession.Instance?.SelectedCharacter?.GetClassName() ?? "";

        SwordmasterSkillCaster swordmaster = GetComponent<SwordmasterSkillCaster>();
        HealerSkillCaster      healer      = GetComponent<HealerSkillCaster>();
        VampireSkillCaster     vampire     = GetComponent<VampireSkillCaster>();

        if (swordmaster != null) swordmaster.enabled = false;
        if (healer      != null) healer.enabled      = false;
        if (vampire     != null) vampire.enabled      = false;

        if (faction == PlayerFaction.Vampire)
        {
            if (vampire != null) vampire.enabled = true;
            return vampire;
        }

        switch (className)
        {
            case "Swordmaster":
                if (swordmaster != null) swordmaster.enabled = true;
                return swordmaster;
            case "Healer":
                if (healer != null) healer.enabled = true;
                return healer;
            default:
                if (swordmaster != null) swordmaster.enabled = true;
                return swordmaster;
        }
    }

    void OnLevelChanged(int prev, int next, bool asServer)
    {
        level = next;
        xpToNextLevel = _xpToNextLevel.Value;
    }

    public void RecalculateStats()
    {
        strength = baseStrength + gearStrength;
        dexterity = baseDexterity + gearDexterity;
        intelligence = baseIntelligence + gearIntelligence;
        endurance = baseEndurance + gearEndurance;

        meleeDamageBonus = (strength * 2.0f) + gearMeleeDamage;
        rangedDamageBonus = (dexterity * 1.75f) + gearRangedDamage;
        magicalSkillPower = (intelligence * 2.25f) + gearMagicDamage;
        magicalArmorPenetration = intelligence * 0.5f;
        buffEffectivenessBonus = intelligence * 0.01f;
        buffDurationBonus = intelligence * 0.015f;
        criticalChance = CalculateCriticalChance(dexterity);

        if (faction == PlayerFaction.Vampire)
            maxHealth = 100 + (strength * 3) + (endurance * 12) + (intelligence * 5) + gearHealth;
        else
            maxHealth = 100 + (strength * 3) + (endurance * 12) + gearHealth;

        maxMana = 50 + (intelligence * 8) + gearMana;

        armor = (strength * 0.25f) + (dexterity * 0.15f) + (endurance * 1.25f) + gearArmor;
        resistance = (intelligence * 0.75f) + (endurance * 0.35f) + gearResistance;
        meleeResistance = armor + gearMeleeResistance + gearAllResistance;
        magicResistance = resistance + gearMagicResistance + gearAllResistance;

        if (IsServerStarted)
        {
            _currentHealth.Value = Mathf.Clamp(_currentHealth.Value, 0, maxHealth);
            _currentMana.Value = Mathf.Clamp(_currentMana.Value, 0, maxMana);
        }
    }

    public void ClearGearBonuses()
    {
        gearStrength = gearDexterity = gearIntelligence = gearEndurance = 0;
        gearHealth = gearMana = 0;
        gearMeleeDamage = gearRangedDamage = gearMagicDamage = 0;
        gearArmor = gearResistance = gearMeleeResistance = gearMagicResistance = gearAllResistance = 0;
    }

    public void AddGearBonuses(ItemData item)
    {
        if (item == null) return;

        gearStrength += item.strengthBonus;
        gearDexterity += item.dexterityBonus;
        gearIntelligence += item.intelligenceBonus;
        gearEndurance += item.enduranceBonus;
        gearHealth += item.healthBonus;
        gearMana += item.manaBonus;
        gearMeleeDamage += item.meleeDamageBonus;
        gearRangedDamage += item.rangedDamageBonus;
        gearMagicDamage += item.magicalDamageBonus;
        gearArmor += item.armorBonus;
        gearResistance += item.resistanceBonus;
        gearMeleeResistance += item.meleeResistanceBonus;
        gearMagicResistance += item.magicalResistanceBonus;
        gearAllResistance += item.allResistanceBonus;

        ApplyCrystalBonus(item);
    }

    void ApplyCrystalBonus(ItemData item)
    {
        if (item == null || !item.hasCrystalStat) return;

        int value = item.crystalCurrentValue;
        switch (item.crystalStat)
        {
            case ItemBonusStat.Strength: gearStrength += value; break;
            case ItemBonusStat.Dexterity: gearDexterity += value; break;
            case ItemBonusStat.Intelligence: gearIntelligence += value; break;
            case ItemBonusStat.Endurance: gearEndurance += value; break;
            case ItemBonusStat.Health: gearHealth += value; break;
            case ItemBonusStat.Mana: gearMana += value; break;
            case ItemBonusStat.MeleeDamage: gearMeleeDamage += value; break;
            case ItemBonusStat.RangedDamage: gearRangedDamage += value; break;
            case ItemBonusStat.MagicDamage: gearMagicDamage += value; break;
            case ItemBonusStat.Armor: gearArmor += value; break;
            case ItemBonusStat.Resistance: gearResistance += value; break;
            case ItemBonusStat.MeleeResistance: gearMeleeResistance += value; break;
            case ItemBonusStat.MagicResistance: gearMagicResistance += value; break;
            case ItemBonusStat.AllResistance: gearAllResistance += value; break;
        }
    }

    // ── Damage & Healing ──────────────────────────────────────────────────────

    public void TakeDamage(int incomingDamage)
    {
        ReceiveDamage(incomingDamage, DamageType.Melee);
    }

    public void ReceiveDamage(int finalDamage, DamageType damageType)
    {
        if (!IsServerStarted) return;
        if (isDead) return;

        int damageTaken = damageType == DamageType.Magical
            ? CalculateMagicDamageTaken(finalDamage, 0f)
            : CalculatePhysicalDamageTaken(finalDamage);

        _currentHealth.Value = Mathf.Clamp(_currentHealth.Value - damageTaken, 0, maxHealth);

        if (_currentHealth.Value <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (!IsServerStarted) return;
        if (isDead) return;

        _currentHealth.Value = Mathf.Clamp(_currentHealth.Value + amount, 0, maxHealth);

        if (CombatTextSpawner.Instance != null)
            CombatTextSpawner.Instance.SpawnText(transform.position + Vector3.up * 1.8f, "+" + amount);
    }

    public bool SpendMana(int amount)
    {
        if (_currentMana.Value < amount) return false;
        _currentMana.Value = Mathf.Clamp(_currentMana.Value - amount, 0, maxMana);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (!IsServerStarted) return;
        _currentMana.Value = Mathf.Clamp(_currentMana.Value + amount, 0, maxMana);
    }

    // ── XP & Leveling ─────────────────────────────────────────────────────────

    public void GainXP(int amount)
    {
        if (!IsServerStarted) return;

        if (_xpToNextLevel.Value <= 0)
            _xpToNextLevel.Value = xpToNextLevel > 0 ? xpToNextLevel : 100;

        _currentXP.Value += amount;

        while (_currentXP.Value >= _xpToNextLevel.Value)
        {
            _currentXP.Value -= _xpToNextLevel.Value;
            LevelUp();
        }

        CharacterPersistenceManager.Instance?.SaveAfterChange();
    }

    void LevelUp()
    {
        level++;
        _level.Value = level;
        _xpToNextLevel.Value = Mathf.RoundToInt(_xpToNextLevel.Value * 1.35f);
        xpToNextLevel = _xpToNextLevel.Value;

        baseStrength += 2;
        baseDexterity += 2;
        baseIntelligence += 2;
        baseEndurance += 2;

        _availableStatPoints.Value += statPointsPerLevel;

        RecalculateStats();

        _currentHealth.Value = maxHealth;
        _currentMana.Value = maxMana;

        PlayerSkillManager skillManager = GetComponent<PlayerSkillManager>();
        if (skillManager != null) skillManager.GainSkillPoint();

        CharacterPersistenceManager.Instance?.SaveAfterChange();
        GameSessionBridge.Instance?.SaveGameToSession();
    }

    // ── Stat Points ───────────────────────────────────────────────────────────

    [ServerRpc]
    public void ServerSpendStatPoint(string statName)
    {
        if (_availableStatPoints.Value <= 0) return;

        if (statName == "Strength") baseStrength++;
        else if (statName == "Dexterity") baseDexterity++;
        else if (statName == "Intelligence") baseIntelligence++;
        else if (statName == "Endurance") baseEndurance++;
        else return;

        _availableStatPoints.Value--;
        RecalculateStats();
    }

    public bool SpendStatPoint(string statName)
    {
        ServerSpendStatPoint(statName);
        return true;
    }

    // ── Damage Calculations ───────────────────────────────────────────────────

    public int GetMeleeDamage(int baseDamage) => Mathf.RoundToInt(baseDamage + meleeDamageBonus);
    public int GetRangedDamage(int baseDamage) => Mathf.RoundToInt(baseDamage + rangedDamageBonus);
    public int GetMagicalSkillDamage(int baseDamage) => Mathf.RoundToInt(baseDamage + magicalSkillPower);
    public int GetMagicalHealing(int baseHealing) => Mathf.RoundToInt(baseHealing + magicalSkillPower);

    public int CalculatePhysicalDamageTaken(int incomingDamage)
    {
        float reduction = meleeResistance / (meleeResistance + 100f);
        return Mathf.Max(1, Mathf.RoundToInt(incomingDamage * (1f - reduction)));
    }

    public int CalculateMagicDamageTaken(int incomingDamage, float enemyMagicPenetration)
    {
        float effectiveRes = Mathf.Max(0f, magicResistance - enemyMagicPenetration);
        float reduction = effectiveRes / (effectiveRes + 100f);
        return Mathf.Max(1, Mathf.RoundToInt(incomingDamage * (1f - reduction)));
    }

    float CalculateCriticalChance(int dex)
    {
        return Mathf.Clamp((dex * 0.35f) / (1f + dex * 0.01f), 0f, 60f);
    }

    public bool RollCritical() => Random.Range(0f, 100f) <= criticalChance;

    public int ApplyCriticalDamage(int damage)
    {
        return RollCritical() ? Mathf.RoundToInt(damage * 1.5f) : damage;
    }

    void Die()
    {
        _isDead.Value = true;
    }

    [ServerRpc]
    public void ServerRequestRespawn()
    {
        if (!isDead) return;
        Respawn();
    }

    void Respawn()
    {
        Vector3 spawnPos = GetRespawnPosition();
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;

        _currentHealth.Value = maxHealth;
        _currentMana.Value   = maxMana;
        _isDead.Value        = false;
    }

    Vector3 GetRespawnPosition()
    {
        string factionTag  = faction == PlayerFaction.Vampire ? "VampireRespawn" : "SlayerRespawn";
        string factionName = faction == PlayerFaction.Vampire ? "SpawnPointVampire" : "SpawnPointSlayer";

        GameObject spawnObj = GameObject.FindWithTag(factionTag);
        if (spawnObj == null) spawnObj = GameObject.Find(factionName);
        if (spawnObj == null) spawnObj = GameObject.FindWithTag("Respawn");
        if (spawnObj == null) spawnObj = GameObject.Find("SpawnPointHealer");

        return spawnObj != null ? spawnObj.transform.position : Vector3.zero;
    }

    [ObserversRpc] void RpcOnPlayerDied() { }
    [ObserversRpc] void RpcOnPlayerRespawned(Vector3 spawnPos) { }

    public void LoadFromSave(int savedLevel, int savedXP, int savedXPToNext, int savedStatPoints,
        int savedStr, int savedDex, int savedInt, int savedEnd)
    {
        if (!IsServerStarted) return;

        level = savedLevel;
        _level.Value = savedLevel;
        _currentXP.Value = savedXP;
        int resolvedXPToNext = savedXPToNext > 0 ? savedXPToNext : xpToNextLevel;
        _xpToNextLevel.Value = resolvedXPToNext;
        xpToNextLevel = resolvedXPToNext;
        _availableStatPoints.Value = savedStatPoints;

        baseStrength = savedStr;
        baseDexterity = savedDex;
        baseIntelligence = savedInt;
        baseEndurance = savedEnd;

        RecalculateStats();

        _currentHealth.Value = maxHealth;
        _currentMana.Value = maxMana;
    }

    public void ApplyClassBaseStats()
    {
        if (!IsServerStarted) return;

        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character == null) return;

        ClassBaseStats.BaseStats stats = ClassBaseStats.GetStats(character);

        baseStrength     = stats.strength;
        baseDexterity    = stats.dexterity;
        baseIntelligence = stats.intelligence;
        baseEndurance    = stats.endurance;

        maxHealth = stats.baseHealth;
        maxMana   = stats.baseMana;

        RecalculateStats();

        _currentHealth.Value = maxHealth;
        _currentMana.Value   = maxMana;

        Debug.Log($"[PlayerStats] Applied class stats for {character.GetClassName()}: " +
                  $"STR={baseStrength} DEX={baseDexterity} INT={baseIntelligence} END={baseEndurance}");
    }
}
