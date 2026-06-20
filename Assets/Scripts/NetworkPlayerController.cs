using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.EventSystems;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed    = 6f;
    public float stopDistance = 0.35f;

    [Header("Terrain")]
    [Tooltip("Layers considered walkable ground (terrain, floors, etc.)")]
    public LayerMask groundLayers = ~0;
    [Tooltip("Maximum slope angle the player can walk up (degrees)")]
    public float slopeLimit = 45f;
    [Tooltip("How high above the player to start the downward ground-snap raycast")]
    public float groundSnapRayHeight = 3f;
    [Tooltip("How far below the player to search for ground when snapping")]
    public float groundSnapRayDepth  = 6f;
    [Tooltip("Smooth speed for Y height changes — higher = snappier")]
    public float heightSmoothSpeed   = 20f;

    [Header("Visual")]
    public Transform modelTransform;
    public Animator modelAnimator;

    // Server-side movement state
    private Vector3 _targetPosition;
    private bool _moving = false;

    // Synced to all clients so remote players animate correctly
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

        // Always take animator/transform from PlayerMovement — it has the correct child reference per prefab
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            modelTransform = pm.modelTransform;
            modelAnimator  = pm.modelAnimator;
        }
        if (modelAnimator == null && modelTransform != null)
            modelAnimator = modelTransform.GetComponent<Animator>();
        // Skip root Animator — it may use a different controller (e.g. NewCharacter) that lacks MoveSpeed
        if (modelAnimator == null || modelAnimator.gameObject == gameObject)
        {
            foreach (Animator a in GetComponentsInChildren<Animator>())
            {
                if (a.gameObject != gameObject) { modelAnimator = a; break; }
            }
        }

        string ctrlName = modelAnimator != null && modelAnimator.runtimeAnimatorController != null
            ? modelAnimator.runtimeAnimatorController.name : "none";
        Debug.Log($"[NPC] OnStartClient — animator={modelAnimator?.name ?? "NULL"} ctrl={ctrlName} isOwner={IsOwner}");

        // Disable standalone PlayerMovement — NetworkPlayerController owns movement.
        if (pm != null)
            pm.enabled = false;

        if (!IsOwner)
            return;

        // Point the scene camera at this client's own player.
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
                cam.target = transform;
        }
    }

    void Update()
    {
        if (IsOwner)
            HandleInput();

        if (IsServerStarted)
            MoveOnServer();

        // Drive animation every frame on all clients (including host where IsServerStarted is also true)
        if (IsClientStarted)
            SetMoveAnimation(_syncMoving.Value);
    }

    // ── Input (owner only) ────────────────────────────────────────────────────

    void HandleInput()
    {
        if (MenuTabController.IsMenuOpen)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Raycast against actual geometry so terrain clicks land at the correct height
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayers))
            {
                // Only allow clicking on walkable slopes
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle <= slopeLimit)
                    ServerSetMoveTarget(hit.point);
            }
        }
    }

    // ── Server-authoritative movement ─────────────────────────────────────────

    [ServerRpc]
    void ServerSetMoveTarget(Vector3 target)
    {
        _targetPosition = target;
        _moving = true;
    }

    [ServerRpc]
    public void ServerStopMovement()
    {
        _moving = false;
        _targetPosition = transform.position;
        if (_syncMoving.Value) { _syncMoving.Value = false; SetMoveAnimation(false); }
    }

    void MoveOnServer()
    {
        if (!_moving)
        {
            if (_syncMoving.Value) { _syncMoving.Value = false; SetMoveAnimation(false); }
            return;
        }

        Vector3 dir = _targetPosition - transform.position;
        dir.y = 0f;

        if (dir.magnitude > stopDistance)
        {
            Vector3 moveDir = dir.normalized;
            Vector3 nextPos = transform.position + moveDir * moveSpeed * Time.deltaTime;

            // Snap Y to terrain — cast downward from above the next XZ position
            Vector3 rayOrigin = new Vector3(nextPos.x, nextPos.y + groundSnapRayHeight, nextPos.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                                groundSnapRayHeight + groundSnapRayDepth, groundLayers))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle <= slopeLimit)
                {
                    // Smooth Y transition so the player glides up/down slopes
                    nextPos.y = Mathf.Lerp(transform.position.y, hit.point.y,
                                           heightSmoothSpeed * Time.deltaTime);
                }
                else
                {
                    // Slope too steep — stop movement
                    _moving = false;
                    _targetPosition = transform.position;
                    if (_syncMoving.Value) { _syncMoving.Value = false; SetMoveAnimation(false); }
                    return;
                }
            }

            transform.position = nextPos;
            RotateVisual(moveDir);

            if (!_syncMoving.Value) { _syncMoving.Value = true; SetMoveAnimation(true); }
        }
        else
        {
            _moving = false;
            _targetPosition = transform.position;
            if (_syncMoving.Value) { _syncMoving.Value = false; SetMoveAnimation(false); }
        }
    }

    public void RotateVisualPublic(Vector3 direction) => RotateVisual(direction);

    void RotateVisual(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(direction);
        if (modelTransform != null)
            modelTransform.rotation = rot;
        else
            transform.rotation = rot;
    }

    // ── Animation sync ────────────────────────────────────────────────────────

    void OnMovingChanged(bool prev, bool next, bool asServer)
    {
        if (asServer) return; // clients handle visuals
        SetMoveAnimation(next);
    }

    void SetMoveAnimation(bool moving)
    {
        if (modelAnimator == null)
        {
            Debug.LogWarning("[NPC] SetMoveAnimation called but modelAnimator is null");
            return;
        }
        modelAnimator.SetBool("IsMoving", moving);
        modelAnimator.SetFloat("MoveSpeed", moving ? moveSpeed : 0f);
    }

    // ── Server-authoritative damage (called by EnemyAI / skills) ─────────────

    [ServerRpc(RequireOwnership = false)]
    public void ServerTakeDamage(int damage, DamageType damageType)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.ReceiveDamage(damage, damageType);
    }

    // ── Projectile damage (called by Projectile.cs on hit) ───────────────────

    [ServerRpc]
    public void ServerApplyProjectileDamage(EnemyHealth enemy, int damage, DamageType damageType, bool canCrit)
    {
        if (enemy == null || enemy.IsDead()) return;
        PlayerStats stats = GetComponent<PlayerStats>();
        int finalDamage = damage;
        if (canCrit && stats != null) finalDamage = stats.ApplyCriticalDamage(finalDamage);
        enemy.ReceiveDamage(finalDamage, damageType, stats);
    }

    // ── Death / Respawn hooks (called by PlayerStats.OnDeadChanged) ───────────

    public void OnPlayerDied()
    {
        _moving = false;
        if (IsServerStarted) _syncMoving.Value = false; // SyncVar can only be written on server
        if (modelAnimator != null)
        {
            modelAnimator.SetBool("IsMoving", false);
            modelAnimator.ResetTrigger("Die");
            modelAnimator.SetTrigger("Die");
        }
    }

    public void OnPlayerRespawned(Vector3 spawnPos)
    {
        _targetPosition = spawnPos;
        _moving = false;
        if (modelAnimator != null)
        {
            modelAnimator.SetBool("IsMoving", false);
            modelAnimator.Play("Idle");
        }
    }
}
