using System.Collections;
using UnityEngine;

public class PlayerProjectileAttack : MonoBehaviour
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

    private PlayerMovement playerMovement;
    private PlayerStats playerStats;
    private PlayerSkillManager skillManager;
    private Animator modelAnimator;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();
        skillManager = GetComponent<PlayerSkillManager>();

        if (playerMovement != null && playerMovement.modelAnimator != null)
            modelAnimator = playerMovement.modelAnimator;
    }

    void Update()
    {
        HandleSkillSelection();
        HandleCasting();
    }

    void HandleSkillSelection()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            currentSelectedSkill = f8Skill;

        if (Input.GetKeyDown(KeyCode.F9))
            currentSelectedSkill = f9Skill;

        if (Input.GetKeyDown(KeyCode.F10))
            currentSelectedSkill = f10Skill;

        if (Input.GetKeyDown(KeyCode.F11))
            currentSelectedSkill = f11Skill;
    }

    void HandleCasting()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        if (Time.time < nextCastTime)
            return;

        if (isCasting)
            return;

        if (!CanUseSkill(currentSelectedSkill))
            return;

        StartCoroutine(CastSelectedSkillRoutine());
    }

    bool CanUseSkill(SkillType skill)
    {
        if (skillManager != null && !skillManager.IsSkillUnlocked(skill))
            return false;

        int manaCost = GetManaCost(skill);

        if (playerStats != null && !playerStats.SpendMana(manaCost))
            return false;

        return true;
    }

    IEnumerator CastSelectedSkillRoutine()
    {
        isCasting = true;

        if (playerMovement != null)
            playerMovement.StopMovement();

        Vector3 aimDirection = GetAimDirection();

        if (aimDirection.sqrMagnitude > 0.01f)
        {
            aimDirection.Normalize();
            RotatePlayerVisual(aimDirection);
        }

        PlayCastAnimation();

        yield return new WaitForSeconds(castDelay);

        if (currentSelectedSkill == SkillType.HolyBolt)
            CastHolyBolt(aimDirection);
        else if (currentSelectedSkill == SkillType.HolyRain)
            CastHolyRain();
        else if (currentSelectedSkill == SkillType.HolyCircleHeal)
            CastHolyCircleHeal();
        else if (currentSelectedSkill == SkillType.HealingOrbit)
            CastHealingOrbit();

        nextCastTime = Time.time + GetCooldown(currentSelectedSkill);
        isCasting = false;
    }

    int GetSkillLevel(SkillType skill)
    {
        if (skillManager == null)
            return 1;

        return Mathf.Max(1, skillManager.GetSkillLevel(skill));
    }

    int GetSkillPower(SkillType skill, int fallback)
    {
        if (skillManager == null)
            return fallback;

        int power = skillManager.GetSkillPower(skill);
        return power > 0 ? power : fallback;
    }

    float GetCooldown(SkillType skill)
    {
        if (skillManager == null)
            return 1f;

        return skillManager.GetSkillCooldown(skill);
    }

    int GetManaCost(SkillType skill)
    {
        if (skillManager == null)
            return 0;

        return skillManager.GetSkillManaCost(skill);
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, nextCastTime - Time.time);
    }

    public float GetCurrentSkillCooldown()
    {
        return GetCooldown(currentSelectedSkill);
    }

    public void SetSelectedSkill(SkillType skill)
    {
        currentSelectedSkill = skill;
    }

    public void BindSkill(KeyCode key, SkillType skill)
    {
        if (key == KeyCode.F8)
            f8Skill = skill;

        if (key == KeyCode.F9)
            f9Skill = skill;

        if (key == KeyCode.F10)
            f10Skill = skill;

        if (key == KeyCode.F11)
            f11Skill = skill;

        Debug.Log("Bound " + skill + " to " + key);
    }

    Vector3 GetAimDirection()
    {
        EnemyHealth target = HoverDetector.CurrentEnemyTarget;

        if (target != null)
        {
            Vector3 directionToTarget = target.transform.position - transform.position;
            directionToTarget.y = 0f;
            return directionToTarget;
        }

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        Vector3 directionToMouse = mouseWorldPosition - transform.position;
        directionToMouse.y = 0f;

        return directionToMouse;
    }

    void CastHolyBolt(Vector3 direction)
    {
        if (projectilePrefab == null)
            return;

        EnemyHealth target = HoverDetector.CurrentEnemyTarget;

        if (direction.sqrMagnitude <= 0.01f)
            direction = transform.forward;

        direction.Normalize();

        int damage = GetSkillPower(SkillType.HolyBolt, 20);

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position + direction * 1f + Vector3.up * 0.5f,
            Quaternion.LookRotation(direction)
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetAttacker(gameObject);
            projectile.damage = damage;
            projectile.damageType = DamageType.Magical;
            projectile.canCrit = true;

            if (target != null)
                projectile.SetTarget(target);
        }
    }

    void CastHolyRain()
    {
        Vector3 castPosition = GetMouseWorldPosition();
        castPosition.y = 1.5f;

        int skillLevel = GetSkillLevel(SkillType.HolyRain);
        int damage = GetSkillPower(SkillType.HolyRain, 20);
        float radius = holyRainBaseRadius + (skillLevel - 1) * 0.35f;

        if (holyRainEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                holyRainEffectPrefab,
                castPosition,
                holyRainEffectPrefab.transform.rotation
            );

            if (holyRainBaseRadius > 0.01f)
                effect.transform.localScale *= radius / holyRainBaseRadius;
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            Vector3 enemyPosition = enemy.transform.position;
            enemyPosition.y = castPosition.y;

            float distance = Vector3.Distance(castPosition, enemyPosition);

            if (distance <= radius)
            {
                if (CombatManager.Instance != null)
                {
                    DamageRequest request = new DamageRequest(
                        gameObject,
                        enemy.gameObject,
                        damage,
                        DamageType.Magical,
                        true
                    );

                    CombatManager.Instance.ApplyDamage(request);
                }
                else
                {
                    enemy.ReceiveDamage(damage, DamageType.Magical);
                }
            }
        }
    }

    void CastHolyCircleHeal()
    {
        Vector3 castPosition = GetMouseWorldPosition();
        castPosition.y = holyCircleHealEffectHeight;

        int skillLevel = GetSkillLevel(SkillType.HolyCircleHeal);

        int healAmount = GetSkillPower(SkillType.HolyCircleHeal, 25);
        float radius = holyCircleHealBaseRadius + (skillLevel - 1) * 0.25f;
        float duration = holyCircleHealBaseDuration + (skillLevel - 1) * holyCircleHealDurationPerLevel;

        GameObject effectObject = null;

        if (holyCircleHealEffectPrefab != null)
        {
            effectObject = Instantiate(
                holyCircleHealEffectPrefab,
                castPosition,
                holyCircleHealEffectPrefab.transform.rotation
            );

            if (holyCircleHealBaseRadius > 0.01f)
                effectObject.transform.localScale *= radius / holyCircleHealBaseRadius;
        }

        if (effectObject != null)
        {
            HolyCircleHealArea healArea = effectObject.GetComponent<HolyCircleHealArea>();

            if (healArea == null)
                healArea = effectObject.AddComponent<HolyCircleHealArea>();

            healArea.healAmount = playerStats != null ? playerStats.GetMagicalHealing(healAmount) : healAmount;
            healArea.radius = radius;
            healArea.duration = duration;
            healArea.healInterval = holyCircleHealInterval;
        }
    }

    void CastHealingOrbit()
    {
        if (healingOrbitPrefab == null)
            return;

        int skillLevel = GetSkillLevel(SkillType.HealingOrbit);

        HealingOrbitBuff existingBuff = FindObjectOfType<HealingOrbitBuff>();

        if (existingBuff != null && existingBuff.target == transform)
            Destroy(existingBuff.gameObject);

        GameObject orbitObject = Instantiate(
            healingOrbitPrefab,
            transform.position + Vector3.up * 1.1f,
            healingOrbitPrefab.transform.rotation
        );

        orbitObject.transform.localScale = healingOrbitPrefab.transform.localScale;

        HealingOrbitBuff orbitBuff = orbitObject.GetComponent<HealingOrbitBuff>();

        if (orbitBuff != null)
        {
            orbitBuff.target = transform;
            orbitBuff.baseHealAmount = GetSkillPower(SkillType.HealingOrbit, 10);
            orbitBuff.duration = healingOrbitBaseDuration + (skillLevel - 1) * healingOrbitDurationPerLevel;
            orbitBuff.orbitRadius = healingOrbitBaseRadius + (skillLevel - 1) * healingOrbitRadiusPerLevel;
            orbitBuff.orbitSpeed = healingOrbitBaseSpeed + ((skillLevel - 1) * healingOrbitSpeedPerLevel);
        }
    }

    void PlayCastAnimation()
    {
        if (modelAnimator != null)
        {
            modelAnimator.ResetTrigger("Cast");
            modelAnimator.SetTrigger("Cast");
        }
    }

    void RotatePlayerVisual(Vector3 direction)
    {
        if (playerMovement != null)
            playerMovement.RotateVisual(direction);
        else
            transform.rotation = Quaternion.LookRotation(direction);
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return transform.position + transform.forward * 3f;
    }
}