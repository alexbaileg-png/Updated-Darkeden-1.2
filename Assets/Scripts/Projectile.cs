using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Owner")]
    public GameObject attacker;

    [Header("Damage")]
    public int damage = 15;
    public DamageType damageType = DamageType.Magical;
    public bool canCrit = true;

    [Header("Movement")]
    public float speed = 10f;
    public float lifetime = 4f;
    public float hitRadius = 1.5f;
    public float turnSpeed = 25f;

    [Header("Effects")]
    public GameObject impactEffectPrefab;

    private EnemyHealth targetEnemy;
    private bool hasHit = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetTarget(EnemyHealth target)
    {
        if (target != null && !target.IsDead())
            targetEnemy = target;
    }

    public void SetAttacker(GameObject newAttacker)
    {
        attacker = newAttacker;
    }

    void Update()
    {
        if (hasHit)
            return;

        if (targetEnemy != null && targetEnemy.IsDead())
            targetEnemy = null;

        if (targetEnemy != null)
            MoveTowardTarget();
        else
            transform.position += transform.forward * speed * Time.deltaTime;

        CheckNearbyHits();
    }

    void MoveTowardTarget()
    {
        if (targetEnemy == null || targetEnemy.IsDead())
            return;

        Vector3 targetPosition = targetEnemy.transform.position + Vector3.up * 0.8f;
        Vector3 direction = targetPosition - transform.position;

        if (direction.magnitude <= hitRadius)
        {
            HitEnemy(targetEnemy);
            return;
        }

        // Track in full 3D so projectile doesn't orbit at ground level
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * 100f * Time.deltaTime
            );
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void CheckNearbyHits()
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead())
                continue;

            Vector3 enemyPosition = enemy.transform.position + Vector3.up * 0.8f;
            float distance = Vector3.Distance(transform.position, enemyPosition);

            if (distance <= hitRadius)
            {
                HitEnemy(enemy);
                return;
            }
        }
    }

    void HitEnemy(EnemyHealth enemy)
    {
        if (hasHit || enemy == null || enemy.IsDead())
            return;

        hasHit = true;

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, enemy.transform.position + Vector3.up * 0.8f, Quaternion.identity);

        // Route damage through the attacker's NetworkPlayerController ServerRpc
        if (attacker != null)
        {
            NetworkPlayerController netPlayer = attacker.GetComponent<NetworkPlayerController>();
            if (netPlayer != null)
            {
                netPlayer.ServerApplyProjectileDamage(enemy, damage, damageType, canCrit);
                Destroy(gameObject);
                return;
            }
        }

        // Fallback for non-networked use
        enemy.ReceiveDamage(damage, damageType);

        Destroy(gameObject);
    }
}