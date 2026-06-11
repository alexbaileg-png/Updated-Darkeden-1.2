using System.Collections;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all vampire bloodline skills.
/// Attach this to the PlayerNetworkPrefabVampire prefab.
/// </summary>
public class VampireSkillCaster : NetworkBehaviour, ISkillCaster
{
    [Header("Keybinds")]
    public SkillType f8Skill  = SkillType.DarkBolt;
    public SkillType f9Skill  = SkillType.BloodDrain;
    public SkillType f10Skill = SkillType.BloodFog;
    public SkillType f11Skill = SkillType.VoidBurst;

    [Header("Dark Bolt")]
    public GameObject darkBoltPrefab;

    [Header("Blood Drain")]
    public GameObject bloodDrainBeamPrefab;   // visual beam effect
    public float bloodDrainDuration    = 2f;
    public float bloodDrainTickRate    = 0.5f;
    public float bloodDrainHealPercent = 0.4f; // 40% of damage returned as HP

    [Header("Blood Fog")]
    public GameObject bloodFogPrefab;
    public float bloodFogBaseRadius    = 8f;
    public float bloodFogBaseDuration  = 20f;
    public float bloodFogDurationPerInt = 0.5f; // extra seconds per INT above base (18)
    public int   bloodFogBaseInt       = 18;

    [Header("Void Burst")]
    public GameObject voidBurstEffectPrefab;
    public float voidBurstBaseRadius   = 5f;
    public float voidBurstRadiusPerLevel = 0.3f;

    [Header("Cast Settings")]
    public float castDelay = 0.1f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SkillType _selectedSkill = SkillType.DarkBolt;
    private float _nextCastTime = 0f;
    private bool _isCasting = false;
    private bool _isChanneling = false;

    private PlayerStats _playerStats;
    private PlayerSkillManager _skillManager;
    private NetworkPlayerController _netController;
    private Animator _modelAnimator;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _playerStats  = GetComponent<PlayerStats>();
        _skillManager = GetComponent<PlayerSkillManager>();
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
        if (_isCasting || _isChanneling) return;
        if (!CanCast(_selectedSkill)) return;

        StartCoroutine(CastRoutine(_selectedSkill));
    }

    bool CanCast(SkillType skill)
    {
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null && !ClassSkillConfig.CanUseSkill(character, skill)) return false;
        if (_skillManager != null && !_skillManager.IsSkillUnlocked(skill)) return false;
        // Vampires spend Health to cast — must have more than the cost (can't cast if it would kill them)
        int cost = GetManaCost(skill);
        if (_playerStats != null && _playerStats.currentHealth <= cost) return false;
        return true;
    }

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;

        // Rotate toward mouse / target
        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
        {
            aimDir.Normalize();
            if (_netController != null)
                _netController.RotateVisualPublic(aimDir);
        }

        PlayCastAnimation();
        yield return new WaitForSeconds(castDelay);

        int skillLevel  = GetSkillLevel(skill);
        int skillPower  = GetSkillPower(skill, 25);
        int healthCost  = GetManaCost(skill);   // vampires pay health, not mana
        Vector3 castPos = GetMouseWorldPosition();

        ServerCastSkill(skill, transform.position, aimDir, castPos, skillLevel, skillPower, healthCost);

        // Blood Drain blocks casting for the channel duration
        if (skill == SkillType.BloodDrain)
        {
            _isChanneling = true;
            yield return new WaitForSeconds(bloodDrainDuration);
            _isChanneling = false;
        }

        _nextCastTime = Time.time + GetCooldown(skill);
        _isCasting = false;
    }

    // ── Server RPC ────────────────────────────────────────────────────────────

    [ServerRpc]
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir,
                         Vector3 castPos, int skillLevel, int skillPower, int healthCost)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        // Vampires burn their own blood to cast — deduct health directly
        if (stats != null)
        {
            if (stats.currentHealth <= healthCost) return;  // prevent self-kill via casting
            stats.ReceiveDamage(healthCost, DamageType.Melee);
        }

        switch (skill)
        {
            case SkillType.DarkBolt:   ServerDarkBolt(casterPos, aimDir, skillPower); break;
            case SkillType.BloodDrain: ServerStartBloodDrain(castPos, skillLevel, skillPower); break;
            case SkillType.BloodFog:   ServerBloodFog(castPos, skillLevel); break;
            case SkillType.VoidBurst:  ServerVoidBurst(casterPos, skillLevel, skillPower); break;
        }
    }

    // ── Dark Bolt ─────────────────────────────────────────────────────────────

    void ServerDarkBolt(Vector3 casterPos, Vector3 direction, int damage)
    {
        if (direction.sqrMagnitude <= 0.01f) direction = transform.forward;
        direction.Normalize();

        Vector3 spawnPos = casterPos + direction * 1f + Vector3.up * 0.8f;

        // Home toward nearest enemy in aim direction
        EnemyHealth bestTarget = null;
        float bestAngle = 25f;

        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy.IsDead()) continue;
            Vector3 toEnemy = (enemy.transform.position - casterPos);
            toEnemy.y = 0f; toEnemy.Normalize();
            float angle = Vector3.Angle(direction, toEnemy);
            float dist  = Vector3.Distance(casterPos, enemy.transform.position);
            if (angle < bestAngle && dist < 30f) { bestAngle = angle; bestTarget = enemy; }
        }

        PlayerStats stats = GetComponent<PlayerStats>();
        int finalDamage = stats != null ? stats.GetMagicalSkillDamage(damage) : damage;

        SpawnDarkBoltVisual(spawnPos, direction,
            bestTarget != null ? bestTarget.gameObject : null, finalDamage);
    }

    [ObserversRpc]
    void SpawnDarkBoltVisual(Vector3 spawnPos, Vector3 direction, GameObject target, int damage)
    {
        if (darkBoltPrefab == null) return;

        GameObject proj = Instantiate(darkBoltPrefab, spawnPos, Quaternion.LookRotation(direction));
        Projectile projectile = proj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetAttacker(gameObject);
            projectile.damage     = damage;
            projectile.damageType = DamageType.Magical;
            projectile.canCrit    = true;

            if (target != null)
            {
                EnemyHealth eh = target.GetComponent<EnemyHealth>();
                if (eh != null) projectile.SetTarget(eh);
            }
        }
    }

    // ── Blood Drain ───────────────────────────────────────────────────────────

    void ServerStartBloodDrain(Vector3 castPos, int skillLevel, int damagePerTick)
    {
        // Find the closest enemy to the cast position within range
        EnemyHealth target = null;
        float closest = 12f;

        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy.IsDead()) continue;
            float dist = Vector3.Distance(castPos, enemy.transform.position);
            if (dist < closest) { closest = dist; target = enemy; }
        }

        if (target == null) return;

        StartCoroutine(BloodDrainChannel(target, skillLevel, damagePerTick));
        ShowBloodDrainBeam(target.gameObject);
    }

    IEnumerator BloodDrainChannel(EnemyHealth target, int skillLevel, int damagePerTick)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        int ticks = Mathf.RoundToInt(bloodDrainDuration / bloodDrainTickRate);

        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(bloodDrainTickRate);

            if (target == null || target.IsDead()) break;

            int finalDamage = stats != null ? stats.GetMagicalSkillDamage(damagePerTick) : damagePerTick;
            target.ReceiveDamage(finalDamage, DamageType.Magical, stats);

            // Heal caster
            if (stats != null)
            {
                int healAmount = Mathf.RoundToInt(finalDamage * bloodDrainHealPercent);
                stats.Heal(healAmount);
            }
        }

        HideBloodDrainBeam();
    }

    [ObserversRpc]
    void ShowBloodDrainBeam(GameObject target)
    {
        if (bloodDrainBeamPrefab == null || target == null) return;

        GameObject beam = Instantiate(bloodDrainBeamPrefab, transform.position, Quaternion.identity);
        BloodDrainBeam beamComp = beam.GetComponent<BloodDrainBeam>();
        if (beamComp != null)
        {
            beamComp.source = transform;
            beamComp.target = target.transform;
            beamComp.duration = bloodDrainDuration;
        }
    }

    [ObserversRpc]
    void HideBloodDrainBeam() { /* beam self-destructs via BloodDrainBeam.duration */ }

    // ── Blood Fog ─────────────────────────────────────────────────────────────

    void ServerBloodFog(Vector3 castPos, int skillLevel)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        int intelligence = stats != null ? stats.intelligence : bloodFogBaseInt;

        float extraFromInt = Mathf.Max(0, intelligence - bloodFogBaseInt) * bloodFogDurationPerInt;
        float extraFromLevel = (skillLevel - 1) * 3f;
        float duration = bloodFogBaseDuration + extraFromInt + extraFromLevel;

        castPos.y = 0f;
        SpawnBloodFog(castPos, bloodFogBaseRadius, duration);
    }

    [ObserversRpc]
    void SpawnBloodFog(Vector3 pos, float radius, float duration)
    {
        if (bloodFogPrefab == null) return;

        GameObject fog = Instantiate(bloodFogPrefab, pos, Quaternion.identity);
        BloodFogArea fogArea = fog.GetComponent<BloodFogArea>();
        if (fogArea == null) fogArea = fog.AddComponent<BloodFogArea>();

        fogArea.radius   = radius;
        fogArea.duration = duration;
        fogArea.casterObject = gameObject;
    }

    // ── Void Burst ────────────────────────────────────────────────────────────

    void ServerVoidBurst(Vector3 casterPos, int skillLevel, int damage)
    {
        float radius = voidBurstBaseRadius + (skillLevel - 1) * voidBurstRadiusPerLevel;

        PlayerStats stats = GetComponent<PlayerStats>();
        int finalDamage = stats != null ? stats.GetMagicalSkillDamage(damage) : damage;

        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy.IsDead()) continue;
            float dist = Vector3.Distance(casterPos, enemy.transform.position);
            if (dist <= radius)
                enemy.ReceiveDamage(finalDamage, DamageType.Magical, stats);
        }

        SpawnVoidBurstVisual(casterPos, radius);
    }

    [ObserversRpc]
    void SpawnVoidBurstVisual(Vector3 pos, float radius)
    {
        if (voidBurstEffectPrefab == null) return;

        GameObject effect = Instantiate(voidBurstEffectPrefab, pos, Quaternion.identity);
        if (voidBurstBaseRadius > 0.01f)
            effect.transform.localScale *= radius / voidBurstBaseRadius;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    int GetSkillLevel(SkillType skill)  => _skillManager != null ? Mathf.Max(1, _skillManager.GetSkillLevel(skill)) : 1;
    int GetSkillPower(SkillType skill, int fallback) => _skillManager != null && _skillManager.GetSkillPower(skill) > 0
                                                        ? _skillManager.GetSkillPower(skill) : fallback;
    float GetCooldown(SkillType skill)  => _skillManager != null ? _skillManager.GetSkillCooldown(skill) : 1f;
    int GetManaCost(SkillType skill)    => _skillManager != null ? _skillManager.GetSkillManaCost(skill) : 0;

    public SkillType CurrentSelectedSkill      => _selectedSkill;
    public float GetCooldownRemaining()        => Mathf.Max(0f, _nextCastTime - Time.time);
    public float GetCurrentSkillCooldown()     => GetCooldown(_selectedSkill);
    public void  SetSelectedSkill(SkillType s) => _selectedSkill = s;

    public void BindSkill(KeyCode key, SkillType skill)
    {
        if (key == KeyCode.F8)  f8Skill  = skill;
        if (key == KeyCode.F9)  f9Skill  = skill;
        if (key == KeyCode.F10) f10Skill = skill;
        if (key == KeyCode.F11) f11Skill = skill;
    }

    Vector3 GetAimDirection()
    {
        EnemyHealth target = HoverDetector.CurrentEnemyTarget;
        if (target != null)
        {
            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            return dir;
        }
        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 toMouse  = mousePos - transform.position;
        toMouse.y = 0f;
        return toMouse;
    }

    Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position + transform.forward * 3f;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return transform.position + transform.forward * 3f;
    }

    void PlayCastAnimation()
    {
        if (_modelAnimator == null) return;
        _modelAnimator.ResetTrigger("Cast");
        _modelAnimator.SetTrigger("Cast");
    }
}
