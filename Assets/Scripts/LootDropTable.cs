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

    public void DropLoot(PlayerStats killer = null)
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            Debug.Log($"[LootDropTable] {gameObject.name} has no possibleDrops configured.");
            return;
        }

        // Read faction directly from the killer's PlayerStats — works correctly in multiplayer
        PlayerFaction? killerFaction = killer != null ? killer.faction : (PlayerFaction?)null;

        Debug.Log($"[LootDropTable] DropLoot called — killer={killer?.name ?? "NULL"} faction={killerFaction?.ToString() ?? "NULL"}");

        int dropsCreated = 0;

        foreach (LootDropEntry entry in possibleDrops)
        {
            if (entry == null || entry.item == null) continue;
            if (dropsCreated >= maxDrops) break;

            // Skip items that don't belong to the killer's faction
            if (killerFaction.HasValue)
            {
                var restriction = entry.item.factionRestriction;
                if (restriction == FactionRestriction.SlayerOnly && killerFaction.Value == PlayerFaction.Vampire)
                { Debug.Log($"[LootDropTable] Skipping '{entry.item.itemName}' — SlayerOnly, killer is Vampire"); continue; }
                if (restriction == FactionRestriction.VampireOnly && killerFaction.Value == PlayerFaction.Slayer)
                { Debug.Log($"[LootDropTable] Skipping '{entry.item.itemName}' — VampireOnly, killer is Slayer"); continue; }
            }

            float roll = Random.Range(0f, 100f);
            Debug.Log($"[LootDropTable] Rolling for '{entry.item.itemName}' (id='{entry.item.itemId}'): roll={roll:F1} vs chance={entry.dropChance}");

            if (roll <= entry.dropChance)
            {
                ItemData rolledItem = ItemRoller.RollItem(entry.item);
                if (rolledItem == null) { Debug.LogWarning($"[LootDropTable] ItemRoller returned null for {entry.item.itemName}"); continue; }
                DropItem(rolledItem);
                dropsCreated++;
            }
        }

        Debug.Log($"[LootDropTable] {gameObject.name} dropped {dropsCreated} item(s) for faction={killerFaction?.ToString() ?? "unknown"}");
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
        // LootBagManager is missing — log a warning, nothing we can safely spawn without networking
        Debug.LogWarning($"[LootDropTable] LootBagManager not found — '{item.itemName}' was not dropped. " +
                         "Add a LootBagManager to the scene.");
    }
}