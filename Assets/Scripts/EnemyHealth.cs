using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 50;
    public int currentHealth;

    public Slider healthBar;

    [Header("XP Reward")]
    public int xpReward = 25;

    [Header("Death")]
    public float destroyDelay = 10f;

    private bool isDead = false;

    private EnemyAI enemyAI;
    private Animator animator;
    private LootDropTable lootDropTable;

    void Start()
    {
        currentHealth = maxHealth;

        enemyAI = GetComponent<EnemyAI>();
        animator = GetComponentInChildren<Animator>();
        lootDropTable = GetComponent<LootDropTable>();

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
        if (isDead)
            return;

        isDead = true;

        GiveXPToPlayer();

        if (lootDropTable != null)
            lootDropTable.DropLoot();

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

    void GiveXPToPlayer()
    {
        GameObject playerObject = GameObject.Find("Player");

        if (playerObject == null)
            return;

        PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.GainXP(xpReward);
            Debug.Log("Player gained " + xpReward + " XP.");
        }
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}