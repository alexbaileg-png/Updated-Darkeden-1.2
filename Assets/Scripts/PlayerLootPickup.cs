using UnityEngine;

public class PlayerLootPickup : MonoBehaviour
{
    public InventoryGrid inventoryGrid;

    [Header("Loot Bag Interaction")]
    public float bagOpenRange = 3f;
    public KeyCode openBagKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(openBagKey))
            TryOpenNearestBag();
    }

    void TryOpenNearestBag()
    {
        if (inventoryGrid == null) return;

        LootBag nearest = null;
        float nearestDist = float.MaxValue;

        foreach (LootBag bag in FindObjectsOfType<LootBag>())
        {
            if (bag == null) continue;
            float dist = Vector3.Distance(transform.position, bag.transform.position);
            if (dist <= bagOpenRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = bag;
            }
        }

        if (nearest != null && LootBagUI.Instance != null)
            LootBagUI.Instance.OpenBag(nearest, inventoryGrid);
    }

    // Called by GroundLoot for manual world-drop pickup (non-networked items)
    public bool TryPickupSpecificLoot(GroundLoot loot)
    {
        if (loot == null || loot.itemData == null) return false;
        if (inventoryGrid == null) return false;

        bool added = inventoryGrid.AddItem(loot.itemData);
        if (added)
        {
            if (InventoryPersistenceManager.Instance != null)
                InventoryPersistenceManager.Instance.SaveAfterChange();
            Destroy(loot.gameObject);
            return true;
        }

        return false;
    }
}
