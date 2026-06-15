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

    // ── Judgment Rush ─────────────────────────────────────────────────────────
    [Header("Judgment Rush")]
    public GameObject judgmentRushEffectPrefab;   // VFX spawned along dash path
    public float rushDistance         = 10f;
    public float rushSpeed            = 30f;
    public float rushKnockback        = 3f;
    public float rushWidth            = 1.2f;
    public float rushEffectHeightOffset = -0.5f;  // tune in play mode

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
    public float flurryRadius          = 4f;
    public int   flurryMaxTargets      = 5;
    public float flurryHitInterval     = 0.15f;   // seconds between each hit
    public float flurryEffectHeightOffset = 0.3f; // tune in play mode

    // ── Shared ────────────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float castDelay = 0.1f;

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

    public float GetCooldownRemaining()   => Mathf.Max(0f, _nextCastTime - Time.time);
    public float GetCurrentSkillCooldown() => _lastCooldown;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SkillType _selectedSkill;
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
        _netController?.ServerStopMovement();

        // Rotate toward aim before playing animation
        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
        {
            _netController?.RotateVisualPublic(aimDir.normalized);
            ServerRotateToward(aimDir.normalized);
        }

        PlayCastAnimation();
        yield return new WaitForSeconds(castDelay);

        int   skillLevel = GetSkillLevel(skill);
        int   skillPower = GetSkillPower(skill, 30);
        int   manaCost   = GetManaCost(skill);
        float cooldown   = GetCooldown(skill);

        ServerCastSkill(skill, transform.position, aimDir.normalized, skillLevel, skillPower, manaCost);

        _lastCooldown = cooldown;
        _nextCastTime = Time.time + cooldown;

        // For skills with movement, keep _isCasting true until the movement finishes
        float lockDuration = GetPostCastLockDuration(skill, skillLevel);
        if (lockDuration > 0f)
            yield return new WaitForSeconds(lockDuration);

        _isCasting = false;
    }

    float GetPostCastLockDuration(SkillType skill, int skillLevel)
    {
        switch (skill)
        {
            case SkillType.RadiantFlurry:
                int maxTargets = flurryMaxTargets + (skillLevel - 1);
                return maxTargets * flurryHitInterval;
            default:
                return 0f;
        }
    }

    // ── Rotation RPCs ─────────────────────────────────────────────────────────

    [ServerRpc]
    void ServerRotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            RpcRotateVisual(direction);
    }

    // Used for initial cast rotation — owner already rotated locally so excluded
    [ObserversRpc(ExcludeOwner = true)]
    void RpcRotateVisual(Vector3 direction)
    {
        _netController?.RotateVisualPublic(direction);
    }

    // Used during Radiant Flurry — owner hasn't rotated yet so include everyone
    [ObserversRpc]
    void RpcRotateVisualAll(Vector3 direction)
    {
        _netController?.RotateVisualPublic(direction);
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
        Vector3 rushFxPos = casterPos + aimDir * (distance * 0.5f);
        rushFxPos.y = rushEffectHeightOffset;
        RpcSpawnJudgmentRushEffect(rushFxPos, Quaternion.LookRotation(aimDir), distance, -1f);

        // Damage + knockback: XZ distance from enemy to the dash line segment
        PlayerStats stats = GetComponent<PlayerStats>();
        Vector2 lineStart = new Vector2(casterPos.x, casterPos.z);
        Vector2 lineEnd   = new Vector2(endPos.x,   endPos.z);
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead()) continue;

            Vector2 ePos = new Vector2(enemy.transform.position.x, enemy.transform.position.z);
            float distToLine = PointToSegmentDistance(ePos, lineStart, lineEnd);
            if (distToLine > rushWidth) continue;

            int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(aimDir * rushKnockback, ForceMode.Impulse);
        }
    }

    // ── Consecration ─────────────────────────────────────────────────────────

    void ServerConsecration(Vector3 casterPos, int skillLevel, int skillPower)
    {
        float radius = consecrationRadius + (skillLevel - 1) * 0.5f;

        RpcSpawnConsecrationEffect(casterPos);

        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead()) continue;

            float dist = Vector3.Distance(
                new Vector3(enemy.transform.position.x, casterPos.y, enemy.transform.position.z),
                casterPos);
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

        RpcSpawnAegisEffect(duration);

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
        float radius   = flurryRadius + (skillLevel - 1) * 0.6f;

        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        List<EnemyHealth> targets = new List<EnemyHealth>();
        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            float dist = Vector3.Distance(
                new Vector3(enemy.transform.position.x, casterPos.y, enemy.transform.position.z),
                casterPos);
            if (dist <= radius)
                targets.Add(enemy);
            if (targets.Count >= maxTargets) break;
        }

        Vector3 currentPos = transform.position;
        _netController?.ServerStopMovement();

        foreach (EnemyHealth enemy in targets)
        {
            if (enemy == null || enemy.IsDead()) continue;

            Vector3 enemyPos = enemy.transform.position;
            Vector3 flatDir  = new Vector3(enemyPos.x - currentPos.x, 0f, enemyPos.z - currentPos.z);
            if (flatDir.sqrMagnitude > 0.01f) flatDir.Normalize();
            else flatDir = transform.forward;

            // Rotate all clients (including owner) toward this enemy
            RpcRotateVisualAll(flatDir);

            // Stop 1.2m in front of the enemy, keep current height
            Vector3 stopPos = enemyPos - flatDir * 1.2f;
            stopPos.y = currentPos.y;

            // Trail effect: centered between start and stop at ground level
            float travelDist = Mathf.Max(Vector3.Distance(currentPos, stopPos), 1f);
            Vector3 effectCenter = new Vector3(
                (currentPos.x + stopPos.x) * 0.5f,
                flurryEffectHeightOffset,
                (currentPos.z + stopPos.z) * 0.5f);
            RpcSpawnJudgmentRushEffect(effectCenter, Quaternion.LookRotation(flatDir), travelDist, 0.4f);

            // Smooth glide
            Vector3 glideStart    = transform.position;
            float   glideDuration = flurryHitInterval * 0.6f;
            float   elapsed       = 0f;
            while (elapsed < glideDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(glideStart, stopPos, Mathf.Clamp01(elapsed / glideDuration));
                yield return null;
            }
            transform.position = stopPos;
            currentPos = stopPos;

            // Damage
            int damage = stats != null ? stats.GetMeleeDamage(skillPower) : skillPower;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            // Brief pause before next target
            yield return new WaitForSeconds(flurryHitInterval * 0.4f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    [ObserversRpc]
    void RpcSpawnJudgmentRushEffect(Vector3 pos, Quaternion rot, float length, float height)
    {
        if (judgmentRushEffectPrefab == null) return;
        GameObject fx = Instantiate(judgmentRushEffectPrefab, pos, rot);
        // Stop any Play On Awake particles — burst effects removed for now
        foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        JudgmentRushEffect fxComp = fx.GetComponent<JudgmentRushEffect>();
        if (fxComp != null)
        {
            if (length >= 0f) fxComp.rushDistance = length;
            if (height >= 0f) fxComp.heightScale  = height;
        }
    }

    [ObserversRpc]
    void RpcSpawnConsecrationEffect(Vector3 pos)
    {
        if (consecrationEffectPrefab == null) return;
        Instantiate(consecrationEffectPrefab, pos, Quaternion.identity);
    }

    [ObserversRpc]
    void RpcSpawnAegisEffect(float duration)
    {
        if (aegisShieldEffectPrefab == null) return;
        GameObject go = Instantiate(aegisShieldEffectPrefab, transform.position, Quaternion.identity);
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, -2.25f, 0f);
        go.transform.localRotation = Quaternion.identity;
        AegisShieldEffect fx = go.GetComponent<AegisShieldEffect>();
        if (fx != null) fx.duration = duration;
        else Destroy(go, duration); // fallback if script not on prefab
    }

    [ObserversRpc]
    void RpcSpawnRadiantHitEffect(Vector3 pos)
    {
        if (radiantHitEffectPrefab == null) return;
        Instantiate(radiantHitEffectPrefab, pos, Quaternion.identity);
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

    static float PointToSegmentDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(point, a);
        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lenSq);
        return Vector2.Distance(point, a + t * ab);
    }
}
