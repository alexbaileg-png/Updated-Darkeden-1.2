using UnityEngine;

public class DamageRequest
{
    public GameObject attacker;
    public GameObject target;
    public int baseDamage;
    public DamageType damageType;
    public bool canCrit;

    public DamageRequest(GameObject attacker, GameObject target, int baseDamage, DamageType damageType, bool canCrit)
    {
        this.attacker = attacker;
        this.target = target;
        this.baseDamage = baseDamage;
        this.damageType = damageType;
        this.canCrit = canCrit;
    }
}