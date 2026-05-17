using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject attacker;

    public int damage = 15;
    public float speed = 10f;
    public float lifetime = 4f;
    public float hitRadius = 1.25f;
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

        if (targetEnemy != null)
        {
            Vector3 targetPosition = targetEnemy.transform.position + Vector3.up * 0.5f;
            Vector3 direction = targetPosition - transform.position;

            if (direction.magnitude <= hitRadius)
            {
                HitEnemy(targetEnemy);
                return;
            }

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * 100f * Time.deltaTime
                );
            }
        }

        transform.position += transform.forward * speed * Time.deltaTime;

        CheckNearbyHits();
    }

    void CheckNearbyHits()
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            Vector3 enemyPosition = enemy.transform.position + Vector3.up * 0.5f;
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
        if (hasHit)
            return;

        hasHit = true;

        if (impactEffectPrefab != null)
        {
            Instantiate(
                impactEffectPrefab,
                enemy.transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );
        }

        if (CombatManager.Instance != null)
        {
            DamageRequest request = new DamageRequest(
                attacker != null ? attacker : gameObject,
                enemy.gameObject,
                damage,
                DamageType.Magical,
                true
            );

            CombatManager.Instance.ApplyDamage(request);
        }
        else
        {
            enemy.ReceiveDamage(damage, DamageType.Magical);
        }

        Destroy(gameObject);
    }
}