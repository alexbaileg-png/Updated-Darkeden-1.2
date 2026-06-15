using System.Collections;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all Blood Knight skills.
/// Attach to the Blood Knight player network prefab.
/// </summary>
public class BloodKnightSkillCaster : NetworkBehaviour, ISkillCaster
{
    [Header("Keybinds")]
    public SkillType f8Skill  = SkillType.BloodyTalons;
    public SkillType f9Skill  = SkillType.BloodNova;
    public SkillType f10Skill = SkillType.BloodArmor;
    public SkillType f11Skill = SkillType.CrimsonCyclone;

    // ── Bloody Talons ─────────────────────────────────────────────────────────
    [Header("Bloody Talons")]
    public GameObject bloodyTalonsPrefab;
    public float talonsConeAngle    = 120f; // total cone width in degrees
    public float talonsRange        = 4f;  // max enemy distance
    public int   talonsMaxTargets   = 10;  // max enemies hit per cast
    public float talonsForwardOffset = 1.5f; // how far in front of player the effect spawns
    public float talonsHeightOffset  = 0.8f; // height above ground (on hit)
    public float talonsMissHeightOffset = 1.2f; // height above ground (on miss)

    // ── Cast Settings ─────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float castDelay        = 0.1f;
    public float postCastLockTime = 0.5f;

    // ── ISkillCaster ──────────────────────────────────────────────────────────
    public SkillType CurrentSelectedSkill => _selectedSkill;
    public void SetSelectedSkill(SkillType skill) => _selectedSkill = skill;
    public void BindSkill(KeyCode key, SkillType skill)
    {
        if (key == KeyCode.F8)  f8Skill  = skill;
        if (key == KeyCode.F9)  f9Skill  = skill;
        if (key == KeyCode.F10) f10Skill = skill;
        if (key == KeyCode.F11) f11Skill = skill;
    }
    public float GetCooldownRemaining()    => Mathf.Max(0f, _nextCastTime - Time.time);
    public float GetCurrentSkillCooldown() => _lastCooldown;
    public SavedKeyBindings GetBindings()          => new SavedKeyBindings();
    public void LoadBindings(SavedKeyBindings saved) { }

    // ── Runtime ───────────────────────────────────────────────────────────────
    private SkillType _selectedSkill = SkillType.BloodyTalons;
    private float _nextCastTime  = 0f;
    private float _lastCooldown  = 0f;
    private bool  _isCasting     = false;

    private PlayerStats             _playerStats;
    private PlayerSkillManager      _skillManager;
    private NetworkPlayerController _netController;
    private Animator                _modelAnimator;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _playerStats   = GetComponent<PlayerStats>();
        _skillManager  = GetComponent<PlayerSkillManager>();
        _netController = GetComponent<NetworkPlayerController>();
        if (_netController != null)
            _modelAnimator = _netController.modelAnimator;
        if (_modelAnimator == null)
            _modelAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!IsOwner) return;
        HandleSkillSelection();
        HandleCasting();
    }

    void HandleSkillSelection()
    {
        if (Input.GetKeyDown(KeyCode.F8))  _selectedSkill = f8Skill;
        if (Input.GetKeyDown(KeyCode.F9))  _selectedSkill = f9Skill;
        if (Input.GetKeyDown(KeyCode.F10)) _selectedSkill = f10Skill;
        if (Input.GetKeyDown(KeyCode.F11)) _selectedSkill = f11Skill;
    }

    void HandleCasting()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (_playerStats != null && _playerStats.isDead) return;
        if (Time.time < _nextCastTime) return;
        if (_isCasting) return;
        if (!CanCast(_selectedSkill)) return;

        StartCoroutine(CastRoutine(_selectedSkill));
    }

    bool CanCast(SkillType skill)
    {
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null && !ClassSkillConfig.CanUseSkill(character, skill)) return false;

        int level = GetSkillLevel(skill);
        if (level <= 0) return false;

        int manaCost = GetManaCost(skill);
        if (_playerStats != null && _playerStats.currentMana < manaCost) return false;

        return true;
    }

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;
        _netController?.ServerStopMovement();

        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
        {
            _netController?.RotateVisualPublic(aimDir.normalized);
            ServerRotateToward(aimDir.normalized);
        }

        PlayCastAnimation(skill);

        if (castDelay > 0f)
            yield return new WaitForSeconds(castDelay);

        int skillLevel = GetSkillLevel(skill);
        int skillPower = GetSkillPower(skill);
        int manaCost   = GetManaCost(skill);

        ServerCastSkill(skill, transform.position, aimDir.normalized, skillLevel, skillPower, manaCost);

        _lastCooldown = GetCooldown(skill);
        _nextCastTime = Time.time + _lastCooldown;

        yield return new WaitForSeconds(postCastLockTime);
        _isCasting = false;
    }

    // ── Server RPC ────────────────────────────────────────────────────────────

    [ServerRpc]
    void ServerRotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            RpcRotateVisual(direction);
    }

    [ObserversRpc(ExcludeOwner = true)]
    void RpcRotateVisual(Vector3 direction)
    {
        _netController?.RotateVisualPublic(direction);
    }

    [ServerRpc]
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir,
                         int skillLevel, int skillPower, int manaCost)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;
        if (stats.currentMana < manaCost) return;
        stats.SpendMana(manaCost);

        switch (skill)
        {
            case SkillType.BloodyTalons: ServerBloodyTalons(casterPos, aimDir, skillLevel, skillPower); break;
        }
    }

    // ── Bloody Talons ─────────────────────────────────────────────────────────

    void ServerBloodyTalons(Vector3 casterPos, Vector3 aimDir, int skillLevel, int skillPower)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
        damage     = stats != null ? stats.ApplyCriticalDamage(damage) : damage;

        float range     = talonsRange + (skillLevel - 1) * 0.3f;
        float halfCone  = talonsConeAngle * 0.5f;
        int   hits      = 0;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (hits >= talonsMaxTargets) break;
            if (enemy == null || enemy.IsDead()) continue;

            Vector3 toEnemy = enemy.transform.position - casterPos;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;
            if (dist > range) continue;

            float angle = Vector3.Angle(aimDir, toEnemy.normalized);
            if (angle > halfCone) continue;

            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            Vector3 fxPos = enemy.transform.position;
            fxPos.y = talonsHeightOffset;
            SpawnTalonsEffect(fxPos, Quaternion.LookRotation(aimDir));

            hits++;
        }

        // If no enemies hit, still show effect in front of player
        if (hits == 0)
        {
            Vector3 fxPos = casterPos + aimDir * talonsForwardOffset;
            fxPos.y = talonsMissHeightOffset;
            SpawnTalonsEffect(fxPos, Quaternion.LookRotation(aimDir));
        }
    }

    [ObserversRpc]
    void SpawnTalonsEffect(Vector3 pos, Quaternion rot)
    {
        if (bloodyTalonsPrefab == null) return;
        Instantiate(bloodyTalonsPrefab, pos, rot);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    void PlayCastAnimation(SkillType skill)
    {
        if (_modelAnimator == null) return;

        string trigger = skill switch
        {
            _ => HasParameter("Attack") ? "Attack" : "Cast",
        };

        foreach (string t in new[] { "Cast", "Attack", "Spell" })
            _modelAnimator.ResetTrigger(t);

        _modelAnimator.SetTrigger(trigger);
    }

    bool HasParameter(string paramName)
    {
        if (_modelAnimator == null) return false;
        foreach (AnimatorControllerParameter p in _modelAnimator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 GetAimDirection()
    {
        if (Camera.main == null) return transform.forward;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 target = ray.GetPoint(dist);
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
        }
        return transform.forward;
    }

    int GetSkillLevel(SkillType skill)
    {
        if (_skillManager == null) return 1;
        foreach (var p in _skillManager.skillProgress)
            if (p.skill != null && p.skill.skillType == skill && p.unlocked)
                return Mathf.Max(1, p.skillLevel);
        return 0;
    }

    int GetSkillPower(SkillType skill)
    {
        if (_skillManager == null) return 20;
        foreach (var p in _skillManager.skillProgress)
            if (p.skill != null && p.skill.skillType == skill)
                return p.skill.basePower + (Mathf.Max(1, p.skillLevel) - 1) * p.skill.powerPerSkillLevel;
        return 20;
    }

    int GetManaCost(SkillType skill)
    {
        if (_skillManager == null) return 10;
        foreach (var p in _skillManager.skillProgress)
            if (p.skill != null && p.skill.skillType == skill)
                return p.skill.baseManaCost;
        return 10;
    }

    float GetCooldown(SkillType skill)
    {
        if (_skillManager == null) return 1f;
        foreach (var p in _skillManager.skillProgress)
            if (p.skill != null && p.skill.skillType == skill)
                return p.skill.baseCooldown;
        return 1f;
    }
}
