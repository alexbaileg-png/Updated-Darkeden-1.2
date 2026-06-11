using FishNet;
using FishNet.Object;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public NetworkObject enemyPrefab;

    public int maxEnemies = 10;
    public float spawnRadius = 15f;
    public float spawnInterval = 2f;

    private float nextSpawnTime = 0f;

    void Update()
    {
        // Only the server spawns enemies
        if (!InstanceFinder.IsServerStarted) return;

        if (Time.time >= nextSpawnTime)
        {
            TrySpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void TrySpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab is not assigned.", this);
            return;
        }

        if (FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length >= maxEnemies)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        NetworkObject enemy = InstanceFinder.NetworkManager.GetPooledInstantiated(
            enemyPrefab, spawnPosition, Quaternion.identity, true);

        InstanceFinder.ServerManager.Spawn(enemy);
    }
}
