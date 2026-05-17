using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    public int maxEnemies = 10;

    public float spawnRadius = 15f;

    public float spawnInterval = 2f;

    private float nextSpawnTime = 0f;

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            TrySpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void TrySpawnEnemy()
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();

        if (enemies.Length >= maxEnemies)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPosition = new Vector3(
            randomCircle.x,
            1f,
            randomCircle.y
        );

        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        EnemyAI ai = enemy.GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.player = GameObject.Find("Player").transform;
        }
    }
}