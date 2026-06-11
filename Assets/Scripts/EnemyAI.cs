using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // ── Static player registry — populated by PlayerStats.OnStartServer ───────
    private static readonly List<PlayerStats> _registeredPlayers = new List<PlayerStats>();

    public static void RegisterPlayer(PlayerStats ps)
    {
        if (!_registeredPlayers.Contains(ps)) _registeredPlayers.Add(ps);
    }

    public static void UnregisterPlayer(PlayerStats ps)
    {
        _registeredPlayers.Remove(ps);
    }

    public static PlayerStats GetNearestPlayer(Vector3 position)
    {
        PlayerStats nearest = null;
        float bestDist = float.MaxValue;
        foreach (PlayerStats ps in _registeredPlayers)
        {
            if (ps == null || ps.isDead) continue;
            float dist = Vector3.Distance(position, ps.transform.position);
            if (dist < bestDist) { bestDist = dist; nearest = ps; }
        }
        return nearest;
    }

    // ─────────────────────────────────────────────────────────────────────────

    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float chaseRange = 30f;
    public float stopDistance = 2.0f;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 2.8f;
    public float attackCooldown = 1.25f;
    public float attackHitDelay = 0.25f;

    [Header("Visual Model")]
    public Transform modelTransform;
    public Animator modelAnimator;

    private float nextAttackTime = 0f;
    private PlayerStats playerStats;
    private bool isAttacking = false;

    void Start()
    {
        if (modelAnimator == null && modelTransform != null)
            modelAnimator = modelTransform.GetComponent<Animator>();
    }

    void Update()
    {
        // Use the registry to always target the nearest living player
        PlayerStats nearest = GetNearestPlayer(transform.position);
        if (nearest != null)
        {
            player      = nearest.transform;
            playerStats = nearest;
        }

        if (player == null)
        {
            SetMoving(false);
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance > chaseRange)
        {
            SetMoving(false);
            return;
        }

        FacePlayer(direction);

        if (distance <= attackRange)
        {
            SetMoving(false);
            TryAttack();
            return;
        }

        if (distance > stopDistance && !isAttacking)
        {
            Move(direction.normalized);
        }
        else
        {
            SetMoving(false);
        }
    }

    void Move(Vector3 direction)
    {
        transform.position += direction * moveSpeed * Time.deltaTime;
        SetMoving(true);
    }

    void TryAttack()
    {
        if (isAttacking)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        SetMoving(false);

        if (modelAnimator != null)
        {
            modelAnimator.ResetTrigger("Attack");
            modelAnimator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(attackHitDelay);

        if (player != null)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= attackRange)
            {
                if (CombatManager.Instance != null)
                {
                    DamageRequest request = new DamageRequest(
                        gameObject,
                        player.gameObject,
                        attackDamage,
                        DamageType.Melee,
                        false
                    );

                    CombatManager.Instance.ApplyDamage(request);
                }
                else if (playerStats != null)
                {
                    playerStats.ReceiveDamage(attackDamage, DamageType.Melee);
                }
            }
        }

        isAttacking = false;
    }

    void FacePlayer(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);

        if (modelTransform != null)
            modelTransform.rotation = lookRotation;
        else
            transform.rotation = lookRotation;
    }

    void SetMoving(bool isMoving)
    {
        if (modelAnimator == null)
            return;

        modelAnimator.SetBool("IsMoving", isMoving);
        modelAnimator.SetFloat("MoveSpeed", isMoving ? moveSpeed : 0f);
    }

    private float _baseChaseRange = -1f;

    public void ApplyFogDebuff(float detectionMultiplier)
    {
        if (_baseChaseRange < 0f) _baseChaseRange = chaseRange;
        chaseRange = _baseChaseRange * detectionMultiplier;
    }

    public void RemoveFogDebuff()
    {
        if (_baseChaseRange >= 0f)
        {
            chaseRange = _baseChaseRange;
            _baseChaseRange = -1f;
        }
    }
}