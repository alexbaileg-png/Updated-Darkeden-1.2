using System.Collections;
using FishNet.Object;
using UnityEngine;

public class PlayerProjectileAttack : NetworkBehaviour
{
    [Header("Selected Skill")]
    public SkillType currentSelectedSkill = SkillType.HolyBolt;

    [Header("Keybinds")]
    public SkillType f8Skill = SkillType.HolyBolt;
    public SkillType f9Skill = SkillType.HolyCircleHeal;
    public SkillType f10Skill = SkillType.HolyRain;
    public SkillType f11Skill = SkillType.HealingOrbit;

    [Header("Cast Settings")]
    public float castDelay = 0.15f;

    [Header("Holy Bolt")]
    public GameObject projectilePrefab;

    [Header("Holy Rain")]
    public GameObject holyRainEffectPrefab;
    public float holyRainBaseRadius = 3f;

    [Header("Holy Circle Heal")]
    public GameObject holyCircleHealEffectPrefab;
    public float holyCircleHealBaseRadius = 4f;
    public float holyCircleHealBaseDuration = 4f;
    public float holyCircleHealDurationPerLevel = 0.75f;
    public float holyCircleHealInterval = 1f;
    public float holyCircleHealEffectHeight = 0.25f;

    [Header("Healing Orbit Buff")]
    public GameObject healingOrbitPrefab;
    public float healingOrbitBaseDuration = 10f;
    public float healingOrbitDurationPerLevel = 1f;
    public float healingOrbitBaseRadius = 1.5f;
    public float healingOrbitRadiusPerLevel = 0.15f;
    public float healingOrbitBaseSpeed = 420f;
    public float healingOrbitSpeedPerLevel = 35f;

    private float nextCastTime = 0f;
    private bool isCasting = false;

    private PlayerStats playerStats;
    private PlayerSkillManager skillManager;
    private Animator modelAnimator;
    private NetworkPlayerController netController;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;

        playerStats = GetComponent<PlayerStats>();
        skillManager = GetComponent<PlayerSkillManager>();
        netController = GetComponent<NetworkPlayerController>();

        // Get animator from NetworkPlayerController's modelTransform
        if (netController != null && netController.modelTransform != null)
            modelAnimator = netController.modelTransform.GetComponent<Animator>();

        if (modelAnimator == null)
            modelAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleSkillSelection();
        HandleCasting();
    }

    void HandleSkillSelection()
    {
        if (Input.GetKeyDown(KeyCode.F8)) currentSelectedSkill = f8Skill;
        if (Input.GetKeyDown(KeyCode.F9)) currentSelectedSkill = f9Skill;
        if (Input.GetKeyDown(KeyCode.F10)) currentSelectedSkill = f10Skill;
        if (Input.GetKeyDown(KeyCode.F11)) currentSelectedSkill = f11Skill;
    }

    void HandleCasting()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (Time.time < nextCastTime) return;
        if (isCasting) return;
        if (!CanCastSkill(currentSelectedSkill)) return;

        StartCoroutine(CastRoutine());
    }

    bool CanCastSkill(SkillType skill)
    {
        if (skillManager != null && !skillManager.IsSkillUnlocked(skill))
            return false;

        // Mana check (preview only on client — server enforces it)
        int cost = GetManaCost(skill);
        if (playerStats != null && playerStats.currentMana < cost)
            return false;

        return true;
    }

    IEnumerator CastRoutine()
    {
        isCasting = true;

        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
        {
            aimDir.Normalize();
            RotatePlayerVisual(aimDir);
        }

        PlayCastAnimation();

        yield return new WaitForSeconds(castDelay);

        Vector3 castPosition = GetMouseWorldPosition();
        int skillLevel = GetSkillLevel(currentSelectedSkill);
        int skillPower = GetSkillPower(currentSelectedSkill, 20);
        int manaCost = GetManaCost(currentSelectedSkill);

        // Send to server — server validates mana, applies damage/heals
        ServerCastSkill(currentSelectedSkill, transform.position, aimDir, castPosition, skillLevel, skillPower, manaCost);

        nextCastTime = Time.time + GetCooldown(currentSelectedSkill);
        isCasting = false;
    }

    [ServerRpc]
    void ServerCastSkill(SkillType skill, Vector3 casterPos, Vector3 aimDir, Vector3 castPos, int skillLevel, int skillPower, int manaCost)
    {
        // Server validates and spends mana
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && !stats.SpendMana(manaCost))
            return;

        switch (skill)
        {
            case SkillType.HolyBolt:
                ServerHolyBolt(casterPos, aimDir, skillPower);
                break;
            case SkillType.HolyRain:
                ServerHolyRain(castPos, skillLevel, skillPower);
                break;
            case SkillType.HolyCircleHeal:
                ServerHolyCircleHeal(castPos, skillLevel, skillPower);
                break;
            case SkillType.HealingOrbit:
                ServerHealingOrbit(skillLevel, skillPower);
                break;
        }
    }

    // ── Holy Bolt ─────────────────────────────────────────────────────────────

    void ServerHolyBolt(Vector3 casterPos, Vector3 direction, int damage)
    {
        if (direction.sqrMagnitude <= 0.01f) direction = transform.forward;
        direction.Normalize();

        Vector3 spawnPos = casterPos + direction * 1f + Vector3.up * 0.5f;

        // Find nearest enemy in direction as hitscan target
        EnemyHealth bestTarget = null;
        float bestAngle = 25f;

        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy.IsDead()) continue;
            Vector3 toEnemy = (enemy.transform.position - casterPos).normalized;
            toEnemy.y = 0f;
            float angle = Vector3.Angle(direction, toEnemy);
            float dist = Vector3.Distance(casterPos, enemy.transform.position);
            if (angle < bestAngle && dist < 30f)
            {
                bestAngle = angle;
                bestTarget = enemy;
            }
        }

        // Spawn visual projectile on all clients, damage applied on hit server-side
        SpawnHolyBoltVisual(spawnPos, direction, bestTarget != null ? bestTarget.gameObject : null, damage);
    }

    [ObserversRpc]
    void SpawnHolyBoltVisual(Vector3 spawnPos, Vector3 direction, GameObject target, int damage)
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        Projectile projectile = proj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetAttacker(gameObject);
            projectile.damage = damage;
            projectile.damageType = DamageType.Magical;
            projectile.canCrit = true;

            if (target != null)
            {
                EnemyHealth eh = target.GetComponent<EnemyHealth>();
                if (eh != null) projectile.SetTarget(eh);
            }
        }
    }

    // ── Holy Rain ─────────────────────────────────────────────────────────────

    void ServerHolyRain(Vector3 castPos, int skillLevel, int damage)
    {
        castPos.y = 1.5f;
        float radius = holyRainBaseRadius + (skillLevel - 1) * 0.35f;

        foreach (EnemyHealth enemy in FindObjectsOfType<EnemyHealth>())
        {
            if (enemy.IsDead()) continue;
            Vector3 enemyPos = enemy.transform.position;
            enemyPos.y = castPos.y;
            if (Vector3.Distance(castPos, enemyPos) <= radius)
                enemy.ReceiveDamage(damage, DamageType.Magical);
        }

        SpawnHolyRainVisual(castPos, radius);
    }

    [ObserversRpc]
    void SpawnHolyRainVisual(Vector3 castPos, float radius)
    {
        if (holyRainEffectPrefab == null) return;
        GameObject effect = Instantiate(holyRainEffectPrefab, castPos, holyRainEffectPrefab.transform.rotation);
        if (holyRainBaseRadius > 0.01f)
            effect.transform.localScale *= radius / holyRainBaseRadius;
    }

    // ── Holy Circle Heal ──────────────────────────────────────────────────────

    void ServerHolyCircleHeal(Vector3 castPos, int skillLevel, int healAmount)
    {
        castPos.y = holyCircleHealEffectHeight;
        float radius = holyCircleHealBaseRadius + (skillLevel - 1) * 0.25f;
        float duration = holyCircleHealBaseDuration + (skillLevel - 1) * holyCircleHealDurationPerLevel;

        PlayerStats stats = GetComponent<PlayerStats>();
        int finalHeal = stats != null ? stats.GetMagicalHealing(healAmount) : healAmount;

        SpawnHolyCircleHealVisual(castPos, radius, duration, finalHeal);
    }

    [ObserversRpc]
    void SpawnHolyCircleHealVisual(Vector3 castPos, float radius, float duration, int healAmount)
    {
        if (holyCircleHealEffectPrefab == null) return;

        GameObject effectObject = Instantiate(holyCircleHealEffectPrefab, castPos, holyCircleHealEffectPrefab.transform.rotation);
        if (holyCircleHealBaseRadius > 0.01f)
            effectObject.transform.localScale *= radius / holyCircleHealBaseRadius;

        HolyCircleHealArea healArea = effectObject.GetComponent<HolyCircleHealArea>();
        if (healArea == null) healArea = effectObject.AddComponent<HolyCircleHealArea>();

        healArea.healAmount = healAmount;
        healArea.radius = radius;
        healArea.duration = duration;
        healArea.healInterval = holyCircleHealInterval;
    }

    // ── Healing Orbit ─────────────────────────────────────────────────────────

    void ServerHealingOrbit(int skillLevel, int healAmount)
    {
        float duration = healingOrbitBaseDuration + (skillLevel - 1) * healingOrbitDurationPerLevel;
        float radius = healingOrbitBaseRadius + (skillLevel - 1) * healingOrbitRadiusPerLevel;
        float speed = healingOrbitBaseSpeed + (skillLevel - 1) * healingOrbitSpeedPerLevel;

        SpawnHealingOrbitVisual(duration, radius, speed, healAmount);
    }

    [ObserversRpc]
    void SpawnHealingOrbitVisual(float duration, float radius, float speed, int healAmount)
    {
        if (healingOrbitPrefab == null) return;

        HealingOrbitBuff existing = FindObjectOfType<HealingOrbitBuff>();
        if (existing != null && existing.target == transform)
            Destroy(existing.gameObject);

        GameObject orbitObject = Instantiate(healingOrbitPrefab, transform.position + Vector3.up * 1.1f, healingOrbitPrefab.transform.rotation);
        orbitObject.transform.localScale = healingOrbitPrefab.transform.localScale;

        HealingOrbitBuff orbitBuff = orbitObject.GetComponent<HealingOrbitBuff>();
        if (orbitBuff != null)
        {
            orbitBuff.target = transform;
            orbitBuff.duration = duration;
            orbitBuff.orbitRadius = radius;
            orbitBuff.orbitSpeed = speed;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    int GetSkillLevel(SkillType skill) => skillManager != null ? Mathf.Max(1, skillManager.GetSkillLevel(skill)) : 1;
    int GetSkillPower(SkillType skill, int fallback) => skillManager != null && skillManager.GetSkillPower(skill) > 0 ? skillManager.GetSkillPower(skill) : fallback;
    float GetCooldown(SkillType skill) => skillManager != null ? skillManager.GetSkillCooldown(skill) : 1f;
    int GetManaCost(SkillType skill) => skillManager != null ? skillManager.GetSkillManaCost(skill) : 0;

    public float GetCooldownRemaining() => Mathf.Max(0f, nextCastTime - Time.time);
    public float GetCurrentSkillCooldown() => GetCooldown(currentSelectedSkill);
    public void SetSelectedSkill(SkillType skill) => currentSelectedSkill = skill;

    public void BindSkill(KeyCode key, SkillType skill)
    {
        if (key == KeyCode.F8) f8Skill = skill;
        if (key == KeyCode.F9) f9Skill = skill;
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
        Vector3 toMouse = mousePos - transform.position;
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
        if (modelAnimator == null) return;
        modelAnimator.ResetTrigger("Cast");
        modelAnimator.SetTrigger("Cast");
    }

    void RotatePlayerVisual(Vector3 direction)
    {
        if (netController != null)
            netController.RotateVisualPublic(direction);
        else
            transform.rotation = Quaternion.LookRotation(direction);
    }
}
