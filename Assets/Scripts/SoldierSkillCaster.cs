using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all Soldier skills.
/// Attach to the Soldier player network prefab.
/// </summary>
public class SoldierSkillCaster : NetworkBehaviour, ISkillCaster
{
    private readonly Dictionary<KeyCode, List<SkillType>> _keyBindings = new Dictionary<KeyCode, List<SkillType>>
    {
        { KeyCode.F8,  new List<SkillType>() },
        { KeyCode.F9,  new List<SkillType>() },
        { KeyCode.F10, new List<SkillType>() },
        { KeyCode.F11, new List<SkillType>() },
    };
    private readonly Dictionary<KeyCode, int> _keyIndex = new Dictionary<KeyCode, int>
    {
        { KeyCode.F8, 0 }, { KeyCode.F9, 0 }, { KeyCode.F10, 0 }, { KeyCode.F11, 0 },
    };

    // ── Single Shot ───────────────────────────────────────────────────────────
    [Header("Single Shot")]
    public GameObject bulletPrefab;
    public float bulletSpeed    = 30f;
    public float bulletRange    = 25f;
    public Vector3 bulletModelRotationOffset = new Vector3(90f, 0f, 0f);

    // ── Triple Shot ───────────────────────────────────────────────────────────
    [Header("Triple Shot")]
    public float tripleFireRate = 0.25f;   // seconds between each of the 3 bullets

    // ── Automatic Fire ────────────────────────────────────────────────────────
    [Header("Automatic Fire")]
    public int   autoShotCount      = 8;    // bullets fired per burst
    public float autoFireRate       = 0.1f; // seconds between each bullet
    public float autoSpreadAngle    = 6f;   // random cone spread per bullet

    // ── Orbital Strike ────────────────────────────────────────────────────────
    [Header("Orbital Strike")]
    public GameObject orbitalWarningPrefab;      // laser/beam that spirals to center
    public GameObject orbitalImpactPrefab;       // massive explosion VFX at impact
    public float orbitalCastRange        = 20f;  // max targeting distance
    public float orbitalWarningTimeBase  = 3f;   // warning duration at skill level 1
    public float orbitalWarningTimeMin   = 0.8f; // minimum warning duration at max level
    public float orbitalRadius           = 6f;   // AoE radius
    public int   orbitalBaseDamage       = 80;   // damage at skill level 1
    public int   orbitalDamagePerLevel   = 20;   // extra damage per skill level

    // ── Cast Settings ─────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float effectHeightOffset  = 0.9f;
    // Local-space offset from player center to gun barrel (tweak X/Z to shift spawn point)
    public Vector3 bulletSpawnLocalOffset = Vector3.zero;
    // Degrees added to Y when rotating the visual to face aim direction (use -90 or 90 to correct model alignment)
    public float playerFacingYOffset = -90f;
    public float singleShotDelay      = 0.1f;
    public float tripleShotDelay      = 0.1f;
    public float autoFireDelay        = 0.15f;
    public float orbitalStrikeDelay   = 0.3f;

    [Header("Skill Cooldowns")]
    public float singleShotCooldown    = 0.5f;
    public float tripleShotCooldown    = 2f;
    public float autoFireCooldown      = 5f;
    public float orbitalStrikeCooldown = 30f;

    [Header("Skill Mana Costs")]
    public int singleShotMana    = 3;
    public int tripleShotMana    = 8;
    public int autoFireMana      = 15;
    public int orbitalStrikeMana = 40;

    [Header("Knockback")]
    public float bulletKnockback = 4f;   // force applied per bullet hit

    [Header("Weapon Visual")]
    public float weaponShootHoldDuration = 0.4f;

    // ── ISkillCaster ──────────────────────────────────────────────────────────
    public SkillType CurrentSelectedSkill => _selectedSkill;
    public void SetSelectedSkill(SkillType skill) => _selectedSkill = skill;

    public SavedKeyBindings GetBindings()
    {
        var saved = new SavedKeyBindings();
        foreach (var kvp in _keyBindings)
        {
            var entry = new SavedKeyBinding { key = kvp.Key.ToString() };
            foreach (var skill in kvp.Value)
                entry.skills.Add(skill.ToString());
            saved.bindings.Add(entry);
        }
        return saved;
    }

    public void LoadBindings(SavedKeyBindings saved)
    {
        if (saved?.bindings == null) return;
        foreach (var entry in saved.bindings)
        {
            if (!System.Enum.TryParse(entry.key, out KeyCode key)) continue;
            if (!_keyBindings.ContainsKey(key)) continue;
            _keyBindings[key].Clear();
            foreach (var skillStr in entry.skills)
                if (System.Enum.TryParse(skillStr, out SkillType skill))
                    _keyBindings[key].Add(skill);
            _keyIndex[key] = 0;
        }
        foreach (var kvp in _keyBindings)
            if (kvp.Value.Count > 0) { _selectedSkill = kvp.Value[0]; break; }
    }

    public void BindSkill(KeyCode key, SkillType skill)
    {
        if (!_keyBindings.ContainsKey(key)) return;
        List<SkillType> list = _keyBindings[key];
        if (list.Contains(skill))
            list.Remove(skill);
        else
        {
            list.Add(skill);
            _keyIndex[key] = list.Count - 1;
            _selectedSkill = skill;
        }
    }

    public float GetCooldownRemaining()    => Mathf.Max(0f, GetNextCastTime(_selectedSkill) - Time.time);
    public float GetCurrentSkillCooldown() => GetCooldown(_selectedSkill);

    float GetNextCastTime(SkillType skill) => _nextCastTimePerSkill.TryGetValue(skill, out float t) ? t : 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SkillType _selectedSkill;
    private readonly Dictionary<SkillType, float> _nextCastTimePerSkill = new Dictionary<SkillType, float>();
    private bool _isCasting = false;

    private PlayerStats             _playerStats;
    private PlayerSkillManager      _skillManager;
    private NetworkPlayerController _netController;
    private Animator                _modelAnimator;
    private EquipmentManager        _equipmentManager;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _playerStats      = GetComponent<PlayerStats>();
        _skillManager     = GetComponent<PlayerSkillManager>();
        _netController    = GetComponent<NetworkPlayerController>();
        _equipmentManager = GetComponent<EquipmentManager>()
                         ?? GetComponentInParent<EquipmentManager>()
                         ?? GetComponentInChildren<EquipmentManager>();
        if (_equipmentManager == null)
            Debug.LogWarning("[SoldierSkillCaster] EquipmentManager not found — weapon rotation won't work.");

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
        if (Input.GetKeyDown(KeyCode.F8))  CycleKey(KeyCode.F8);
        if (Input.GetKeyDown(KeyCode.F9))  CycleKey(KeyCode.F9);
        if (Input.GetKeyDown(KeyCode.F10)) CycleKey(KeyCode.F10);
        if (Input.GetKeyDown(KeyCode.F11)) CycleKey(KeyCode.F11);
    }

    void CycleKey(KeyCode key)
    {
        if (!_keyBindings.ContainsKey(key)) return;
        List<SkillType> list = _keyBindings[key];
        if (list.Count == 0) return;
        int idx = (_keyIndex[key] + 1) % list.Count;
        _keyIndex[key] = idx;
        _selectedSkill = list[idx];
    }

    void HandleCasting()
    {
        if (_playerStats != null && _playerStats.isDead) return;

        // Auto fire: hold right-click to keep firing, release to stop
        if (_selectedSkill == SkillType.AutomaticFire)
        {
            if (Input.GetMouseButton(1) && !_isCasting && CanCast(_selectedSkill))
                StartCoroutine(AutoFireHoldRoutine());
            return;
        }

        if (!Input.GetMouseButtonDown(1)) return;
        if (_isCasting) return;
        if (!CanCast(_selectedSkill)) return;

        StartCoroutine(CastRoutine(_selectedSkill));
    }

    bool CanCast(SkillType skill)
    {
        if (Time.time < GetNextCastTime(skill)) return false;
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null && !ClassSkillConfig.CanUseSkill(character, skill)) return false;
        if (_skillManager != null && !_skillManager.IsSkillUnlocked(skill)) return false;
        if (_playerStats != null && _playerStats.currentMana < GetManaCost(skill)) return false;
        return true;
    }

    // ── Cast Routing ──────────────────────────────────────────────────────────

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;
        _netController?.ServerStopMovement();
        ApplyWeaponCastRotation();

        Vector3 aimDir = GetAimDirection(out float aimDist);
        if (aimDir.sqrMagnitude > 0.01f)
        {
            _netController?.RotateVisualPublic(FacingDir(aimDir.normalized));
            ServerRotateToward(aimDir.normalized);
        }

        if (skill != SkillType.TripleShot)
            PlayCastAnimation(skill);

        float delay = GetCastDelay(skill);
        yield return new WaitForSeconds(delay);

        int   manaCost = GetManaCost(skill);
        float cooldown = GetCooldown(skill);

        Vector3 spawnPos = transform.position + transform.TransformDirection(bulletSpawnLocalOffset) + Vector3.up * effectHeightOffset;
        ServerCastSkill(skill, spawnPos, aimDir.normalized, aimDist, manaCost);

        _nextCastTimePerSkill[skill] = Time.time + cooldown;
        _isCasting = false;

        yield return new WaitForSeconds(weaponShootHoldDuration);
        RestoreWeaponRotation();
    }

    // Holds auto fire active while right mouse is held
    IEnumerator AutoFireHoldRoutine()
    {
        _isCasting = true;
        _netController?.ServerStopMovement();

        _modelAnimator?.SetBool("IsAutoFiring", true);
        ApplyWeaponCastRotation();

        while (Input.GetMouseButton(1) && !(_playerStats != null && _playerStats.isDead))
        {
            if (Time.time < GetNextCastTime(SkillType.AutomaticFire)) { yield return null; continue; }
            if (_playerStats != null && _playerStats.currentMana < GetManaCost(SkillType.AutomaticFire)) break;

            Vector3 aimDir = GetAimDirection(out float aimDist);
            if (aimDir.sqrMagnitude > 0.01f)
            {
                _netController?.RotateVisualPublic(FacingDir(aimDir.normalized));
                ServerRotateToward(aimDir.normalized);
            }

            Vector3 spawnPos = transform.position + transform.TransformDirection(bulletSpawnLocalOffset) + Vector3.up * effectHeightOffset;
            ServerCastSkill(SkillType.AutomaticFire, spawnPos, aimDir.normalized, aimDist, GetManaCost(SkillType.AutomaticFire));
            _nextCastTimePerSkill[SkillType.AutomaticFire] = Time.time + autoFireRate;

            yield return new WaitForSeconds(autoFireRate);
        }

        if (_modelAnimator != null)
            _modelAnimator.SetBool("IsAutoFiring", false);
        RestoreWeaponRotation();

        _isCasting = false;
    }

    float GetCastDelay(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.SingleShot:    return singleShotDelay;
            case SkillType.TripleShot:    return tripleShotDelay;
            case SkillType.AutomaticFire: return autoFireDelay;
            case SkillType.OrbitalStrike: return orbitalStrikeDelay;
            default:                      return 0.1f;
        }
    }

    // ── Rotation RPCs ─────────────────────────────────────────────────────────

    // Rotates the aim direction by playerFacingYOffset before passing to the visual
    // so the model faces correctly when the mesh forward doesn't match transform forward.
    Vector3 FacingDir(Vector3 aimDir) =>
        Quaternion.Euler(0f, playerFacingYOffset, 0f) * aimDir;

    [ServerRpc]
    void ServerRotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            RpcRotateVisual(direction);
    }

    [ObserversRpc(ExcludeOwner = true)]
    void RpcRotateVisual(Vector3 direction)
    {
        _netController?.RotateVisualPublic(FacingDir(direction));
    }

    // ── Server-side skill execution ───────────────────────────────────────────

    [ServerRpc]
    void ServerCastSkill(SkillType skill, Vector3 spawnPos, Vector3 aimDir, float aimDist, int manaCost)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;
        if (stats.currentMana < manaCost) return;
        stats.SpendMana(manaCost);

        switch (skill)
        {
            case SkillType.SingleShot:    ServerSingleShot(spawnPos, aimDir);    break;
            case SkillType.TripleShot:    ServerTripleShot(spawnPos, aimDir);    break;
            case SkillType.AutomaticFire: ServerAutoFire(spawnPos, aimDir);      break;
            case SkillType.OrbitalStrike: ServerOrbitalStrike(aimDir, aimDist);  break;
        }
    }

    // ── Single Shot ───────────────────────────────────────────────────────────

    void ServerSingleShot(Vector3 spawnPos, Vector3 aimDir)
    {
        if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
        float life = bulletRange / bulletSpeed;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, aimDir);
        SpawnBullet(spawnPos, rot, aimDir, life);

        PlayerStats stats = GetComponent<PlayerStats>();
        StartCoroutine(SingleShotHitCheck(spawnPos, aimDir, bulletRange, stats));
    }

    void ApplyKnockback(EnemyHealth enemy, Vector3 dir, float force)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(new Vector3(dir.x, 0f, dir.z).normalized * force, ForceMode.Impulse);
    }

    IEnumerator SingleShotHitCheck(Vector3 origin, Vector3 dir, float range, PlayerStats stats)
    {
        float elapsed = 0f;
        float life    = range / bulletSpeed;
        Vector3 pos   = origin;
        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            pos     += dir * bulletSpeed * Time.deltaTime;

            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth e in enemies)
            {
                if (hit.Contains(e) || e.IsDead()) continue;
                if (Vector3.Distance(pos, e.transform.position) < 0.8f)
                {
                    int dmg = stats != null ? stats.GetRangedDamage(25) : 25;
                    dmg = stats != null ? stats.ApplyCriticalDamage(dmg) : dmg;
                    e.ReceiveDamage(dmg, DamageType.Ranged, stats);
                    ApplyKnockback(e, dir, bulletKnockback);
                    hit.Add(e);
                    yield break; // single shot stops on first hit
                }
            }
            yield return null;
        }
    }

    // ── Triple Shot ───────────────────────────────────────────────────────────

    void ServerTripleShot(Vector3 spawnPos, Vector3 aimDir)
    {
        if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
        StartCoroutine(TripleShotCoroutine(spawnPos, aimDir));
    }

    IEnumerator TripleShotCoroutine(Vector3 spawnPos, Vector3 aimDir)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        float life = bulletRange / bulletSpeed;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, aimDir);

        for (int i = 0; i < 3; i++)
        {
            _modelAnimator?.SetTrigger("Shoot");
            Vector3 origin = transform.position + Vector3.up * effectHeightOffset;
            SpawnBullet(origin, rot, aimDir, life);
            StartCoroutine(BulletHitCheck(origin, aimDir, bulletRange, stats, true));
            yield return new WaitForSeconds(tripleFireRate);
        }
    }

    // ── Automatic Fire ────────────────────────────────────────────────────────

    void ServerAutoFire(Vector3 spawnPos, Vector3 aimDir)
    {
        if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;

        if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;

        float life = bulletRange / bulletSpeed;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, aimDir);
        SpawnBullet(spawnPos, rot, aimDir, life);
        StartCoroutine(BulletHitCheck(spawnPos, aimDir, bulletRange, GetComponent<PlayerStats>(), false));
    }

    // ── Orbital Strike ────────────────────────────────────────────────────────

    void ServerOrbitalStrike(Vector3 aimDir, float aimDist)
    {
        float range    = Mathf.Min(aimDist, orbitalCastRange);
        Vector3 target = transform.position + aimDir * range;
        target.y       = 0f;

        int   skillLevel  = _skillManager != null ? Mathf.Max(1, _skillManager.GetSkillLevel(SkillType.OrbitalStrike)) : 1;
        float warningTime = Mathf.Max(orbitalWarningTimeMin,
                                orbitalWarningTimeBase - (skillLevel - 1) *
                                ((orbitalWarningTimeBase - orbitalWarningTimeMin) / 9f));
        int baseDamage = orbitalBaseDamage + (skillLevel - 1) * orbitalDamagePerLevel;

        SpawnOrbitalWarning(target, warningTime, orbitalRadius);
        StartCoroutine(OrbitalStrikeImpact(target, warningTime, baseDamage, GetComponent<PlayerStats>()));
    }

    IEnumerator OrbitalStrikeImpact(Vector3 target, float delay, int baseDamage, PlayerStats stats)
    {
        yield return new WaitForSeconds(delay);

        SpawnOrbitalImpact(target);

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth e in enemies)
        {
            if (e == null || e.IsDead()) continue;
            float dist = Vector3.Distance(new Vector3(e.transform.position.x, target.y, e.transform.position.z), target);
            if (dist > orbitalRadius) continue;

            int dmg = stats != null ? stats.GetRangedDamage(baseDamage) : baseDamage;
            dmg = stats != null ? stats.ApplyCriticalDamage(dmg) : dmg;
            e.ReceiveDamage(dmg, DamageType.Ranged, stats);
        }
    }

    // ── Shared bullet hit check ───────────────────────────────────────────────

    IEnumerator BulletHitCheck(Vector3 origin, Vector3 dir, float range,
                                PlayerStats stats, bool stopOnFirstHit)
    {
        float elapsed = 0f;
        float life    = range / bulletSpeed;
        Vector3 pos   = origin;
        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            pos     += dir * bulletSpeed * Time.deltaTime;

            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth e in enemies)
            {
                if (hit.Contains(e) || e.IsDead()) continue;
                if (Vector3.Distance(pos, e.transform.position) < 0.8f)
                {
                    int dmg = stats != null ? stats.GetRangedDamage(25) : 25;
                    dmg = stats != null ? stats.ApplyCriticalDamage(dmg) : dmg;
                    e.ReceiveDamage(dmg, DamageType.Ranged, stats);
                    ApplyKnockback(e, dir, bulletKnockback);
                    hit.Add(e);
                    if (stopOnFirstHit) yield break;
                }
            }
            yield return null;
        }
    }

    // ── ObserversRpc Visual Spawners ──────────────────────────────────────────

    [ObserversRpc]
    void SpawnBullet(Vector3 pos, Quaternion rot, Vector3 dir, float lifetime)
    {
        if (bulletPrefab == null) return;
        GameObject go = Instantiate(bulletPrefab, pos, rot);
        SimpleProjectileVisual vis = go.GetComponent<SimpleProjectileVisual>();
        if (vis == null) vis = go.AddComponent<SimpleProjectileVisual>();
        vis.speed    = bulletSpeed;
        vis.lifetime = lifetime;
        vis.SetDirection(dir);
    }

    [ObserversRpc]
    void SpawnOrbitalWarning(Vector3 center, float duration, float radius)
    {
        if (orbitalWarningPrefab == null) return;
        // Start on the perimeter
        Vector3 startPos = center + new Vector3(radius, 0f, 0f);
        GameObject go = Instantiate(orbitalWarningPrefab, startPos, Quaternion.identity);

        OrbitalStrikeWarning warning = go.GetComponent<OrbitalStrikeWarning>();
        if (warning == null) warning = go.AddComponent<OrbitalStrikeWarning>();
        warning.center   = center;
        warning.radius   = radius;
        warning.duration = duration;
    }

    [ObserversRpc]
    void SpawnOrbitalImpact(Vector3 pos)
    {
        if (orbitalImpactPrefab == null) return;
        GameObject go = Instantiate(orbitalImpactPrefab, pos, Quaternion.identity);
        OrbitalImpactExpand expand = go.GetComponent<OrbitalImpactExpand>();
        if (expand == null) expand = go.AddComponent<OrbitalImpactExpand>();
        expand.startScale  = 0.1f;
        expand.targetScale = 1f;
    }

    WeaponFollowBone GetWeaponFollow()
    {
        if (_equipmentManager == null) return null;
        GameObject visual = _equipmentManager.GetSpawnedVisual(_equipmentManager.rightWeaponSlot);
        return visual != null ? visual.GetComponent<WeaponFollowBone>() : null;
    }

    void ApplyWeaponCastRotation()
    {
        WeaponFollowBone f = GetWeaponFollow();
        if (f != null) f.isShooting = true;
    }

    void RestoreWeaponRotation()
    {
        WeaponFollowBone f = GetWeaponFollow();
        if (f != null) f.isShooting = false;
    }

    void PlayCastAnimation(SkillType skill)
    {
        if (_modelAnimator == null) return;
        switch (skill)
        {
            case SkillType.AutomaticFire:
                break; // handled by IsAutoFiring bool
            case SkillType.OrbitalStrike:
                _modelAnimator.SetTrigger("OrbitalStrike");
                break;
            default:
                _modelAnimator.SetTrigger("Shoot");
                break;
        }
    }

    Vector3 GetAimDirection(out float distance)
    {
        distance = orbitalCastRange;
        if (Camera.main == null) return transform.forward;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 target = ray.GetPoint(dist);
            Vector3 dir    = target - transform.position;
            dir.y = 0f;
            distance = dir.magnitude;
            return dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
        }
        return transform.forward;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    int GetManaCost(SkillType skill)
    {
        return skill switch
        {
            SkillType.SingleShot    => singleShotMana,
            SkillType.TripleShot    => tripleShotMana,
            SkillType.AutomaticFire => autoFireMana,
            SkillType.OrbitalStrike => orbitalStrikeMana,
            _                       => 10,
        };
    }

    float GetCooldown(SkillType skill)
    {
        return skill switch
        {
            SkillType.SingleShot    => singleShotCooldown,
            SkillType.TripleShot    => tripleShotCooldown,
            SkillType.AutomaticFire => autoFireCooldown,
            SkillType.OrbitalStrike => orbitalStrikeCooldown,
            _                       => 3f,
        };
    }
}
