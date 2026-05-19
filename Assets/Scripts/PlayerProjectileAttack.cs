using System.Collections;
using UnityEngine;

public class PlayerProjectileAttack : MonoBehaviour
{
    public enum SelectedSkill
    {
        HolyBolt,
        HolyRain,
        HolyCircleHeal,
        HealingOrbit
    }

    [Header("Selected Skill")]
    public SelectedSkill selectedSkill = SelectedSkill.HolyBolt;

    [Header("Cast Settings")]
    public float castDelay = 0.15f;

    [Header("Holy Bolt")]
    public GameObject projectilePrefab;
    public float holyBoltCooldown = 0.35f;

    [Header("Holy Rain")]
    public GameObject holyRainEffectPrefab;
    public float holyRainCooldown = 1.5f;
    public float holyRainRadius = 3f;
    public int holyRainDamage = 20;

    [Header("Holy Circle Heal")]
    public GameObject holyCircleHealEffectPrefab;
    public float holyCircleHealCooldown = 2f;
    public float holyCircleHealRadius = 4f;
    public int holyCircleHealBaseAmount = 25;
    public float holyCircleHealEffectHeight = 0.25f;

    [Header("Healing Orbit Buff")]
    public GameObject healingOrbitPrefab;
    public float healingOrbitCooldown = 8f;
    public int healingOrbitBaseHeal = 10;
    public float healingOrbitDuration = 10f;
    public float healingOrbitRadius = 1.5f;
    public float healingOrbitSpeed = 180f;

    private float nextCastTime = 0f;
    private bool isCasting = false;

    private PlayerMovement playerMovement;
    private PlayerStats playerStats;
    private Animator modelAnimator;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();

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
            selectedSkill = SelectedSkill.HolyBolt;

        if (Input.GetKeyDown(KeyCode.F10))
            selectedSkill = SelectedSkill.HolyRain;

        if (Input.GetKeyDown(KeyCode.F9))
            selectedSkill = SelectedSkill.HolyCircleHeal;

        if (Input.GetKeyDown(KeyCode.F11))
            selectedSkill = SelectedSkill.HealingOrbit;
    }

    void HandleCasting()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        if (Time.time < nextCastTime)
            return;

        if (isCasting)
            return;

        StartCoroutine(CastSelectedSkillRoutine());
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

        if (selectedSkill == SelectedSkill.HolyBolt)
        {
            CastHolyBolt(aimDirection);
            nextCastTime = Time.time + holyBoltCooldown;
        }
        else if (selectedSkill == SelectedSkill.HolyRain)
        {
            CastHolyRain();
            nextCastTime = Time.time + holyRainCooldown;
        }
        else if (selectedSkill == SelectedSkill.HolyCircleHeal)
        {
            CastHolyCircleHeal();
            nextCastTime = Time.time + holyCircleHealCooldown;
        }
        else if (selectedSkill == SelectedSkill.HealingOrbit)
        {
            CastHealingOrbit();
            nextCastTime = Time.time + healingOrbitCooldown;
        }

        isCasting = false;
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

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position + direction * 1f + Vector3.up * 0.5f,
            Quaternion.LookRotation(direction)
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetAttacker(gameObject);
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

        if (holyRainEffectPrefab != null)
        {
            Instantiate(
                holyRainEffectPrefab,
                castPosition,
                holyRainEffectPrefab.transform.rotation
            );
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            Vector3 enemyPosition = enemy.transform.position;
            enemyPosition.y = castPosition.y;

            float distance = Vector3.Distance(castPosition, enemyPosition);

            if (distance <= holyRainRadius)
            {
                if (CombatManager.Instance != null)
                {
                    DamageRequest request = new DamageRequest(
                        gameObject,
                        enemy.gameObject,
                        holyRainDamage,
                        DamageType.Magical,
                        true
                    );

                    CombatManager.Instance.ApplyDamage(request);
                }
                else
                {
                    enemy.ReceiveDamage(holyRainDamage, DamageType.Magical);
                }
            }
        }
    }

    void CastHolyCircleHeal()
    {
        Vector3 castPosition = GetMouseWorldPosition();
        castPosition.y = holyCircleHealEffectHeight;

        if (holyCircleHealEffectPrefab == null)
        {
            Debug.LogError("Holy Circle Heal Effect Prefab is missing.");
        }
        else
        {
            GameObject effectObject = Instantiate(
                holyCircleHealEffectPrefab,
                castPosition,
                holyCircleHealEffectPrefab.transform.rotation
            );

            Debug.Log("Holy Circle Heal effect spawned: " + effectObject.name + " at " + castPosition);
        }

        int healAmount = holyCircleHealBaseAmount;

        if (playerStats != null)
            healAmount = playerStats.GetMagicalHealing(holyCircleHealBaseAmount);

        PlayerStats[] players = FindObjectsOfType<PlayerStats>();

        foreach (PlayerStats targetStats in players)
        {
            float distance = Vector3.Distance(castPosition, targetStats.transform.position);

            if (distance <= holyCircleHealRadius)
                targetStats.Heal(healAmount);
        }
    }

    void CastHealingOrbit()
    {
        if (healingOrbitPrefab == null)
            return;

        HealingOrbitBuff existingBuff = GetComponentInChildren<HealingOrbitBuff>();

        if (existingBuff != null)
            Destroy(existingBuff.gameObject);

        GameObject orbitObject = Instantiate(
            healingOrbitPrefab,
            transform.position + Vector3.up * 1.1f,
            Quaternion.identity,
            transform
        );

        HealingOrbitBuff orbitBuff = orbitObject.GetComponent<HealingOrbitBuff>();

        if (orbitBuff != null)
        {
            orbitBuff.target = transform;
            orbitBuff.baseHealAmount = healingOrbitBaseHeal;
            orbitBuff.duration = healingOrbitDuration;
            orbitBuff.orbitRadius = healingOrbitRadius;
            orbitBuff.orbitSpeed = healingOrbitSpeed;
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