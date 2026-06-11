using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float stopDistance = 0.35f;

    [Header("Visual")]
    [SerializeField] public Transform modelTransform;
    [SerializeField] public Animator modelAnimator;

    // Server-side movement state
    private Vector3 _targetPosition;
    private bool _moving = false;

    // Synced to all clients so remote players animate correctly (FishNet v4 style)
    private readonly SyncVar<bool> _syncMoving = new SyncVar<bool>();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _syncMoving.OnChange += OnMovingChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _syncMoving.OnChange -= OnMovingChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _targetPosition = transform.position;

        if (modelTransform == null)
            modelTransform = GetComponentInChildren<Animator>()?.transform;

        if (modelAnimator == null && modelTransform != null)
            modelAnimator = modelTransform.GetComponent<Animator>();

        if (modelAnimator == null)
            modelAnimator = GetComponentInChildren<Animator>();

        // NetworkPlayerController owns movement on the network prefab — disable the
        // standalone PlayerMovement if it was left on the prefab.
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
            pm.enabled = false;

        if (!IsOwner)
            return;

        // Point the scene camera at this client's own player.
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
                cam.target = modelTransform != null ? modelTransform : transform;
        }
    }

    void Update()
    {
        if (IsOwner)
            HandleInput();

        if (IsServerStarted)
            MoveOnServer();

        if (IsClientStarted)
            UpdateAnimation(_syncMoving.Value);
    }

    // ── Input (owner only) ────────────────────────────────────────────────────

    void HandleInput()
    {
        if (_isDead) return;
        if (MenuTabController.IsMenuOpen)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Use player's Y so the plane matches the actual floor level
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

                if (groundPlane.Raycast(ray, out float dist))
            {
                ServerSetMoveTarget(ray.GetPoint(dist));
            }
            else if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                ServerSetMoveTarget(hit.point);
            }
        }
    }

    // ── Server-authoritative movement ─────────────────────────────────────────

    [ServerRpc]
    void ServerSetMoveTarget(Vector3 target)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && stats.isDead) return;

        _targetPosition = target;
        _moving = true;
    }

    void MoveOnServer()
    {
        if (!_moving) return;

        Vector3 dir = _targetPosition - transform.position;
        dir.y = 0f;

        if (dir.magnitude > stopDistance)
        {
            Vector3 moveDir = dir.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
            RotateVisual(moveDir);

            if (!_syncMoving.Value) _syncMoving.Value = true;
        }
        else
        {
            _moving = false;
            _targetPosition = transform.position;
            if (_syncMoving.Value) _syncMoving.Value = false;
        }
    }

    public void RotateVisualPublic(Vector3 direction) => RotateVisual(direction);

    /// <summary>Called after a skill teleports the player so movement doesn't freeze.</summary>
    public void ResetMovementTarget(Vector3 newPosition)
    {
        _targetPosition = newPosition;
        _moving = false;
        if (_syncMoving.Value) _syncMoving.Value = false;
    }

    void RotateVisual(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f) return;

        // Rotate the root so NetworkTransform syncs it to all clients.
        // The model child inherits rotation from the root.
        transform.rotation = Quaternion.LookRotation(direction);
    }

    // ── Animation sync ────────────────────────────────────────────────────────

    void OnMovingChanged(bool prev, bool next, bool asServer) { }

    void UpdateAnimation(bool isMoving)
    {
        if (modelAnimator == null) return;

        // Don't let locomotion override an action animation mid-cast
        SwordmasterSkillCaster caster = GetComponent<SwordmasterSkillCaster>();
        if (caster != null && caster.IsCasting) return;

        modelAnimator.SetBool("IsMoving", isMoving);
        modelAnimator.SetFloat("MoveSpeed", isMoving ? moveSpeed : 0f);
    }

    // ── Server-authoritative damage (called by EnemyAI / skills) ─────────────

    [ServerRpc(RequireOwnership = false)]
    public void ServerTakeDamage(int damage, DamageType damageType)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.ReceiveDamage(damage, damageType);
    }

    // ── Death / Respawn ───────────────────────────────────────────────────────

    private bool _isDead = false;

    public void OnPlayerDied()
    {
        _isDead = true;

        if (modelAnimator != null)
            modelAnimator.SetTrigger("Death");

        if (IsOwner)
        {
            DeathScreenUI deathScreen = FindObjectOfType<DeathScreenUI>(true);
            if (deathScreen != null) deathScreen.Show();
        }
    }

    public void OnPlayerRespawned(Vector3 spawnPos)
    {
        _isDead = false;
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;

        if (modelAnimator != null)
        {
            modelAnimator.ResetTrigger("Death");
            modelAnimator.SetBool("IsMoving", false);
            // Force the animator back to its default state
            modelAnimator.Play("Idle", 0, 0f);
        }

        if (IsOwner)
        {
            DeathScreenUI deathScreen = FindObjectOfType<DeathScreenUI>(true);
            if (deathScreen != null) deathScreen.Hide();
        }
    }

    // Called by Projectile on hit — routes damage to server
    [ServerRpc]
    public void ServerApplyProjectileDamage(EnemyHealth enemy, int damage, DamageType damageType, bool canCrit)
    {
        if (enemy == null || enemy.IsDead()) return;

        PlayerStats stats = GetComponent<PlayerStats>();
        int finalDamage = damage;
        if (stats != null)
        {
            finalDamage = stats.GetMagicalSkillDamage(damage);
            if (canCrit) finalDamage = stats.ApplyCriticalDamage(finalDamage);
        }

        enemy.ReceiveDamage(finalDamage, damageType, stats);
    }
}
