using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour, IDamageable
{
    public int maxHealth = 50;

    [Header("XP Reward")]
    public int xpReward = 25;

    [Header("Death")]
    public float destroyDelay = 10f;

    public Slider healthBar;

    private readonly SyncVar<int>  _currentHealth = new SyncVar<int>();
    private readonly SyncVar<bool> _isDead        = new SyncVar<bool>();

    public int  currentHealth => _currentHealth.Value;
    public bool isDead        => _isDead.Value;

    private EnemyAI        _enemyAI;
    private Animator       _animator;
    private LootDropTable  _lootDropTable;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentHealth.Value = maxHealth;
        _isDead.Value        = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _currentHealth.OnChange += OnHealthChanged;
        _isDead.OnChange        += OnDeadChanged;

        _enemyAI      = GetComponent<EnemyAI>();
        _animator     = GetComponentInChildren<Animator>();
        _lootDropTable = GetComponent<LootDropTable>();

        UpdateHealthBar(_currentHealth.Value);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _currentHealth.OnChange -= OnHealthChanged;
        _isDead.OnChange        -= OnDeadChanged;
    }

    void OnHealthChanged(int prev, int next, bool asServer) => UpdateHealthBar(next);

    void OnDeadChanged(bool prev, bool next, bool asServer)
    {
        if (next) PlayDeathEffects();
    }

    public bool IsDead() => _isDead.Value;

    // Called by melee/skill scripts that pass the attacker's stats for XP credit
    public void ReceiveDamage(int finalDamage, DamageType damageType, PlayerStats attacker)
    {
        if (!IsServerStarted) return;
        if (_isDead.Value)    return;

        _currentHealth.Value = Mathf.Clamp(_currentHealth.Value - finalDamage, 0, maxHealth);
        SpawnDamageTextRpc(finalDamage, GetEnemyVisualCenter());

        if (_currentHealth.Value <= 0)
            Die(attacker);
    }

    // IDamageable fallback — no attacker credit
    public void TakeDamage(int damage)
    {
        ReceiveDamage(damage, DamageType.Melee, null);
    }

    // IDamageable overload used by some older callers
    public void ReceiveDamage(int finalDamage, DamageType damageType)
    {
        ReceiveDamage(finalDamage, damageType, null);
    }

    void Die(PlayerStats attacker)
    {
        if (_isDead.Value) return;
        _isDead.Value = true;

        // Give XP to the player who landed the kill
        if (attacker != null)
            attacker.GainXP(xpReward);
        else
        {
            // Fallback: give XP to nearest registered player
            PlayerStats nearest = EnemyAI.GetNearestPlayer(transform.position);
            if (nearest != null) nearest.GainXP(xpReward);
        }

        if (_lootDropTable != null)
            _lootDropTable.DropLoot();

        if (gameObject.activeInHierarchy)
            StartCoroutine(DespawnAfterDeath());
    }

    IEnumerator DespawnAfterDeath()
    {
        yield return new WaitForSeconds(destroyDelay);
        if (IsServerStarted)
            ServerManager.Despawn(NetworkObject);
    }

    void PlayDeathEffects()
    {
        DisableAllColliders();

        if (_enemyAI != null) _enemyAI.enabled = false;

        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
            _animator.SetFloat("MoveSpeed", 0f);
            _animator.ResetTrigger("Die");
            _animator.SetTrigger("Die");
        }

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);
    }

    void DisableAllColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    [ObserversRpc]
    void SpawnDamageTextRpc(int damage, Vector3 position)
    {
        if (CombatTextSpawner.Instance != null)
            CombatTextSpawner.Instance.SpawnText(position + Vector3.up * 1.2f, damage.ToString());
    }

    void UpdateHealthBar(int health)
    {
        if (healthBar == null) return;
        healthBar.maxValue = maxHealth;
        healthBar.value    = health;
    }

    Vector3 GetEnemyVisualCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.center;
    }
}
