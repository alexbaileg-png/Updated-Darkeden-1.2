using UnityEngine;

public class PlayerLootPickup : MonoBehaviour
{
    public InventoryGrid inventoryGrid;
    public float pickupRadius = 2f;
    public float pickupCheckInterval = 0.25f;

    private float nextPickupCheckTime;

    void Update()
    {
        if (Time.time >= nextPickupCheckTime)
        {
            nextPickupCheckTime = Time.time + pickupCheckInterval;
            TryAutoPickupLoot();
        }
    }

    void TryAutoPickupLoot()
    {
        GroundLoot[] lootObjects = FindObjectsOfType<GroundLoot>();

        foreach (GroundLoot loot in lootObjects)
        {
            if (loot == null || loot.itemData == null)
                continue;

            if (!loot.CanBeAutoPickedUp())
                continue;

            float distance = Vector3.Distance(transform.position, loot.transform.position);

            if (distance <= pickupRadius)
            {
                TryPickupSpecificLoot(loot);
            }
        }
    }

    public bool TryPickupSpecificLoot(GroundLoot loot)
    {
        if (loot == null || loot.itemData == null)
            return false;

        if (inventoryGrid == null)
            return false;

        bool added = inventoryGrid.AddItem(loot.itemData);

        if (added)
        {
            Destroy(loot.gameObject);
            return true;
        }

        return false;
    }
}