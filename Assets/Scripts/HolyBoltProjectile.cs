using UnityEngine;

public class HolyBoltProjectile : MonoBehaviour
{
    public float       speed     = 18f;
    public float       range     = 20f;
    public float       hitRadius = 1.2f;
    public int         damage;
    public DamageType  damageType = DamageType.Magical;
    public PlayerStats caster;

    private float _traveled;
    private bool  _hasHit;

    void Update()
    {
        if (_hasHit) return;

        float step = speed * Time.deltaTime;
        transform.position += transform.forward * step;
        _traveled += step;

        if (_traveled >= range)
        {
            Destroy(gameObject);
            return;
        }

        // Compare only XZ distance so terrain height differences don't interfere
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead()) continue;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            float xzDist = new Vector2(toEnemy.x, toEnemy.z).magnitude;
            if (xzDist <= hitRadius)
            {
                _hasHit = true;
                enemy.ReceiveDamage(damage, damageType, caster);
                Destroy(gameObject);
                return;
            }
        }
    }
}
