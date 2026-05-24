using UnityEngine;

public class HolyCircleHealArea : MonoBehaviour
{
    public int healAmount = 25;
    public float radius = 4f;
    public float duration = 5f;
    public float healInterval = 1f;

    private float nextHealTime;
    private float endTime;

    void Start()
    {
        endTime = Time.time + duration;
        nextHealTime = Time.time;
        Destroy(gameObject, duration);
    }

    void Update()
    {
        if (Time.time >= endTime)
            return;

        if (Time.time >= nextHealTime)
        {
            nextHealTime = Time.time + healInterval;
            HealPlayersInArea();
        }
    }

    void HealPlayersInArea()
    {
        PlayerStats[] players = FindObjectsOfType<PlayerStats>();

        foreach (PlayerStats player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= radius)
                player.Heal(healAmount);
        }
    }
}