using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all Swordmaster skills.
/// Attach to the Swordmaster player network prefab.
/// </summary>
public class SwordmasterSkillCaster : NetworkBehaviour, ISkillCaster
{
    [Header("Keybinds")]
    public SkillType f8Skill  = SkillType.JudgmentRush;
    public SkillType f9Skill  = SkillType.Consecration;
    public SkillType f10Skill = SkillType.AegisOfFaith;
    public SkillType f11Skill = SkillType.RadiantFlurry;

    // ── Judgment Rush ─────────────────────────────────────────────────────────
    [Header("Judgment Rush")]
    public GameObject judgmentRushEffectPrefab;   // VFX spawned along dash path
    public float rushDistance      = 10f;
    public float rushSpeed         = 30f;          // how fast the player moves
    public float rushKnockback     = 3f;
    public float rushWidth         = 1.2f;         // half-width of the line hitbox

    // ── Consecration ─────────────────────────────────────────────────────────
    [Header("Consecration")]
    public GameObject consecrationEffectPrefab;
    public float consecrationRadius = 5f;

    // ── Aegis of Faith ────────────────────────────────────────────────────────
    [Header("Aegis of Faith")]
    public GameObject aegisShieldEffectPrefab;    // holy shield VFX parented to player
    public float aegisBaseDuration  = 8f;
    public float aegisArmorBonus    = 30f;
    public float aegisResistBonus   = 20f;

    // ── Radiant Flurry ────────────────────────────────────────────────────────
    [Header("Radiant Flurry")]
    public GameObject radiantHitEffectPrefab;     // small hit flash per target
    public float flurryRadius       = 4f;
    public int   flurryMaxTargets   = 5;
    public float flurryHitInterval  = 0.15f;      // seconds between each hit

    // ── Shared ────────────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float castDelay = 0.1f;

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

    public float GetCooldownRemaining()   => Mathf.Max(0f, _nextCastTime - Time.time);
    public float GetCurrentSkillCooldown() => _lastCooldown;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SkillType _selectedSkill = SkillType.JudgmentRush;
    private float _nextCastTime  = 0f;
    private float _lastCooldown  = 0f;
    private bool  _isCasting     = false;

    private PlayerStats            _playerStats;
    private PlayerSkillManager     _skillManager;
    private NetworkPlayerController _netController;
    private Animator               _modelAnimator;

    // Active Aegis buff tracking (server-side)
    private bool  _aegisActive    = false;
    private float _aegisArmor     = 0f;
    private float _aegisResistance = 0f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _playerStats   = GetComponent<PlayerStats>();
        _skillManager  = GetComponent<PlayerSkillManager>();
        _netController = GetComponent<NetworkPlayerController>();

        if (_netController != null && _netController.modelTransform != null)
            _modelAnimator = _netController.modelTransform.GetComponent<Animator>();
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

    // ── Cast Routing ──────────────────────────────────────────────────────────

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;

        PlayCastAnimation();
        yield return new WaitForSeconds(castDelay);

        int   skillLevel = GetSkillLevel(skill);
        int   skillPower = GetSkillPower(skill, 30);
        int   manaCost   = GetManaCost(skill);
        float cooldown   = GetCooldown(skill);

        // Aim direction toward mouse
        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f && _netController != null)
            _netController.RotateVisualPublic(aimDir.normalized);

        ServerCastSkill(skill, transform.position, aimDir.normalized, skillLevel, skillPower, manaCost);

        _lastCooldown  = cooldown;
        _nextCastTime  = Time.time + cooldown;
        _isCasting     = false;
    }

    // ── Server-side skill execution ───────────────────────────────────────────

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
            case SkillType.JudgmentRush:   ServerJudgmentRush(casterPos, aimDir, skillLevel, skillPower);  break;
            case SkillType.Consecration:   ServerConsecration(casterPos, skillLevel, skillPower);           break;
            case SkillType.AegisOfFaith:   ServerAegisOfFaith(skillLevel);                                  break;
            case SkillType.RadiantFlurry:  ServerRadiantFlurry(casterPos, skillLevel, skillPower);          break;
        }
    }

    // ── Judgment Rush ─────────────────────────────────────────────────────────

    void ServerJudgmentRush(Vector3 casterPos, Vector3 aimDir, int skillLevel, int skillPower)
    {
        if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;

        float distance = rushDistance + (skillLevel - 1) * 1f;
        Vector3 endPos = casterPos + aimDir * distance;

        // Teleport player to end of rush
        transform.position = endPos;

        // Spawn VFX at midpoint of the dash
        SpawnEffectOnClients(judgmentRushEffectPrefab, casterPos + aimDir * (distance * 0.5f),
                             Quaternion.LookRotation(aimDir));

        // Damage + knockback every enemy in the line
        Collider[] hits = Physics.OverlapCapsule(casterPos, endPos, rushWidth);
        foreach (Collider col in hits)
        {
            EnemyHealth enemy = col.GetComponent<EnemyHealth>();
            if (enemy == null || enemy.IsDead()) continue;

            PlayerStats stats = GetComponent<PlayerStats>();
            int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            // Knockback — push enemy away from the rush direction
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(aimDir * rushKnockback, ForceMode.Impulse);
        }
    }

    // ── Consecration ─────────────────────────────────────────────────────────

    void ServerConsecration(Vector3 casterPos, int skillLevel, int skillPower)
    {
        float radius = consecrationRadius + (skillLevel - 1) * 0.5f;

        SpawnEffectOnClients(consecrationEffectPrefab, casterPos, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(casterPos, radius);
        PlayerStats stats = GetComponent<PlayerStats>();

        foreach (Collider col in hits)
        {
            EnemyHealth enemy = col.GetComponent<EnemyHealth>();
            if (enemy == null || enemy.IsDead()) continue;

            int damage = stats != null ? stats.GetMagicalSkillDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Magical, stats);
        }
    }

    // ── Aegis of Faith ────────────────────────────────────────────────────────

    void ServerAegisOfFaith(int skillLevel)
    {
        float duration = aegisBaseDuration + (skillLevel - 1) * 2f;

        // Remove old aegis if already active
        if (_aegisActive)
        {
            PlayerStats ps = GetComponent<PlayerStats>();
            if (ps != null)
            {
                ps.gearArmor      -= (int)_aegisArmor;
                ps.gearResistance  -= (int)_aegisResistance;
                ps.RecalculateStats();
            }
        }

        _aegisArmor      = aegisArmorBonus     + (skillLevel - 1) * 5f;
        _aegisResistance = aegisResistBonus    + (skillLevel - 1) * 3f;
        _aegisActive     = true;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.gearArmor      += (int)_aegisArmor;
            stats.gearResistance  += (int)_aegisResistance;
            stats.RecalculateStats();
        }

        SpawnEffectOnClients(aegisShieldEffectPrefab, transform.position, Quaternion.identity);

        StartCoroutine(AegisExpiry(duration));
    }

    IEnumerator AegisExpiry(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!_aegisActive) yield break;
        _aegisActive = false;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.gearArmor      -= (int)_aegisArmor;
            stats.gearResistance  -= (int)_aegisResistance;
            stats.RecalculateStats();
        }
    }

    // ── Radiant Flurry ────────────────────────────────────────────────────────

    void ServerRadiantFlurry(Vector3 casterPos, int skillLevel, int skillPower)
    {
        StartCoroutine(RadiantFlurryCoroutine(casterPos, skillLevel, skillPower));
    }

    IEnumerator RadiantFlurryCoroutine(Vector3 casterPos, int skillLevel, int skillPower)
    {
        int maxTargets = flurryMaxTargets + (skillLevel - 1);
        float radius   = flurryRadius + (skillLevel - 1) * 0.5f;

        Collider[] hits = Physics.OverlapSphere(casterPos, radius);
        PlayerStats stats = GetComponent<PlayerStats>();

        // Build a sorted list of valid enemies by distance
        List<EnemyHealth> targets = new List<EnemyHealth>();
        foreach (Collider col in hits)
        {
            EnemyHealth enemy = col.GetComponent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead())
                targets.Add(enemy);
            if (targets.Count >= maxTargets) break;
        }

        foreach (EnemyHealth enemy in targets)
        {
            if (enemy == null || enemy.IsDead()) continue;

            int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            SpawnEffectOnClients(radiantHitEffectPrefab,
                                 enemy.transform.position + Vector3.up * 1f,
                                 Quaternion.identity);

            yield return new WaitForSeconds(flurryHitInterval);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    [ObserversRpc]
    void SpawnEffectOnClients(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return;
        Instantiate(prefab, pos, rot);
    }

    void PlayCastAnimation()
    {
        if (_modelAnimator != null)
            _modelAnimator.SetTrigger("Attack");
    }

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

    int GetSkillPower(SkillType skill, int basePower)
    {
        int level = GetSkillLevel(skill);
        return basePower + (level - 1) * 8;
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
        if (_skillManager == null) return 3f;
        foreach (var p in _skillManager.skillProgress)
            if (p.skill != null && p.skill.skillType == skill)
                return p.skill.baseCooldown;
        return 3f;
    }
}
