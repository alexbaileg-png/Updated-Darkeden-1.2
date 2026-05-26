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

    public bool IsDead()
    {
        return isDead;
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

        SpawnDamageText(finalDamage);

        UpdateHealthBar();

        if (currentHealth <= 0)
            Die();
    }

    void SpawnDamageText(int damage)
    {
        if (CombatTextSpawner.Instance == null)
            return;

        Vector3 textPosition = GetEnemyVisualCenter();
        textPosition.y += 1.2f;

        CombatTextSpawner.Instance.SpawnText(textPosition, damage.ToString());
    }

    Vector3 GetEnemyVisualCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
            return transform.position;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.center;
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

        DisableAllColliders();

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

        StartCoroutine(DestroyAfterDeath());
    }

    void DisableAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.enabled = false;
    }

    void GiveXPToPlayer()
    {
        GameObject playerObject = GameObject.Find("Player");

        if (playerObject == null)
            return;

        PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();

        if (playerStats != null)
            playerStats.GainXP(xpReward);
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}