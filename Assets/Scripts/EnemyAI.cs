using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float chaseRange = 30f;
    public float stopDistance = 1.8f;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 2.5f;
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

        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (player == null)
        {
            SetMoving(false);
            return;
        }

        if (playerStats == null)
            playerStats = player.GetComponent<PlayerStats>();

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
            Move(direction.normalized);
        else
            SetMoving(false);
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
            float distance = Vector3.Distance(transform.position, player.position);

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
}