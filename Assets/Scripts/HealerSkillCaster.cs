using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all Healer skills.
/// Attach to the Slayer player network prefab — EnableClassSkillCaster activates it at runtime.
/// </summary>
public class HealerSkillCaster : NetworkBehaviour, ISkillCaster
{
    [Header("Keybinds")]
    public SkillType f8Skill  = SkillType.HolyBolt;
    public SkillType f9Skill  = SkillType.HolyRain;
    public SkillType f10Skill = SkillType.HolyCircleHeal;
    public SkillType f11Skill = SkillType.HealingOrbit;

    // ── Holy Bolt ─────────────────────────────────────────────────────────────
    [Header("Holy Bolt")]
    public GameObject holyBoltProjectilePrefab;
    public float boltSpeed    = 18f;
    public float boltRange    = 20f;

    // ── Holy Rain ─────────────────────────────────────────────────────────────
    [Header("Holy Rain")]
    public GameObject holyRainEffectPrefab;
    public float rainRadius   = 6f;
    public float rainDuration = 3f;      // how long rain lingers
    public float rainTickRate = 0.5f;    // heal tick every X seconds

    // ── Holy Circle Heal ──────────────────────────────────────────────────────
    [Header("Holy Circle Heal")]
    public GameObject holyCircleEffectPrefab;
    public float circleRadius = 5f;

    // ── Healing Orbit ─────────────────────────────────────────────────────────
    [Header("Healing Orbit")]
    public GameObject orbitEffectPrefab;
    public float orbitDuration   = 10f;  // how long the orbit lasts
    public float orbitHealAmount = 15f;  // heal per tick while active
    public float orbitTickRate   = 1f;

    // ── Cast Settings ─────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float effectHeightOffset   = 1.2f;
    public float holyBoltDelay        = 0.2f;
    public float holyRainDelay        = 0.4f;
    public float holyCircleHealDelay  = 0.4f;
    public float healingOrbitDelay    = 0.3f;

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

    // ── Runtime ───────────────────────────────────────────────────────────────
    private SkillType _selectedSkill = SkillType.HolyBolt;
    private float _nextCastTime  = 0f;
    private float _lastCooldown  = 0f;
    private bool  _isCasting     = false;

    public bool IsCasting => _isCasting;

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

        int   skillLevel = GetSkillLevel(skill);
        int   skillPower = GetSkillPower(skill, 25);
        int   manaCost   = GetManaCost(skill);
        float cooldown   = GetCooldown(skill);

        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
            ServerRotateToward(aimDir.normalized);

        PlayCastAnimation(skill);

        float delay = skill switch
        {
            SkillType.HolyBolt       => holyBoltDelay,
            SkillType.HolyRain       => holyRainDelay,
            SkillType.HolyCircleHeal => holyCircleHealDelay,
            SkillType.HealingOrbit   => healingOrbitDelay,
            _                        => 0.2f,
        };

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ServerCastSkill(skill, transform.position, aimDir.normalized, skillLevel, skillPower, manaCost);

        _lastCooldown = cooldown;
        _nextCastTime = Time.time + cooldown;
        _isCasting    = false;
    }

    // ── Server RPC ────────────────────────────────────────────────────────────

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
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;
        if (stats.currentMana < manaCost) return;
        stats.SpendMana(manaCost);

        switch (skill)
        {
            case SkillType.HolyBolt:       ServerHolyBolt(casterPos, aimDir, skillLevel, skillPower);  break;
            case SkillType.HolyRain:       ServerHolyRain(casterPos, skillLevel, skillPower);          break;
            case SkillType.HolyCircleHeal: ServerHolyCircleHeal(casterPos, skillLevel, skillPower);    break;
            case SkillType.HealingOrbit:   ServerHealingOrbit(skillLevel, skillPower);                 break;
        }
    }

    // ── Holy Bolt ─────────────────────────────────────────────────────────────
    // Fires a projectile toward the aimed direction that damages enemies on hit

    void ServerHolyBolt(Vector3 casterPos, Vector3 aimDir, int skillLevel, int skillPower)
    {
        if (holyBoltProjectilePrefab == null) return;

        Vector3 spawnPos = casterPos + Vector3.up * effectHeightOffset;
        SpawnHolyBoltEffect(spawnPos, Quaternion.LookRotation(aimDir));

        // Raycast along aim direction to find first enemy in range
        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        EnemyHealth closest = null;
        float closestDot = 0.8f; // must be roughly in front

        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            Vector3 toEnemy = enemy.transform.position - casterPos;
            float dist = toEnemy.magnitude;
            if (dist > boltRange) continue;

            float dot = Vector3.Dot(toEnemy.normalized, aimDir);
            if (dot > closestDot)
            {
                closestDot = dot;
                closest    = enemy;
            }
        }

        if (closest != null)
        {
            int damage = stats != null ? stats.GetMagicalSkillDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            closest.ReceiveDamage(damage, DamageType.Magical, stats);
        }
    }

    // ── Holy Rain ─────────────────────────────────────────────────────────────
    // Places a healing rain AoE — heals nearby players over time

    void ServerHolyRain(Vector3 casterPos, int skillLevel, int skillPower)
    {
        SpawnHolyRainEffect(casterPos);
        StartCoroutine(HolyRainTick(casterPos, skillLevel, skillPower));
    }

    IEnumerator HolyRainTick(Vector3 center, int skillLevel, int skillPower)
    {
        float duration = rainDuration + (skillLevel - 1) * 0.5f;
        float radius   = rainRadius   + (skillLevel - 1) * 0.5f;
        float elapsed  = 0f;
        int   healAmt  = skillPower + (skillLevel - 1) * 5;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(rainTickRate);
            elapsed += rainTickRate;

            // Heal all players in range
            PlayerStats[] allPlayers = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
            foreach (PlayerStats player in allPlayers)
            {
                if (player == null) continue;
                float dist = Vector3.Distance(center, player.transform.position);
                if (dist <= radius)
                    player.Heal(healAmt);
            }
        }
    }

    // ── Holy Circle Heal ──────────────────────────────────────────────────────
    // Instantly heals all players in a circle around the caster

    void ServerHolyCircleHeal(Vector3 casterPos, int skillLevel, int skillPower)
    {
        float radius  = circleRadius + (skillLevel - 1) * 0.5f;
        int   healAmt = skillPower * 2 + (skillLevel - 1) * 10;

        SpawnCircleHealEffect(casterPos);

        PlayerStats[] allPlayers = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        foreach (PlayerStats player in allPlayers)
        {
            if (player == null) continue;
            float dist = Vector3.Distance(casterPos, player.transform.position);
            if (dist <= radius)
                player.Heal(healAmt);
        }
    }

    // ── Healing Orbit ─────────────────────────────────────────────────────────
    // Summons orbiting lights that heal the caster over time

    void ServerHealingOrbit(int skillLevel, int skillPower)
    {
        SpawnOrbitEffect(transform.position);
        StartCoroutine(HealingOrbitTick(skillLevel, skillPower));
    }

    IEnumerator HealingOrbitTick(int skillLevel, int skillPower)
    {
        float duration  = orbitDuration  + (skillLevel - 1) * 2f;
        float healAmt   = orbitHealAmount + (skillLevel - 1) * 5f;
        float elapsed   = 0f;

        PlayerStats stats = GetComponent<PlayerStats>();

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(orbitTickRate);
            elapsed += orbitTickRate;
            if (stats != null) stats.Heal((int)healAmt);
        }
    }

    // ── ObserversRpc Effect Spawners ──────────────────────────────────────────

    [ObserversRpc]
    void SpawnHolyBoltEffect(Vector3 pos, Quaternion rot)
    {
        if (holyBoltProjectilePrefab == null) return;
        Instantiate(holyBoltProjectilePrefab, pos, rot);
    }

    [ObserversRpc]
    void SpawnHolyRainEffect(Vector3 pos)
    {
        if (holyRainEffectPrefab == null) return;
        Instantiate(holyRainEffectPrefab, pos, Quaternion.identity);
    }

    [ObserversRpc]
    void SpawnCircleHealEffect(Vector3 pos)
    {
        if (holyCircleEffectPrefab == null) return;
        Instantiate(holyCircleEffectPrefab, pos, Quaternion.identity);
    }

    [ObserversRpc]
    void SpawnOrbitEffect(Vector3 pos)
    {
        if (orbitEffectPrefab == null) return;
        GameObject go = Instantiate(orbitEffectPrefab, pos, Quaternion.identity);
        go.transform.SetParent(transform); // follows the caster
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    void PlayCastAnimation(SkillType skill)
    {
        if (_modelAnimator == null) return;

        string trigger = skill switch
        {
            SkillType.HealingOrbit   => HasParameter("Spell")  ? "Spell"  : "Cast",
            SkillType.HolyCircleHeal => HasParameter("Spell")  ? "Spell"  : "Cast",
            _                        => HasParameter("Attack") ? "Attack" : "Cast",
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
