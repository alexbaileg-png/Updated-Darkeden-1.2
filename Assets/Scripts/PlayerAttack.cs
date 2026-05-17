using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public int damage = 10;
    public float attackRange = 3f;
    public float attackAngle = 90f;
    public float attackCooldown = 0.5f;

    private float nextAttackTime = 0f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            directionToEnemy.y = 0f;

            float distance = directionToEnemy.magnitude;

            if (distance > attackRange)
                continue;

            float angle = Vector3.Angle(transform.forward, directionToEnemy);

            if (angle <= attackAngle / 2f)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Hit enemy: " + enemy.gameObject.name);
                return;
            }
        }

        Debug.Log("Attack missed.");
    }
}