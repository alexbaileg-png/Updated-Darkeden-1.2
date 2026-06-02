using UnityEngine;

[System.Serializable]
public class LootDropEntry
{
    public ItemData item;

    [Range(0f, 100f)]
    public float dropChance = 25f;
}

public class LootDropTable : MonoBehaviour
{
    [Header("Fallback Prefab")]
    public GameObject defaultGroundLootPrefab;

    [Header("Loot Table")]
    public LootDropEntry[] possibleDrops;

    [Header("Drop Settings")]
    public int maxDrops = 1;
    public float dropScatterRadius = 1.2f;
    public float dropHeight = 0f;

    public void DropLoot()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
            return;

        int dropsCreated = 0;

        foreach (LootDropEntry entry in possibleDrops)
        {
            if (entry == null || entry.item == null)
                continue;

            if (dropsCreated >= maxDrops)
                break;

            float roll = Random.Range(0f, 100f);

            if (roll <= entry.dropChance)
            {
                ItemData rolledItem = ItemRoller.RollItem(entry.item);
                DropItem(rolledItem);
                dropsCreated++;
            }
        }
    }

    void DropItem(ItemData item)
    {
        if (item == null)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * dropScatterRadius;

        Vector3 dropPosition = transform.position + new Vector3(
            randomCircle.x,
            dropHeight,
            randomCircle.y
        );

        if (LootBagManager.Instance != null)
        {
            LootBagManager.Instance.AddLoot(item, dropPosition);
            return;
        }

        SpawnFallbackGroundLoot(item, dropPosition);
    }

    void SpawnFallbackGroundLoot(ItemData item, Vector3 dropPosition)
    {
        GameObject prefabToSpawn = item.worldLootPrefab;

        if (prefabToSpawn == null)
            prefabToSpawn = defaultGroundLootPrefab;

        if (prefabToSpawn == null)
            return;

        GameObject lootObject = Instantiate(
            prefabToSpawn,
            dropPosition,
            prefabToSpawn.transform.rotation
        );

        GroundLoot groundLoot = lootObject.GetComponent<GroundLoot>();

        if (groundLoot != null)
        {
            groundLoot.SetItem(item);
            groundLoot.canAutoPickup = true;
        }
    }
}