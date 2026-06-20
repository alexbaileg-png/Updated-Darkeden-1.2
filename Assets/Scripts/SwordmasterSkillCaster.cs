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

    [Header("Skill Cooldowns")]
    public float judgmentRushCooldown  = 6f;
    public float consecrationCooldown  = 8f;
    public float aegisCooldown         = 15f;
    public float radiantFlurryCooldown = 10f;

    [Header("Skill Mana Costs")]
    public int judgmentRushMana  = 15;
    public int consecrationMana  = 20;
    public int aegisMana         = 25;
    public int radiantFlurryMana = 20;

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

    public float GetCooldownRemaining()   => Mathf.Max(0f, GetNextCastTime(_selectedSkill) - Time.time);
    public float GetCurrentSkillCooldown() => GetCooldown(_selectedSkill);

    float GetNextCastTime(SkillType skill) => _nextCastTimePerSkill.TryGetValue(skill, out float t) ? t : 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SkillType _selectedSkill;
    private readonly Dictionary<SkillType, float> _nextCastTimePerSkill = new Dictionary<SkillType, float>();
    private bool  _isCasting     = false;

    private PlayerStats             _playerStats;
    private PlayerSkillManager      _skillManager;
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

        // Rotate toward aim before playing animation
        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
        {
            _netController?.RotateVisualPublic(aimDir.normalized);
            ServerRotateToward(aimDir.normalized);
        }

        PlayCastAnimation();
        yield return new WaitForSeconds(castDelay);

        int   manaCost = GetManaCost(skill);
        float cooldown = GetCooldown(skill);

        ServerCastSkill(skill, transform.position, aimDir.normalized, manaCost);

        _nextCastTimePerSkill[skill] = Time.time + cooldown;

        // For skills with movement, keep _isCasting true until the movement finishes
        float lockDuration = GetPostCastLockDuration(skill);
        if (lockDuration > 0f)
            yield return new WaitForSeconds(lockDuration);

        _isCasting = false;
    }

    float GetPostCastLockDuration(SkillType skill)
    {
        if (skill == SkillType.RadiantFlurry)
            return flurryMaxTargets * flurryHitInterval;
        return 0f;
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
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir, int manaCost)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;
        if (stats.currentMana < manaCost) return;
        stats.SpendMana(manaCost);

        switch (skill)
        {
            case SkillType.JudgmentRush:  ServerJudgmentRush(casterPos, aimDir); break;
            case SkillType.Consecration:  ServerConsecration(casterPos);          break;
            case SkillType.AegisOfFaith:  ServerAegisOfFaith();                   break;
            case SkillType.RadiantFlurry: ServerRadiantFlurry(casterPos);         break;
        }
    }

    // ── Judgment Rush ─────────────────────────────────────────────────────────

    void ServerJudgmentRush(Vector3 casterPos, Vector3 aimDir)
    {
        if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;

        Vector3 endPos = casterPos + aimDir * rushDistance;

        // Teleport player to end of rush
        transform.position = endPos;

        // Spawn VFX at midpoint of the dash
        Vector3 rushFxPos = casterPos + aimDir * (rushDistance * 0.5f);
        rushFxPos.y = rushEffectHeightOffset;
        RpcSpawnJudgmentRushEffect(rushFxPos, Quaternion.LookRotation(aimDir), rushDistance, -1f);

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

            int damage = stats != null ? stats.GetMeleeDamage(30) : 30;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Melee, stats);

            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(aimDir * rushKnockback, ForceMode.Impulse);
        }
    }

    // ── Consecration ─────────────────────────────────────────────────────────

    void ServerConsecration(Vector3 casterPos)
    {
        RpcSpawnConsecrationEffect(casterPos);

        PlayerStats stats = GetComponent<PlayerStats>();
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            float dist = Vector3.Distance(
                new Vector3(enemy.transform.position.x, casterPos.y, enemy.transform.position.z),
                casterPos);
            if (dist > consecrationRadius) continue;

            int damage = stats != null ? stats.GetMagicalSkillDamage(30) : 30;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            enemy.ReceiveDamage(damage, DamageType.Magical, stats);
        }
    }

    // ── Aegis of Faith ────────────────────────────────────────────────────────

    void ServerAegisOfFaith()
    {
        float duration = aegisBaseDuration;

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

        _aegisArmor      = aegisArmorBonus;
        _aegisResistance = aegisResistBonus;
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

    void ServerRadiantFlurry(Vector3 casterPos)
    {
        StartCoroutine(RadiantFlurryCoroutine(casterPos));
    }

    IEnumerator RadiantFlurryCoroutine(Vector3 casterPos)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        _netController?.ServerStopMovement();

        Vector3 currentPos = transform.position;
        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();

        for (int i = 0; i < flurryMaxTargets; i++)
        {
            // Find nearest unhit living enemy within radius of current position
            EnemyHealth next  = null;
            float       bestDist = flurryRadius;
            foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead() || hit.Contains(enemy)) continue;
                float dist = Vector3.Distance(
                    new Vector3(enemy.transform.position.x, currentPos.y, enemy.transform.position.z),
                    currentPos);
                if (dist < bestDist) { bestDist = dist; next = enemy; }
            }

            if (next == null) break;
            hit.Add(next);

            Vector3 enemyPos = next.transform.position;
            Vector3 flatDir  = new Vector3(enemyPos.x - currentPos.x, 0f, enemyPos.z - currentPos.z);
            if (flatDir.sqrMagnitude > 0.01f) flatDir.Normalize();
            else flatDir = transform.forward;

            RpcRotateVisualAll(flatDir);

            Vector3 stopPos = enemyPos - flatDir * 1.2f;
            stopPos.y = currentPos.y;

            float travelDist    = Mathf.Max(Vector3.Distance(currentPos, stopPos), 1f);
            float glideDuration = flurryHitInterval * 0.6f;
            Vector3 effectCenter = new Vector3(
                (currentPos.x + stopPos.x) * 0.5f,
                flurryEffectHeightOffset,
                (currentPos.z + stopPos.z) * 0.5f);
            RpcSpawnJudgmentRushEffect(effectCenter, Quaternion.LookRotation(flatDir), travelDist, 0.4f);

            RpcFlurryGlide(currentPos, stopPos, glideDuration);
            transform.position = stopPos;
            currentPos = stopPos;

            yield return new WaitForSeconds(glideDuration);

            int damage = stats != null ? stats.GetMeleeDamage(30) : 30;
            damage = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
            next.ReceiveDamage(damage, DamageType.Melee, stats);

            yield return new WaitForSeconds(flurryHitInterval * 0.4f);
        }
    }

    [ObserversRpc]
    void RpcFlurryGlide(Vector3 from, Vector3 to, float duration)
    {
        StartCoroutine(FlurryGlideCoroutine(from, to, duration));
    }

    IEnumerator FlurryGlideCoroutine(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = to;
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

    int GetManaCost(SkillType skill)
    {
        return skill switch
        {
            SkillType.JudgmentRush  => judgmentRushMana,
            SkillType.Consecration  => consecrationMana,
            SkillType.AegisOfFaith  => aegisMana,
            SkillType.RadiantFlurry => radiantFlurryMana,
            _                       => 10,
        };
    }

    float GetCooldown(SkillType skill)
    {
        return skill switch
        {
            SkillType.JudgmentRush  => judgmentRushCooldown,
            SkillType.Consecration  => consecrationCooldown,
            SkillType.AegisOfFaith  => aegisCooldown,
            SkillType.RadiantFlurry => radiantFlurryCooldown,
            _                       => 3f,
        };
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
