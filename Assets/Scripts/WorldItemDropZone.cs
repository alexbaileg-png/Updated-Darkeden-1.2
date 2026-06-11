using UnityEngine;
using UnityEngine.EventSystems;

public class WorldItemDropZone : MonoBehaviour, IDropHandler
{
    [Header("Drop Settings")]
    public float dropDistanceFromPlayer = 1.5f;
    public float dropHeight = 0f;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        InventorySlot inventorySlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (inventorySlot == null || inventorySlot.currentItem == null)
            return;

        bool dropped = DropItemOnGround(inventorySlot.currentItem);

        if (dropped)
        {
            inventorySlot.ClearSlot();

            if (InventoryPersistenceManager.Instance != null)
                InventoryPersistenceManager.Instance.SaveAfterChange();
        }
    }

    bool DropItemOnGround(ItemData item)
    {
        if (item == null || item.worldLootPrefab == null)
            return false;

        // Use the local player — works regardless of how the player was spawned
        PlayerStats localPlayer = PlayerStats.LocalInstance;
        if (localPlayer == null)
        {
            Debug.LogWarning("[WorldItemDropZone] No local player found.");
            return false;
        }

        Transform playerTransform = localPlayer.transform;

        Vector3 dropPosition = playerTransform.position
                             + playerTransform.forward * dropDistanceFromPlayer;
        dropPosition.y = dropHeight > 0f ? dropHeight : playerTransform.position.y;

        GameObject lootObject = Instantiate(
            item.worldLootPrefab,
            dropPosition,
            item.worldLootPrefab.transform.rotation
        );

        GroundLoot groundLoot = lootObject.GetComponent<GroundLoot>();
        if (groundLoot != null)
        {
            groundLoot.SetItem(item);
            groundLoot.canAutoPickup = false;
            groundLoot.canClickPickup = true;
        }

        return true;
    }
}
