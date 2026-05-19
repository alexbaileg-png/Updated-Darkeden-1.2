using UnityEngine;

public class HealingOrbitBuff : MonoBehaviour
{
    public Transform target;

    public int baseHealAmount = 10;
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 180f;
    public float duration = 10f;

    private float currentAngle;
    private float lastAngle;
    private float endTime;
    private PlayerStats playerStats;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        if (target != null)
            playerStats = target.GetComponent<PlayerStats>();

        endTime = Time.time + duration;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        lastAngle = currentAngle;
        currentAngle += orbitSpeed * Time.deltaTime;

        if (currentAngle >= 360f)
        {
            currentAngle -= 360f;
            HealOnce();
        }

        float radians = currentAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            1.1f,
            Mathf.Sin(radians) * orbitRadius
        );

        transform.position = target.position + offset;
    }

    void HealOnce()
    {
        if (playerStats == null)
            return;

        int healAmount = playerStats.GetMagicalHealing(baseHealAmount);
        playerStats.Heal(healAmount);
    }
}