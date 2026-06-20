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

    // ── Holy Bolt ─────────────────────────────────────────────────────────────
    [Header("Holy Bolt")]
    public GameObject holyBoltProjectilePrefab;
    public float boltSpeed    = 18f;
    public float boltRange    = 20f;
    // For elongated/oblong meshes — corrects the model's facing so it travels point-first instead of sideways.
    public Vector3 holyBoltModelRotationOffset = new Vector3(90f, 0f, 0f);

    // ── Holy Rain ─────────────────────────────────────────────────────────────
    [Header("Holy Rain")]
    public GameObject holyRainEffectPrefab;     // falling object spawned above the AoE
    public GameObject holyRainImpactPrefab;     // ground-impact hole sprite/decal
    public int   holyRainObjectCount = 10;      // how many falling objects spawn
    public float holyRainFallHeight  = 8f;      // how high above the ground they start
    public float holyRainFallSpeed   = 20f;     // fall speed of each object
    public int   holyRainHitCount    = 10;      // number of small damage ticks dealt
    public float holyRainImpactLifetime = 4f;   // how long the ground-impact decal stays before despawning
    public float holyRainImpactHeightOffset = 0.02f; // vertical offset applied to the decal spawn position
    // Lays the decal flat on the ground — adjust if the sprite still appears upright/sideways.
    public Vector3 holyRainImpactRotationOffset = Vector3.zero;
    public float holyRainAllLandWithin = 1f;    // every falling object must land within this many seconds of cast
    public float rainRadius    = 1.5f;
    public float rainDuration  = 3f;
    public float rainCastRange = 12f;    // how far from the player the rain lands

    // ── Holy Circle Heal ──────────────────────────────────────────────────────
    [Header("Holy Circle Heal")]
    public GameObject holyCircleEffectPrefab;
    public float circleRadius    = 5f;
    public float circleCastRange = 12f;

    // ── Healing Orbit ─────────────────────────────────────────────────────────
    [Header("Healing Orbit")]
    public GameObject orbitEffectPrefab;
    public float orbitDuration   = 10f;
    public float orbitRadius     = 1.5f; // visual orbit distance and AOE heal range
    public float orbitHealAmount = 15f;  // heal per tick while active
    public float orbitTickRate   = 1f;
    public int   orbitMaxStacks  = 3;    // max simultaneous orbits allowed

    int _activeOrbitCount = 0;

    // ── Cast Settings ─────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float effectHeightOffset   = 0.8f;
    public float holyBoltDelay        = 0.2f;
    public float holyRainDelay        = 0.4f;
    public float holyCircleHealDelay  = 0.4f;
    public float healingOrbitDelay    = 0.3f;

    [Header("Skill Cooldowns")]
    public float holyBoltCooldown      = 1f;
    public float holyRainCooldown      = 8f;
    public float holyCircleCooldown    = 6f;
    public float healingOrbitCooldown  = 12f;

    [Header("Skill Mana Costs")]
    public int holyBoltManaCost      = 5;
    public int holyRainManaCost      = 20;
    public int holyCircleManaCost    = 15;
    public int healingOrbitManaCost  = 25;

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

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;
        _netController?.ServerStopMovement();

        int   manaCost   = GetManaCost(skill);
        float cooldown   = GetCooldown(skill);

        Vector3 aimDir = GetAimDirection(out float aimDistance);
        if (aimDir.sqrMagnitude > 0.01f)
        {
            _netController?.RotateVisualPublic(aimDir.normalized);
            ServerRotateToward(aimDir.normalized);
        }

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

        ServerCastSkill(skill, transform.position, aimDir.normalized, aimDistance, manaCost);

        _nextCastTimePerSkill[skill] = Time.time + cooldown;
        _isCasting    = false;
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
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir, float aimDistance, int manaCost)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;
        if (stats.currentMana < manaCost) return;
        stats.SpendMana(manaCost);

        switch (skill)
        {
            case SkillType.HolyBolt:       ServerHolyBolt(casterPos, aimDir);              break;
            case SkillType.HolyRain:       ServerHolyRain(casterPos, aimDir, aimDistance); break;
            case SkillType.HolyCircleHeal: ServerHolyCircleHeal(casterPos, aimDir, aimDistance); break;
            case SkillType.HealingOrbit:   ServerHealingOrbit();                            break;
        }
    }

    // ── Holy Bolt ─────────────────────────────────────────────────────────────
    // Fires a projectile toward the aimed direction that damages enemies on hit

    void ServerHolyBolt(Vector3 casterPos, Vector3 aimDir)
    {
        if (holyBoltProjectilePrefab == null) return;

        Vector3    spawnPos = casterPos + Vector3.up * effectHeightOffset;
        Quaternion spawnRot = Quaternion.LookRotation(aimDir);

        SpawnHolyBoltEffect(spawnPos, spawnRot);

        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = stats != null ? stats.GetMagicalSkillDamage(25) : 25;
        damage     = stats != null ? stats.ApplyCriticalDamage(damage) : damage;

        GameObject proj = new GameObject("HolyBoltServer");
        proj.transform.SetPositionAndRotation(spawnPos, spawnRot);
        HolyBoltProjectile bolt = proj.AddComponent<HolyBoltProjectile>();
        bolt.speed      = boltSpeed;
        bolt.range      = boltRange;
        bolt.damage     = damage;
        bolt.damageType = DamageType.Magical;
        bolt.caster     = stats;
    }

    // ── Holy Rain ─────────────────────────────────────────────────────────────
    // Places a healing rain AoE — heals nearby players over time

    void ServerHolyRain(Vector3 casterPos, Vector3 aimDir, float aimDistance)
    {
        Vector3 center = casterPos + aimDir * Mathf.Min(aimDistance, rainCastRange);
        center.y = casterPos.y;

        SpawnHolyRainEffect(center, rainRadius, rainDuration);
        StartCoroutine(HolyRainTick(center, rainRadius, holyRainAllLandWithin));
    }

    IEnumerator HolyRainTick(Vector3 center, float radius, float duration)
    {
        int   hitCount     = Mathf.Max(1, holyRainHitCount);
        float interval     = duration / hitCount;
        PlayerStats stats  = GetComponent<PlayerStats>();
        int   damagePerHit = Mathf.Max(1, stats != null ? stats.GetMagicalSkillDamage(25) / hitCount : 5);

        for (int i = 0; i < hitCount; i++)
        {
            yield return new WaitForSeconds(interval);

            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null || enemy.IsDead()) continue;
                float dist = Vector3.Distance(center, enemy.transform.position);
                if (dist <= radius) enemy.ReceiveDamage(damagePerHit, DamageType.Magical, stats);
            }
        }
    }

    // ── Holy Circle Heal ──────────────────────────────────────────────────────
    // Instantly heals all players in a circle around the caster

    void ServerHolyCircleHeal(Vector3 casterPos, Vector3 aimDir, float aimDistance)
    {
        Vector3 center = casterPos + aimDir * Mathf.Min(aimDistance, circleCastRange);
        center.y = casterPos.y;

        PlayerStats stats = GetComponent<PlayerStats>();
        int healAmt = stats != null ? stats.GetMagicalSkillDamage(25) * 2 : 50;

        SpawnCircleHealEffect(center);

        PlayerStats[] allPlayers = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        foreach (PlayerStats player in allPlayers)
        {
            if (player == null) continue;
            float dist = Vector3.Distance(center, player.transform.position);
            if (dist <= circleRadius)
                player.Heal(healAmt);
        }
    }

    // ── Healing Orbit ─────────────────────────────────────────────────────────
    // Summons orbiting lights that heal the caster over time

    void ServerHealingOrbit()
    {
        if (_activeOrbitCount >= orbitMaxStacks) return;

        _activeOrbitCount++;
        SpawnOrbitEffect(transform.position, orbitDuration, orbitRadius);
        StartCoroutine(HealingOrbitTick(orbitDuration, (int)orbitHealAmount));
    }

    IEnumerator HealingOrbitTick(float duration, int healAmt)
    {
        float elapsed = 0f;
        PlayerStats caster = GetComponent<PlayerStats>();

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(orbitTickRate);
            elapsed += orbitTickRate;

            if (caster != null) caster.Heal(healAmt);

            // TODO: heal friendly Slayers within orbitRadius
        }

        _activeOrbitCount--;
    }

    // ── ObserversRpc Effect Spawners ──────────────────────────────────────────

    [ObserversRpc]
    void SpawnHolyBoltEffect(Vector3 pos, Quaternion rot)
    {
        if (holyBoltProjectilePrefab == null) return;

        Vector3 travelDir = rot * Vector3.forward;
        Quaternion visualRot = rot * Quaternion.Euler(holyBoltModelRotationOffset);

        GameObject go = Instantiate(holyBoltProjectilePrefab, pos, visualRot);
        SimpleProjectileVisual vis = go.AddComponent<SimpleProjectileVisual>();
        vis.speed    = boltSpeed;
        vis.lifetime = boltRange / boltSpeed;
        vis.SetDirection(travelDir);
    }

    [ObserversRpc]
    void SpawnHolyRainEffect(Vector3 center, float radius, float duration)
    {
        StartCoroutine(SpawnHolyRainObjects(center, radius, duration));
    }

    IEnumerator SpawnHolyRainObjects(Vector3 center, float radius, float duration)
    {
        if (holyRainEffectPrefab == null) yield break;

        int   count        = Mathf.Max(1, holyRainObjectCount);
        float fallDuration = holyRainFallHeight / Mathf.Max(0.01f, holyRainFallSpeed);

        // Stagger spawns so even the last one still lands by holyRainAllLandWithin.
        float spawnWindow = Mathf.Max(0f, holyRainAllLandWithin - fallDuration);
        float interval     = count > 1 ? spawnWindow / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset    = Random.insideUnitCircle * radius;
            Vector3 groundPos = center + new Vector3(offset.x, 0f, offset.y);

            StartCoroutine(FallAndImpact(groundPos));

            yield return new WaitForSeconds(interval);
        }
    }

    Vector3 SnapToGround(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
            return hit.point;
        return pos;
    }

    IEnumerator FallAndImpact(Vector3 groundPos)
    {
        groundPos = SnapToGround(groundPos);

        Vector3 startPos = groundPos + Vector3.up * holyRainFallHeight;
        GameObject obj = Instantiate(holyRainEffectPrefab, startPos, Quaternion.identity);

        float duration = holyRainFallHeight / Mathf.Max(0.01f, holyRainFallSpeed);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            obj.transform.position = Vector3.Lerp(startPos, groundPos, elapsed / duration);
            yield return null;
        }

        Destroy(obj);

        if (holyRainImpactPrefab != null)
        {
            Quaternion impactRot = Quaternion.Euler(holyRainImpactRotationOffset);
            GameObject impact = Instantiate(holyRainImpactPrefab, groundPos + Vector3.up * holyRainImpactHeightOffset, impactRot);

            // Strip leftover physics/auto-destroy from the source prefab — the decal
            // is static and its lifetime is controlled entirely by FadingDecal below.
            foreach (Rigidbody2D rb in impact.GetComponentsInChildren<Rigidbody2D>())
                Destroy(rb);
            foreach (Collider2D col in impact.GetComponentsInChildren<Collider2D>())
                Destroy(col);
            foreach (MonoBehaviour mb in impact.GetComponentsInChildren<MonoBehaviour>())
                if (mb.GetType().Name == "DestroyAfterDelay")
                    Destroy(mb);

            FadingDecal fader = impact.AddComponent<FadingDecal>();
            fader.lifetime = holyRainImpactLifetime;
        }
    }

    [ObserversRpc]
    void SpawnCircleHealEffect(Vector3 pos)
    {
        if (holyCircleEffectPrefab == null) return;
        Instantiate(holyCircleEffectPrefab, pos, Quaternion.identity);
    }

    [ObserversRpc]
    void SpawnOrbitEffect(Vector3 pos, float duration, float radius)
    {
        if (orbitEffectPrefab == null) return;
        GameObject go = Instantiate(orbitEffectPrefab, pos, Quaternion.identity);

        HealingOrbitBuff buff = go.GetComponent<HealingOrbitBuff>();
        if (buff != null)
        {
            buff.target      = transform;
            buff.duration    = duration;
            buff.orbitRadius = radius;
        }
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    void PlayCastAnimation(SkillType skill)
    {
        if (_modelAnimator == null) return;

        // "Spell" → Buff Cast animation, "Cast" → Spell Cast (attack) animation
        string trigger = skill switch
        {
            SkillType.HealingOrbit   => HasParameter("Spell") ? "Spell" : "Cast",
            SkillType.HolyCircleHeal => HasParameter("Spell") ? "Spell" : "Cast",
            _                        => "Cast",
        };

        foreach (string t in new[] { "Cast", "Spell" })
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

    Vector3 GetAimDirection() => GetAimDirection(out _);

    Vector3 GetAimDirection(out float distance)
    {
        distance = 0f;
        if (Camera.main == null) return transform.forward;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 target = ray.GetPoint(dist);
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                distance = dir.magnitude;
                return dir.normalized;
            }
        }
        return transform.forward;
    }

    int GetManaCost(SkillType skill)
    {
        return skill switch
        {
            SkillType.HolyBolt       => holyBoltManaCost,
            SkillType.HolyRain       => holyRainManaCost,
            SkillType.HolyCircleHeal => holyCircleManaCost,
            SkillType.HealingOrbit   => healingOrbitManaCost,
            _                        => 10,
        };
    }

    float GetCooldown(SkillType skill)
    {
        return skill switch
        {
            SkillType.HolyBolt       => holyBoltCooldown,
            SkillType.HolyRain       => holyRainCooldown,
            SkillType.HolyCircleHeal => holyCircleCooldown,
            SkillType.HealingOrbit   => healingOrbitCooldown,
            _                        => 3f,
        };
    }
}
