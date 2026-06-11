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
    public float rushDuration      = 0.25f;        // seconds the dash takes (matches animation)
    public float rushKnockback     = 3f;
    public float rushWidth         = 2f;           // half-width of the line hitbox

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
    public float effectHeightOffset = 1.2f;
    [Tooltip("Delay before JudgmentRush fires (matches wind-up)")]
    public float judgmentRushDelay  = 0.15f;
    [Tooltip("Delay before Consecration fires — set to when the character lands")]
    public float consecrationDelay  = 0.5f;
    [Tooltip("Delay before RadiantFlurry fires")]
    public float radiantFlurryDelay = 0.15f;
    [Tooltip("Delay before AegisOfFaith fires")]
    public float aegisDelay         = 0.2f;

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

    public bool IsCasting => _isCasting;

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

        // Use the exact same Animator reference as NetworkPlayerController
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
        if (character != null && !ClassSkillConfig.CanUseSkill(character, skill))
        {
            Debug.Log($"[Skill] CanCast BLOCKED — class can't use {skill}");
            return false;
        }

        int level = GetSkillLevel(skill);
        if (level <= 0)
        {
            Debug.Log($"[Skill] CanCast BLOCKED — {skill} not unlocked (level={level})");
            return false;
        }

        int manaCost = GetManaCost(skill);
        if (_playerStats != null && _playerStats.currentMana < manaCost)
        {
            Debug.Log($"[Skill] CanCast BLOCKED — not enough mana ({_playerStats.currentMana} < {manaCost})");
            return false;
        }

        return true;
    }

    // ── Cast Routing ──────────────────────────────────────────────────────────

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;

        int   skillLevel = GetSkillLevel(skill);
        int   skillPower = GetSkillPower(skill, 30);
        int   manaCost   = GetManaCost(skill);
        float cooldown   = GetCooldown(skill);

        // Aim direction toward mouse
        Vector3 aimDir = GetAimDirection();

        // Play animation FIRST
        PlayCastAnimation();

        // Rotate toward aim direction via server
        if (aimDir.sqrMagnitude > 0.01f)
            ServerRotateToward(aimDir.normalized);

        // Wait for the hit frame of this skill's animation before firing
        float delay = skill switch
        {
            SkillType.JudgmentRush  => judgmentRushDelay,
            SkillType.Consecration  => consecrationDelay,
            SkillType.AegisOfFaith  => aegisDelay,
            SkillType.RadiantFlurry => radiantFlurryDelay,
            _                       => 0.15f,
        };

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ServerCastSkill(skill, transform.position, aimDir.normalized, skillLevel, skillPower, manaCost);

        _lastCooldown  = cooldown;
        _nextCastTime  = Time.time + cooldown;
        _isCasting     = false;
    }

    // ── Server-side skill execution ───────────────────────────────────────────

    [ServerRpc]
    void ServerRotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    [ServerRpc]
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir,
                         int skillLevel, int skillPower, int manaCost)
    {
        Debug.Log($"[Skill] ServerCastSkill received — skill={skill}, power={skillPower}, mana={manaCost}");
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) { Debug.Log("[Skill] BLOCKED — PlayerStats null on server"); return; }
        if (stats.currentMana < manaCost) { Debug.Log($"[Skill] BLOCKED — server mana {stats.currentMana} < {manaCost}"); return; }
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

        // Face the dash direction on the server
        if (aimDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(aimDir);

        // Spawn effect at player center height along the midpoint of the dash
        Vector3 midpoint = casterPos + aimDir * (distance * 0.5f);
        midpoint.y = casterPos.y + effectHeightOffset;
        SpawnJudgmentRushEffect(midpoint, Quaternion.LookRotation(aimDir), distance);

        StartCoroutine(SmoothDash(endPos, rushDuration));

        // Physics.Overlap won't work across FishNet physics scenes —
        // do a manual distance check against all enemies instead
        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;

            // Project enemy position onto the dash line
            Vector3 toEnemy = enemy.transform.position - casterPos;
            float dot = Vector3.Dot(toEnemy, aimDir);

            // Must be within the line segment (not behind or past the end)
            if (dot < -1f || dot > distance + 1f) continue;

            // Perpendicular distance from the dash line
            Vector3 projected = casterPos + aimDir * Mathf.Clamp(dot, 0f, distance);
            float distFromLine = Vector3.Distance(
                new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z),
                new Vector3(projected.x, 0f, projected.z));

            if (distFromLine > rushWidth) continue;

            int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            // Knockback
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(aimDir * rushKnockback, ForceMode.Impulse);
        }
    }

    // ── Consecration ─────────────────────────────────────────────────────────

    IEnumerator SmoothDash(Vector3 endPos, float duration)
    {
        NetworkPlayerController controller = GetComponent<NetworkPlayerController>();
        if (controller != null) controller.ResetMovementTarget(transform.position);

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        transform.position = endPos;
        if (controller != null) controller.ResetMovementTarget(endPos);
    }

    void ServerConsecration(Vector3 casterPos, int skillLevel, int skillPower)
    {
        float radius = consecrationRadius + (skillLevel - 1) * 0.5f;

        // Spawn slightly above ground so the ring sits on the surface
        Vector3 effectPos = new Vector3(casterPos.x, casterPos.y + 0.05f, casterPos.z);
        SpawnConsecrationEffect(effectPos);

        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            float dist = Vector3.Distance(casterPos, enemy.transform.position);
            if (dist > radius) continue;

            int damage = stats != null ? stats.GetMagicalSkillDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Magical, stats);
        }
    }

    // ── Aegis of Faith ────────────────────────────────────────────────────────

    void ServerAegisOfFaith(int skillLevel)
    {
        float duration = aegisBaseDuration + (skillLevel - 1) * 2f;

        PlayerStats stats = GetComponent<PlayerStats>();

        // Remove old aegis if already active before reapplying
        if (_aegisActive && stats != null)
        {
            stats.gearArmor      -= (int)_aegisArmor;
            stats.gearResistance -= (int)_aegisResistance;
            stats.RecalculateStats();
        }

        _aegisArmor      = aegisArmorBonus  + (skillLevel - 1) * 5f;
        _aegisResistance = aegisResistBonus + (skillLevel - 1) * 3f;
        _aegisActive     = true;

        if (stats != null)
        {
            stats.gearArmor      += (int)_aegisArmor;
            stats.gearResistance += (int)_aegisResistance;
            stats.RecalculateStats();
        }

        SpawnAegisEffect(transform.position);

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
            stats.gearResistance -= (int)_aegisResistance;
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

        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        // Build list of enemies within radius, sorted by distance
        List<EnemyHealth> targets = new List<EnemyHealth>();
        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            if (Vector3.Distance(casterPos, enemy.transform.position) <= radius)
                targets.Add(enemy);
            if (targets.Count >= maxTargets) break;
        }

        foreach (EnemyHealth enemy in targets)
        {
            if (enemy == null || enemy.IsDead()) continue;

            int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            SpawnRadiantHitEffect(enemy.transform.position + Vector3.up * 1f);

            yield return new WaitForSeconds(flurryHitInterval);
        }
    }

    bool HasAnimatorState(string stateName)
    {
        if (_modelAnimator == null) return false;
        foreach (AnimatorControllerParameter p in _modelAnimator.parameters)
            if (p.name == stateName) return true;
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    [ObserversRpc]
    void SpawnJudgmentRushEffect(Vector3 pos, Quaternion rot, float distance)
    {
        if (judgmentRushEffectPrefab == null) return;
        GameObject go = Instantiate(judgmentRushEffectPrefab, pos, rot);
        JudgmentRushEffect fx = go.GetComponent<JudgmentRushEffect>();
        if (fx != null) fx.rushDistance = distance;
    }

    [ObserversRpc]
    void SpawnConsecrationEffect(Vector3 pos)
    {
        if (consecrationEffectPrefab == null) return;
        Instantiate(consecrationEffectPrefab, pos, Quaternion.identity);
    }

    [ObserversRpc]
    void SpawnAegisEffect(Vector3 pos)
    {
        if (aegisShieldEffectPrefab == null) return;
        GameObject go = Instantiate(aegisShieldEffectPrefab, pos, Quaternion.identity);
        go.transform.SetParent(transform); // parent to player so it follows them
    }

    [ObserversRpc]
    void SpawnRadiantHitEffect(Vector3 pos)
    {
        if (radiantHitEffectPrefab == null) return;
        Instantiate(radiantHitEffectPrefab, pos, Quaternion.identity);
    }

    void PlayCastAnimation()
    {
        PlayCastAnimation(_selectedSkill);
    }

    void PlayCastAnimation(SkillType skill)
    {
        if (_modelAnimator == null) return;

        string trigger = skill switch
        {
            SkillType.AegisOfFaith  => HasAnimatorState("Spell")         ? "Spell"         : "Cast",
            SkillType.Consecration  => HasAnimatorState("Consecration")   ? "Consecration"  : "Cast",
            _                       => HasAnimatorState("Attack")         ? "Attack"        : "Cast",
        };

        // Reset all cast triggers first to avoid stacking
        foreach (string t in new[] { "Cast", "Attack", "Spell", "Consecration" })
            _modelAnimator.ResetTrigger(t);

        _modelAnimator.SetTrigger(trigger);
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
