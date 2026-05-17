using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Level")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

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

    [Header("Gear Bonuses")]
    public int gearStrength;
    public int gearDexterity;
    public int gearIntelligence;
    public int gearEndurance;
    public int gearArmor;
    public int gearResistance;
    public int gearHealth;
    public int gearMana;

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

        meleeDamageBonus = strength * 2.0f;
        rangedDamageBonus = dexterity * 1.75f;

        magicalSkillPower = intelligence * 2.25f;
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

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
    }

    public void ClearGearBonuses()
    {
        gearStrength = 0;
        gearDexterity = 0;
        gearIntelligence = 0;
        gearEndurance = 0;
        gearArmor = 0;
        gearResistance = 0;
        gearHealth = 0;
        gearMana = 0;
    }

    public void AddGearBonuses(ItemData item)
    {
        if (item == null)
            return;

        gearStrength += item.strengthBonus;
        gearDexterity += item.dexterityBonus;
        gearIntelligence += item.intelligenceBonus;
        gearEndurance += item.enduranceBonus;
        gearArmor += item.armorBonus;
        gearResistance += item.resistanceBonus;
        gearHealth += item.healthBonus;
        gearMana += item.manaBonus;
    }

    float CalculateCriticalChance(int dex)
    {
        float crit = (dex * 0.35f) / (1f + dex * 0.01f);
        return Mathf.Clamp(crit, 0f, 60f);
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

    public float GetBuffEffectiveness(float baseEffect)
    {
        return baseEffect * (1f + buffEffectivenessBonus);
    }

    public float GetBuffDuration(float baseDuration)
    {
        return baseDuration * (1f + buffDurationBonus);
    }

    public bool RollCritical()
    {
        float roll = Random.Range(0f, 100f);
        return roll <= criticalChance;
    }

    public int ApplyCriticalDamage(int damage)
    {
        if (RollCritical())
        {
            Debug.Log("Critical Hit!");
            return Mathf.RoundToInt(damage * 1.5f);
        }

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

        Debug.Log("Player took " + damageTaken + " " + damageType + " damage.");

        if (currentHealth <= 0)
            Die();
    }

    public void TakeDamage(int incomingDamage)
    {
        ReceiveDamage(incomingDamage, DamageType.Melee);
    }

    public int CalculatePhysicalDamageTaken(int incomingDamage)
    {
        float damageReduction = armor / (armor + 100f);
        float reducedDamage = incomingDamage * (1f - damageReduction);

        return Mathf.Max(1, Mathf.RoundToInt(reducedDamage));
    }

    public int CalculateMagicDamageTaken(int incomingDamage, float enemyMagicPenetration)
    {
        float effectiveResistance = Mathf.Max(0f, resistance - enemyMagicPenetration);
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

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player died.");
    }
}