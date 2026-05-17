using System.Collections;
using UnityEngine;

public class PlayerProjectileAttack : MonoBehaviour
{
    public enum SelectedSkill
    {
        HolyBolt,
        HolyRain
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

    private float nextCastTime = 0f;
    private bool isCasting = false;

    private PlayerMovement playerMovement;
    private Animator modelAnimator;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement != null && playerMovement.modelAnimator != null)
        {
            modelAnimator = playerMovement.modelAnimator;
        }
    }

    void Update()
    {
        HandleSkillSelection();
        HandleCasting();
    }

    void HandleSkillSelection()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            selectedSkill = SelectedSkill.HolyBolt;
            Debug.Log("Selected Skill: Holy Bolt");
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            selectedSkill = SelectedSkill.HolyRain;
            Debug.Log("Selected Skill: Holy Rain");
        }
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
        {
            playerMovement.StopMovement();
        }

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
        {
            Debug.LogError("Holy Bolt projectilePrefab is missing.");
            return;
        }

        EnemyHealth target = HoverDetector.CurrentEnemyTarget;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = transform.forward;
        }

        direction.Normalize();

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position + direction * 1f + Vector3.up * 0.5f,
            Quaternion.LookRotation(direction)
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null && target != null)
        {
            projectile.SetTarget(target);
            Debug.Log("Holy Bolt homing on: " + target.gameObject.name);
        }
        else
        {
            Debug.Log("Holy Bolt fired without target.");
        }
    }

    void CastHolyRain()
    {
        Vector3 castPosition = GetMouseWorldPosition();
        castPosition.y = 1.5f;

        if (holyRainEffectPrefab == null)
        {
            Debug.LogError("Holy Rain effect prefab is missing.");
        }
        else
        {
            GameObject effect = Instantiate(
                holyRainEffectPrefab,
                castPosition,
                holyRainEffectPrefab.transform.rotation
            );

            Debug.Log("Holy Rain effect spawned: " + effect.name);
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            Vector3 enemyPosition = enemy.transform.position;
            enemyPosition.y = castPosition.y;

            float distance = Vector3.Distance(castPosition, enemyPosition);

            if (distance <= holyRainRadius)
            {
                enemy.TakeDamage(holyRainDamage);
            }
        }

        Debug.Log("Holy Rain cast at: " + castPosition);
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
        {
            playerMovement.RotateVisual(direction);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position + transform.forward * 3f;
    }
}