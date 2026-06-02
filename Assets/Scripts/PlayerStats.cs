using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Level")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Assignable Stat Points")]
    public int availableStatPoints = 0;
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
    public int gearStrength;
    public int gearDexterity;
    public int gearIntelligence;
    public int gearEndurance;

    [Header("Gear Resource Bonuses")]
    public int gearHealth;
    public int gearMana;

    [Header("Gear Offensive Bonuses")]
    public int gearMeleeDamage;
    public int gearRangedDamage;
    public int gearMagicDamage;

    [Header("Gear Defensive Bonuses")]
    public int gearArmor;
    public int gearResistance;
    public int gearMeleeResistance;
    public int gearMagicResistance;
    public int gearAllResistance;

    [Header("Resources")]
    public int maxHealth;
    public int currentHealth;
    public int maxMana;
    public int currentMana;

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

    private bool isDead = false;

    void Start()
    {
        RecalculateStats();
        currentHealth = maxHealth;
        currentMana = maxMana;
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

        maxHealth = 100 + (strength * 3) + (endurance * 12) + gearHealth;
        maxMana = 50 + (intelligence * 8) + gearMana;

        armor = 0f;
        armor += strength * 0.25f;
        armor += dexterity * 0.15f;
        armor += endurance * 1.25f;
        armor += gearArmor;

        resistance = 0f;
        resistance += intelligence * 0.75f;
        resistance += endurance * 0.35f;
        resistance += gearResistance;

        meleeResistance = armor + gearMeleeResistance + gearAllResistance;
        magicResistance = resistance + gearMagicResistance + gearAllResistance;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
    }

    public void ClearGearBonuses()
    {
        gearStrength = 0;
        gearDexterity = 0;
        gearIntelligence = 0;
        gearEndurance = 0;

        gearHealth = 0;
        gearMana = 0;

        gearMeleeDamage = 0;
        gearRangedDamage = 0;
        gearMagicDamage = 0;

        gearArmor = 0;
        gearResistance = 0;
        gearMeleeResistance = 0;
        gearMagicResistance = 0;
        gearAllResistance = 0;
    }

    public void AddGearBonuses(ItemData item)
    {
        if (item == null)
            return;

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
        if (item == null)
            return;

        if (!item.hasCrystalStat)
            return;

        int value = item.crystalCurrentValue;

        switch (item.crystalStat)
        {
            case ItemBonusStat.Strength:
                gearStrength += value;
                break;

            case ItemBonusStat.Dexterity:
                gearDexterity += value;
                break;

            case ItemBonusStat.Intelligence:
                gearIntelligence += value;
                break;

            case ItemBonusStat.Endurance:
                gearEndurance += value;
                break;

            case ItemBonusStat.Health:
                gearHealth += value;
                break;

            case ItemBonusStat.Mana:
                gearMana += value;
                break;

            case ItemBonusStat.MeleeDamage:
                gearMeleeDamage += value;
                break;

            case ItemBonusStat.RangedDamage:
                gearRangedDamage += value;
                break;

            case ItemBonusStat.MagicDamage:
                gearMagicDamage += value;
                break;

            case ItemBonusStat.Armor:
                gearArmor += value;
                break;

            case ItemBonusStat.Resistance:
                gearResistance += value;
                break;

            case ItemBonusStat.MeleeResistance:
                gearMeleeResistance += value;
                break;

            case ItemBonusStat.MagicResistance:
                gearMagicResistance += value;
                break;

            case ItemBonusStat.AllResistance:
                gearAllResistance += value;
                break;
        }
    }

    public int GetMeleeDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage + meleeDamageBonus);
    }

    public int GetRangedDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage + rangedDamageBonus);
    }

    public int GetMagicalSkillDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage + magicalSkillPower);
    }

    public int GetMagicalHealing(int baseHealing)
    {
        return Mathf.RoundToInt(baseHealing + magicalSkillPower);
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (CombatTextSpawner.Instance != null)
            CombatTextSpawner.Instance.SpawnText(transform.position + Vector3.up * 1.8f, "+" + amount);
    }

    public void GainXP(int amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.35f);

        baseStrength += 2;
        baseDexterity += 2;
        baseIntelligence += 2;
        baseEndurance += 2;

        availableStatPoints += statPointsPerLevel;

        RecalculateStats();

        currentHealth = maxHealth;
        currentMana = maxMana;

        PlayerSkillManager skillManager = GetComponent<PlayerSkillManager>();

        if (skillManager != null)
            skillManager.GainSkillPoint();
    }

    public bool SpendStatPoint(string statName)
    {
        if (availableStatPoints <= 0)
            return false;

        if (statName == "Strength")
            baseStrength++;
        else if (statName == "Dexterity")
            baseDexterity++;
        else if (statName == "Intelligence")
            baseIntelligence++;
        else if (statName == "Endurance")
            baseEndurance++;
        else
            return false;

        availableStatPoints--;
        RecalculateStats();

        return true;
    }

    float CalculateCriticalChance(int dex)
    {
        float crit = (dex * 0.35f) / (1f + dex * 0.01f);
        return Mathf.Clamp(crit, 0f, 60f);
    }

    public bool RollCritical()
    {
        float roll = Random.Range(0f, 100f);
        return roll <= criticalChance;
    }

    public int ApplyCriticalDamage(int damage)
    {
        if (RollCritical())
            return Mathf.RoundToInt(damage * 1.5f);

        return damage;
    }

    public void ReceiveDamage(int finalDamage, DamageType damageType)
    {
        if (isDead)
            return;

        int damageTaken = finalDamage;

        if (damageType == DamageType.Melee || damageType == DamageType.Ranged)
            damageTaken = CalculatePhysicalDamageTaken(finalDamage);

        if (damageType == DamageType.Magical)
            damageTaken = CalculateMagicDamageTaken(finalDamage, 0f);

        currentHealth -= damageTaken;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void TakeDamage(int incomingDamage)
    {
        ReceiveDamage(incomingDamage, DamageType.Melee);
    }

    public int CalculatePhysicalDamageTaken(int incomingDamage)
    {
        float damageReduction = meleeResistance / (meleeResistance + 100f);
        float reducedDamage = incomingDamage * (1f - damageReduction);

        return Mathf.Max(1, Mathf.RoundToInt(reducedDamage));
    }

    public int CalculateMagicDamageTaken(int incomingDamage, float enemyMagicPenetration)
    {
        float effectiveResistance = Mathf.Max(0f, magicResistance - enemyMagicPenetration);
        float damageReduction = effectiveResistance / (effectiveResistance + 100f);
        float reducedDamage = incomingDamage * (1f - damageReduction);

        return Mathf.Max(1, Mathf.RoundToInt(reducedDamage));
    }

    public bool SpendMana(int amount)
    {
        if (currentMana < amount)
            return false;

        currentMana -= amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        return true;
    }

    public void RestoreMana(int amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player died.");
    }
}