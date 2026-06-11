using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
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
    private bool isAttacking = false;
    private Transform _target;
    private PlayerStats _targetStats;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (modelAnimator == null && modelTransform != null)
            modelAnimator = modelTransform.GetComponent<Animator>();

        if (modelAnimator == null)
            modelAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // AI only runs on the server
        if (!IsServerStarted) return;

        RefreshTarget();

        if (_target == null)
        {
            SetMoving(false);
            return;
        }

        Vector3 direction = _target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance > chaseRange)
        {
            SetMoving(false);
            return;
        }

        FaceTarget(direction);

        if (distance <= attackRange)
        {
            SetMoving(false);
            TryAttack();
            return;
        }

        if (!isAttacking)
            Move(direction.normalized);
        else
            SetMoving(false);
    }

    // Finds the closest living player each frame
    void RefreshTarget()
    {
        float closestDist = chaseRange;
        Transform closest = null;

        foreach (PlayerStats ps in FindObjectsOfType<PlayerStats>())
        {
            float dist = Vector3.Distance(transform.position, ps.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = ps.transform;
                _targetStats = ps;
            }
        }

        _target = closest;
    }

    void Move(Vector3 direction)
    {
        transform.position += direction * moveSpeed * Time.deltaTime;
        SetMoving(true);
    }

    void TryAttack()
    {
        if (isAttacking) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        SetMoving(false);

        TriggerAttackAnimOnClients();

        yield return new WaitForSeconds(attackHitDelay);

        if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.position);

            if (distance <= attackRange)
            {
                // Try NetworkPlayerController first (networked player), fall back to PlayerStats
                NetworkPlayerController netPlayer = _target.GetComponent<NetworkPlayerController>();
                if (netPlayer != null)
                {
                    netPlayer.ServerTakeDamage(attackDamage, DamageType.Melee);
                }
                else if (_targetStats != null)
                {
                    if (CombatManager.Instance != null)
                    {
                        DamageRequest request = new DamageRequest(
                            gameObject, _target.gameObject, attackDamage, DamageType.Melee, false);
                        CombatManager.Instance.ApplyDamage(request);
                    }
                    else
                    {
                        _targetStats.ReceiveDamage(attackDamage, DamageType.Melee);
                    }
                }
            }
        }

        isAttacking = false;
    }

    [ObserversRpc]
    void TriggerAttackAnimOnClients()
    {
        if (modelAnimator == null) return;
        modelAnimator.ResetTrigger("Attack");
        modelAnimator.SetTrigger("Attack");
    }

    void FaceTarget(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.01f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);

        if (modelTransform != null)
            modelTransform.rotation = lookRotation;
        else
            transform.rotation = lookRotation;
    }

    void SetMoving(bool isMoving)
    {
        if (modelAnimator == null) return;
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
