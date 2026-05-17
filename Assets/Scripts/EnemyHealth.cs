using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 50;
    public int currentHealth;

    public Slider healthBar;

    [Header("Death")]
    public float destroyDelay = 10f;

    private bool isDead = false;

    private EnemyAI enemyAI;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;

        enemyAI = GetComponent<EnemyAI>();
        animator = GetComponentInChildren<Animator>();

        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        ReceiveDamage(damage, DamageType.Magical);
    }

    public void ReceiveDamage(int finalDamage, DamageType damageType)
    {
        if (isDead)
            return;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthBar()
    {
        if (healthBar == null)
            return;

        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
    }

    void Die()
    {
        isDead = true;

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveSpeed", 0f);
            animator.ResetTrigger("Die");
            animator.SetTrigger("Die");
        }

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        Collider col = GetComponent<Collider>();

        if (col != null)
            col.enabled = false;

        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}