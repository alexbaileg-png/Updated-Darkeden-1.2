using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Handles all Enchanter skills: Aura Blast, Runic Trap, Disenchant, Runic Nova.
/// Attach to the Slayer player network prefab — EnableClassSkillCaster activates it at runtime.
/// </summary>
public class EnchanterSkillCaster : NetworkBehaviour, ISkillCaster
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

    // ── Aura Blast ────────────────────────────────────────────────────────────
    [Header("Aura Blast")]
    public GameObject auraBlastProjectilePrefab;
    public float auraBlastSpeed = 18f;
    public float auraBlastRange = 22f;
    public Vector3 auraBlastModelRotationOffset = new Vector3(90f, 0f, 0f);

    // ── Runic Trap ────────────────────────────────────────────────────────────
    [Header("Runic Trap")]
    public GameObject runicTrapVisualPrefab;
    public float trapTriggerRadius = 1.5f;
    public float trapDuration      = 15f;
    public int   trapMaxStacks     = 3;
    public float trapCheckInterval = 0.2f;

    int _activeTrapCount = 0;
    int _nextTrapId      = 0;
    readonly Dictionary<int, GameObject> _trapVisuals = new Dictionary<int, GameObject>();

    // ── Disenchant ────────────────────────────────────────────────────────────
    [Header("Disenchant")]
    public GameObject disenchantEffectPrefab;
    public float disenchantRange = 10f;

    // ── Runic Nova ────────────────────────────────────────────────────────────
    [Header("Runic Nova")]
    public GameObject runicNovaGroundPrefab;   // spinning sprite on the ground
    public GameObject runicNovaFallingPrefab;  // sprite that falls from high and impacts
    public float novaRadius       = 6f;
    public float novaCastRange    = 12f;
    public int   novaHits         = 5;
    public float novaDuration     = 3f;
    public float novaFallHeight   = 8f;
    public float novaFallSpeed    = 10f;
    public float novaPullStrength = 1.5f;
    public float novaGroundSpinSpeed = 120f;
    public float novaCharSpinDegrees  = 180f;
    public float novaCharSpinDuration = 0.35f;

    GameObject _novaGroundObject;

    // ── Cast Settings ─────────────────────────────────────────────────────────
    [Header("Cast Settings")]
    public float effectHeightOffset    = 0.8f;
    public float auraBlastDelay        = 0.2f;
    public float runicTrapDelay        = 0.3f;
    public float disenchantDelay       = 0.25f;
    public float runicNovaDelay        = 0.4f;

    [Header("Skill Cooldowns")]
    public float auraBlastCooldown    = 1f;
    public float runicTrapCooldown    = 10f;
    public float disenchantCooldown   = 6f;
    public float runicNovaCooldown    = 14f;

    [Header("Skill Mana Costs")]
    public int auraBlastMana   = 5;
    public int runicTrapMana   = 15;
    public int disenchantMana  = 10;
    public int runicNovaMana   = 28;

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
        if (skill == SkillType.RunicTrap && _activeTrapCount >= trapMaxStacks) return false;
        return true;
    }

    IEnumerator CastRoutine(SkillType skill)
    {
        _isCasting = true;
        _netController?.ServerStopMovement();

        int   manaCost = GetManaCost(skill);
        float cooldown = GetCooldown(skill);

        Vector3 aimDir = GetAimDirection(out float aimDistance);
        if (aimDir.sqrMagnitude > 0.01f)
        {
            _netController?.RotateVisualPublic(aimDir.normalized);
            ServerRotateToward(aimDir.normalized);
        }

        PlayCastAnimation(skill);

        float delay = skill switch
        {
            SkillType.AuraBlast  => auraBlastDelay,
            SkillType.RunicTrap  => runicTrapDelay,
            SkillType.Disenchant => disenchantDelay,
            SkillType.RunicNova  => runicNovaDelay,
            _                    => 0.2f,
        };

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ServerCastSkill(skill, transform.position, aimDir.normalized, aimDistance, manaCost);

        _nextCastTimePerSkill[skill] = Time.time + cooldown;
        _isCasting = false;
    }

    // ── Server RPCs ───────────────────────────────────────────────────────────

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
            case SkillType.AuraBlast:  ServerAuraBlast(casterPos, aimDir);              break;
            case SkillType.RunicTrap:  ServerRunicTrap(casterPos, aimDir, aimDistance); break;
            case SkillType.Disenchant: ServerDisenchant(casterPos);                     break;
            case SkillType.RunicNova:  ServerRunicNova(casterPos, aimDir, aimDistance);  break;
        }
    }

    // ── Aura Blast ────────────────────────────────────────────────────────────

    void ServerAuraBlast(Vector3 casterPos, Vector3 aimDir)
    {
        if (auraBlastProjectilePrefab == null) return;

        Vector3    spawnPos = casterPos + Vector3.up * effectHeightOffset;
        Quaternion spawnRot = Quaternion.LookRotation(aimDir);

        RpcSpawnAuraBlastEffect(spawnPos, spawnRot);

        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = stats != null ? stats.GetMagicalSkillDamage(20) : 20;
        damage     = stats != null ? stats.ApplyCriticalDamage(damage) : damage;

        GameObject proj = new GameObject("AuraBlastServer");
        proj.transform.SetPositionAndRotation(spawnPos, spawnRot);
        HolyBoltProjectile bolt = proj.AddComponent<HolyBoltProjectile>();
        bolt.speed      = auraBlastSpeed;
        bolt.range      = auraBlastRange;
        bolt.damage     = damage;
        bolt.damageType = DamageType.Magical;
        bolt.caster     = stats;
    }

    [ObserversRpc]
    void RpcSpawnAuraBlastEffect(Vector3 pos, Quaternion rot)
    {
        if (auraBlastProjectilePrefab == null) return;

        Vector3    travelDir  = rot * Vector3.forward;
        Quaternion visualRot  = rot * Quaternion.Euler(auraBlastModelRotationOffset);

        GameObject go = Instantiate(auraBlastProjectilePrefab, pos, visualRot);
        SimpleProjectileVisual vis = go.AddComponent<SimpleProjectileVisual>();
        vis.speed    = auraBlastSpeed;
        vis.lifetime = auraBlastRange / auraBlastSpeed;
        vis.SetDirection(travelDir);
    }

    // ── Runic Trap ────────────────────────────────────────────────────────────

    void ServerRunicTrap(Vector3 casterPos, Vector3 aimDir, float aimDistance)
    {
        Vector3 trapPos = casterPos + aimDir * Mathf.Min(aimDistance, 12f);
        trapPos.y = casterPos.y;

        int trapId = _nextTrapId++;
        _activeTrapCount++;

        RpcSpawnTrapVisual(trapId, trapPos);
        StartCoroutine(RunicTrapServerRoutine(trapId, trapPos));
    }

    IEnumerator RunicTrapServerRoutine(int trapId, Vector3 trapPos)
    {
        float elapsed = 0f;
        PlayerStats stats = GetComponent<PlayerStats>();

        while (elapsed < trapDuration)
        {
            yield return new WaitForSeconds(trapCheckInterval);
            elapsed += trapCheckInterval;

            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null || enemy.IsDead()) continue;
                float dist = Vector3.Distance(trapPos, enemy.transform.position);
                if (dist > trapTriggerRadius) continue;

                int damage = stats != null ? stats.GetMagicalSkillDamage(35) : 35;
                damage     = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
                enemy.ReceiveDamage(damage, DamageType.Magical, stats);

                RpcDestroyTrapVisual(trapId);
                _activeTrapCount--;
                yield break;
            }
        }

        RpcDestroyTrapVisual(trapId);
        _activeTrapCount--;
    }

    [ObserversRpc]
    void RpcSpawnTrapVisual(int trapId, Vector3 pos)
    {
        if (runicTrapVisualPrefab == null) return;
        GameObject go = Instantiate(runicTrapVisualPrefab, pos, Quaternion.identity);

        // Force all particle systems to loop so they don't self-destruct after one play
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.loop       = true;
            main.stopAction = ParticleSystemStopAction.None;
        }

        // Strip any auto-destroy scripts baked into the prefab
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb.GetType().Name == "DestroyAfterDelay" || mb.GetType().Name == "AutoDestroy")
                Destroy(mb);

        _trapVisuals[trapId] = go;
    }

    [ObserversRpc]
    void RpcDestroyTrapVisual(int trapId)
    {
        if (_trapVisuals.TryGetValue(trapId, out GameObject go))
        {
            if (go != null) Destroy(go);
            _trapVisuals.Remove(trapId);
        }
    }

    // ── Disenchant ────────────────────────────────────────────────────────────

    void ServerDisenchant(Vector3 casterPos)
    {
        EnemyHealth nearest = null;
        float bestDist = disenchantRange;

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.IsDead()) continue;
            float dist = Vector3.Distance(casterPos, enemy.transform.position);
            if (dist < bestDist) { bestDist = dist; nearest = enemy; }
        }

        if (nearest == null) return;

        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = stats != null ? stats.GetMagicalSkillDamage(40) : 40;
        damage     = stats != null ? stats.ApplyCriticalDamage(damage) : damage;
        nearest.ReceiveDamage(damage, DamageType.Magical, stats);

        RpcSpawnDisenchantEffect(nearest.transform.position + Vector3.up * effectHeightOffset);
    }

    [ObserversRpc]
    void RpcSpawnDisenchantEffect(Vector3 pos)
    {
        if (disenchantEffectPrefab == null) return;
        Instantiate(disenchantEffectPrefab, pos, Quaternion.identity);
    }

    // ── Runic Nova ────────────────────────────────────────────────────────────

    void ServerRunicNova(Vector3 casterPos, Vector3 aimDir, float aimDistance)
    {
        Vector3 center = casterPos + aimDir * Mathf.Min(aimDistance, novaCastRange);
        center.y = casterPos.y;
        StartCoroutine(RunicNovaCoroutine(center));
    }

    IEnumerator RunicNovaCoroutine(Vector3 center)
    {
        PlayerStats stats     = GetComponent<PlayerStats>();
        int hitsCount         = Mathf.Max(1, novaHits);
        float hitInterval     = novaDuration / hitsCount;
        float fallDuration    = novaFallHeight / Mathf.Max(0.1f, novaFallSpeed);
        int   baseDamage      = stats != null ? stats.GetMagicalSkillDamage(55) : 55;
        int   damagePerHit    = Mathf.Max(1, baseDamage / hitsCount);

        // Spawn spinning ground sprite on all clients
        RpcSpawnNovaGround(center, novaDuration + 0.5f);

        for (int i = 0; i < hitsCount; i++)
        {
            // Pull enemies toward center before each impact
            foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead()) continue;
                float dist = Vector3.Distance(center, enemy.transform.position);
                if (dist > novaRadius || dist < 0.1f) continue;
                Vector3 toCenter = center - enemy.transform.position;
                toCenter.y = 0f;
                float pull = Mathf.Min(novaPullStrength, toCenter.magnitude);
                enemy.transform.position += toCenter.normalized * pull;
            }

            // Falling sprite descends — clients handle visuals
            RpcSpawnNovaFalling(center, novaFallHeight, fallDuration);

            // Wait for the falling sprite to land
            yield return new WaitForSeconds(fallDuration);

            // Deal damage at impact
            int dmg = stats != null ? stats.ApplyCriticalDamage(damagePerHit) : damagePerHit;
            foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead()) continue;
                if (Vector3.Distance(center, enemy.transform.position) <= novaRadius)
                    enemy.ReceiveDamage(dmg, DamageType.Magical, stats);
            }

            // Remaining interval before next hit
            float wait = hitInterval - fallDuration;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
        }
    }

    [ObserversRpc]
    void RpcSpawnNovaGround(Vector3 center, float duration)
    {
        if (runicNovaGroundPrefab == null) return;
        _novaGroundObject = Instantiate(runicNovaGroundPrefab, center, Quaternion.identity);

        SpinForever spinner = _novaGroundObject.AddComponent<SpinForever>();
        spinner.degreesPerSecond = novaGroundSpinSpeed;

        FadingDecal fader = _novaGroundObject.AddComponent<FadingDecal>();
        fader.lifetime  = duration;
        fader.fadeStart = 0.75f;
    }

    [ObserversRpc]
    void RpcSpawnNovaFalling(Vector3 center, float height, float fallDuration)
    {
        StartCoroutine(NovaFallingCoroutine(center, height, fallDuration));
    }

    IEnumerator NovaFallingCoroutine(Vector3 center, float height, float fallDuration)
    {
        if (runicNovaFallingPrefab == null) yield break;

        Vector3    startPos = center + Vector3.up * height;
        GameObject obj      = Instantiate(runicNovaFallingPrefab, startPos, Quaternion.identity);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            if (obj != null)
                obj.transform.position = Vector3.Lerp(startPos, center, Mathf.Clamp01(elapsed / fallDuration));
            yield return null;
        }

        if (obj != null) obj.transform.position = center;

        // Pulse the ground sprite on landing
        if (_novaGroundObject != null)
            StartCoroutine(NovaPulseCoroutine(_novaGroundObject));

        // Fade out the falling sprite quickly
        if (obj != null)
        {
            FadingDecal fader = obj.AddComponent<FadingDecal>();
            fader.lifetime  = 0.4f;
            fader.fadeStart = 0f;
        }
    }

    IEnumerator NovaPulseCoroutine(GameObject target)
    {
        if (target == null) yield break;
        Vector3 original = target.transform.localScale;
        Vector3 big      = original * 1.35f;
        float duration   = 0.12f;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (target != null)
                target.transform.localScale = Vector3.Lerp(original, big, elapsed / duration);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (target != null)
                target.transform.localScale = Vector3.Lerp(big, original, elapsed / duration);
            yield return null;
        }
        if (target != null)
            target.transform.localScale = original;
    }

    [ObserversRpc]
    void RpcNovaCharacterSpin()
    {
        StartCoroutine(CharacterSpinCoroutine());
    }

    IEnumerator CharacterSpinCoroutine()
    {
        Transform visual = _netController != null ? _netController.transform : transform;
        float     elapsed  = 0f;
        Quaternion startRot = visual.rotation;
        Quaternion endRot   = startRot * Quaternion.Euler(0f, novaCharSpinDegrees, 0f);

        while (elapsed < novaCharSpinDuration)
        {
            elapsed += Time.deltaTime;
            visual.rotation = Quaternion.Lerp(startRot, endRot, Mathf.Clamp01(elapsed / novaCharSpinDuration));
            yield return null;
        }
        visual.rotation = endRot;
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    void PlayCastAnimation(SkillType skill)
    {
        if (_modelAnimator == null) return;

        string trigger = skill switch
        {
            SkillType.HealingOrbit => HasParameter("Spell") ? "Spell" : "Cast",
            _                      => "Cast",
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
            SkillType.AuraBlast  => auraBlastMana,
            SkillType.RunicTrap  => runicTrapMana,
            SkillType.Disenchant => disenchantMana,
            SkillType.RunicNova  => runicNovaMana,
            _                    => 10,
        };
    }

    float GetCooldown(SkillType skill)
    {
        return skill switch
        {
            SkillType.AuraBlast  => auraBlastCooldown,
            SkillType.RunicTrap  => runicTrapCooldown,
            SkillType.Disenchant => disenchantCooldown,
            SkillType.RunicNova  => runicNovaCooldown,
            _                    => 3f,
        };
    }
}
