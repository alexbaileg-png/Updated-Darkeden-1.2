using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.EventSystems;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float stopDistance = 0.35f;

    [Header("Visual")]
    public Transform modelTransform;
    public Animator modelAnimator;

    // Server-side movement state
    private Vector3 _targetPosition;
    private bool _moving = false;

    // Synced to all clients so remote players animate correctly
    [SyncVar(OnChange = nameof(OnMovingChanged))]
    private bool _syncMoving;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _targetPosition = transform.position;

        if (modelAnimator == null && modelTransform != null)
            modelAnimator = modelTransform.GetComponent<Animator>();

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
                cam.target = transform;
        }
    }

    void Update()
    {
        if (IsOwner)
            HandleInput();

        if (IsServerStarted)
            MoveOnServer();
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
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float dist))
                ServerSetMoveTarget(ray.GetPoint(dist));
        }
    }

    // ── Server-authoritative movement ─────────────────────────────────────────

    [ServerRpc]
    void ServerSetMoveTarget(Vector3 target)
    {
        _targetPosition = target;
        _moving = true;
    }

    void MoveOnServer()
    {
        if (!_moving)
        {
            if (_syncMoving) _syncMoving = false;
            return;
        }

        Vector3 dir = _targetPosition - transform.position;
        dir.y = 0f;

        if (dir.magnitude > stopDistance)
        {
            Vector3 moveDir = dir.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
            RotateVisual(moveDir);

            if (!_syncMoving) _syncMoving = true;
        }
        else
        {
            _moving = false;
            _targetPosition = transform.position;
            if (_syncMoving) _syncMoving = false;
        }
    }

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
        if (modelAnimator == null) return;
        modelAnimator.SetBool("IsMoving", next);
        modelAnimator.SetFloat("MoveSpeed", next ? moveSpeed : 0f);
    }

    // ── Server-authoritative damage (called by EnemyAI / skills) ─────────────

    [ServerRpc(RequireOwnership = false)]
    public void ServerTakeDamage(int damage, DamageType damageType)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.ReceiveDamage(damage, damageType);
    }
}
