using FishNet.Object;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{
    public int damage = 10;
    public float attackRange = 3f;
    public float attackAngle = 90f;
    public float attackCooldown = 0.5f;

    private float _nextAttackTime = 0f;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackCooldown;
            ServerAttack();
        }
    }

    [ServerRpc]
    void ServerAttack()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && stats.isDead) return;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead()) continue;

            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            directionToEnemy.y = 0f;

            float distance = directionToEnemy.magnitude;
            if (distance > attackRange) continue;

            float angle = Vector3.Angle(transform.forward, directionToEnemy);
            if (angle > attackAngle / 2f) continue;

            int finalDamage = stats != null ? stats.GetMeleeDamage(damage) : damage;
            finalDamage = stats != null ? stats.ApplyCriticalDamage(finalDamage) : finalDamage;

            enemy.ReceiveDamage(finalDamage, DamageType.Melee, stats);
            return;
        }
    }
}
