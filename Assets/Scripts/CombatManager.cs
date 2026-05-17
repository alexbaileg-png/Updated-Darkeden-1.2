using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ApplyDamage(DamageRequest request)
    {
        if (request == null || request.target == null)
            return;

        IDamageable damageable = request.target.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = request.target.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            Debug.LogWarning("Target has no IDamageable: " + request.target.name);
            return;
        }

        int finalDamage = CalculateFinalDamage(request);
        damageable.ReceiveDamage(finalDamage, request.damageType);
    }

    int CalculateFinalDamage(DamageRequest request)
    {
        int damage = request.baseDamage;

        if (request.attacker != null)
        {
            PlayerStats attackerStats = request.attacker.GetComponent<PlayerStats>();

            if (attackerStats != null)
            {
                if (request.damageType == DamageType.Melee)
                    damage = attackerStats.GetMeleeDamage(request.baseDamage);

                if (request.damageType == DamageType.Ranged)
                    damage = attackerStats.GetRangedDamage(request.baseDamage);

                if (request.damageType == DamageType.Magical)
                    damage = attackerStats.GetMagicalSkillDamage(request.baseDamage);

                if (request.canCrit)
                    damage = attackerStats.ApplyCriticalDamage(damage);
            }
        }

        return Mathf.Max(1, damage);
    }
}