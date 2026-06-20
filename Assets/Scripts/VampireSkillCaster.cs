using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all vampire bloodline skills.
/// Attach this to the PlayerNetworkPrefabVampire prefab.
/// </summary>
public class VampireSkillCaster : NetworkBehaviour, ISkillCaster
{
    // Each F-key holds a list of skills — pressing the key cycles through them
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

    [Header("Dark Bolt")]
    public GameObject darkBoltPrefab;

    [Header("Blood Drain")]
    public GameObject bloodDrainBeamPrefab;
    public float bloodDrainDuration        = 1.2f;
    public float bloodDrainTickRate        = 0.3f;
    public float bloodDrainHealPercent     = 0.4f;

    [Header("Blood Fog")]
    public GameObject bloodFogPrefab;
    public float bloodFogBaseRadius    = 8f;
    public float bloodFogBaseDuration  = 20f;
    public float bloodFogDurationPerInt = 0.5f; // extra seconds per INT above base (18)
    public int   bloodFogBaseInt       = 18;

    [Header("Void Burst")]
    public GameObject voidBurstEffectPrefab;
    public float voidBurstBaseRadius     = 5f;
    public float voidBurstRadiusPerLevel = 0.3f;

    [Header("Bloody Talons")]
    public GameObject bloodyTalonsPrefab;
    public float talonsConeAngle         = 120f;
    public float talonsRange             = 4f;
    public int   talonsMaxTargets        = 10;
    public float talonsForwardOffset     = 1.5f;
    public float talonsHeightOffset      = 0.8f;
    public float talonsMissHeightOffset  = 1.2f;

    [Header("Cast Settings")]
    public float castDelay        = 0.1f;
    public float postCastLockTime = 0.6f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SkillType _selectedSkill;
    private readonly Dictionary<SkillType, float> _nextCastTimePerSkill = new Dictionary<SkillType, float>();
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

        int idx = _keyIndex[key];
        idx = (idx + 1) % list.Count;
        _keyIndex[key] = idx;
        _selectedSkill = list[idx];
    }

    void HandleCasting()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (_isCasting || _isChanneling) return;
        if (!CanCast(_selectedSkill)) return;

        StartCoroutine(CastRoutine(_selectedSkill));
    }

    bool CanCast(SkillType skill)
    {
        if (Time.time < GetNextCastTime(skill)) return false;
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null && !ClassSkillConfig.CanUseSkill(character, skill)) return false;
        if (_skillManager != null && !_skillManager.IsSkillUnlocked(skill)) return false;
        int cost = GetManaCost(skill);
        if (_playerStats != null && _playerStats.currentMana < cost) return false;
        return true;
    }

    float GetNextCastTime(SkillType skill) => _nextCastTimePerSkill.TryGetValue(skill, out float t) ? t : 0f;

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;
        _netController?.ServerStopMovement();

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

        int skillLevel = GetSkillLevel(skill);
        int skillPower = GetSkillPower(skill, 25);
        int manaCost   = GetManaCost(skill);
        Vector3 castPos = GetMouseWorldPosition();

        ServerCastSkill(skill, transform.position, aimDir, castPos, skillLevel, skillPower, manaCost);

        // Blood Drain blocks casting and movement for the full channel duration
        if (skill == SkillType.BloodDrain)
        {
            _isChanneling = true;
            float elapsed = 0f;
            while (elapsed < bloodDrainDuration)
            {
                _netController?.ServerStopMovement();
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            _isChanneling = false;
        }
        else
        {
            yield return new WaitForSeconds(postCastLockTime);
        }

        _nextCastTimePerSkill[skill] = Time.time + GetCooldown(skill);
        _isCasting = false;
    }

    // ── Server RPC ────────────────────────────────────────────────────────────

    [ServerRpc]
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir,
                         Vector3 castPos, int skillLevel, int skillPower, int manaCost)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && !stats.SpendMana(manaCost)) return;

        switch (skill)
        {
            case SkillType.DarkBolt:     ServerDarkBolt(casterPos, aimDir, skillPower); break;
            case SkillType.BloodDrain:   ServerStartBloodDrain(castPos, skillLevel, skillPower); break;
            case SkillType.BloodFog:     ServerBloodFog(castPos, skillLevel); break;
            case SkillType.VoidBurst:    ServerVoidBurst(casterPos, skillLevel, skillPower); break;
            case SkillType.BloodyTalons: ServerBloodyTalons(casterPos, aimDir, skillLevel, skillPower); break;
        }
    }

    // ── Bloody Talons ─────────────────────────────────────────────────────────

    void ServerBloodyTalons(Vector3 casterPos, Vector3 aimDir, int skillLevel, int skillPower)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
        damage     = stats != null ? stats.ApplyCriticalDamage(damage) : damage;

        float range    = talonsRange + (skillLevel - 1) * 0.3f;
        float halfCone = talonsConeAngle * 0.5f;
        int   hits     = 0;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (hits >= talonsMaxTargets) break;
            if (enemy == null || enemy.IsDead()) continue;

            Vector3 toEnemy = enemy.transform.position - casterPos;
            toEnemy.y = 0f;
            if (toEnemy.magnitude > range) continue;
            if (Vector3.Angle(aimDir, toEnemy.normalized) > halfCone) continue;

            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            Vector3 fxPos = enemy.transform.position;
            fxPos.y = talonsHeightOffset;
            SpawnTalonsEffect(fxPos, Quaternion.LookRotation(aimDir));
            hits++;
        }

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
    public float GetCooldownRemaining()        => Mathf.Max(0f, GetNextCastTime(_selectedSkill) - Time.time);
    public float GetCurrentSkillCooldown()     => GetCooldown(_selectedSkill);
    public void  SetSelectedSkill(SkillType s) => _selectedSkill = s;

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
        // Select the first bound skill so the preview shows immediately
        foreach (var kvp in _keyBindings)
            if (kvp.Value.Count > 0) { _selectedSkill = kvp.Value[0]; break; }
    }

    public void  BindSkill(KeyCode key, SkillType skill)
    {
        if (!_keyBindings.ContainsKey(key)) return;
        List<SkillType> list = _keyBindings[key];

        if (list.Contains(skill))
            list.Remove(skill); // toggle off if already bound
        else
        {
            list.Add(skill);
            // immediately select the newly bound skill and reset cycle index to it
            _keyIndex[key] = list.Count - 1;
            _selectedSkill = skill;
        }
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
