using UnityEngine;
using UnityEngine.EventSystems;

public class WorldItemDropZone : MonoBehaviour, IDropHandler
{
    [Header("References")]
    public Transform player;

    [Header("Drop Settings")]
    public float dropDistanceFromPlayer = 1.5f;
    public float dropHeight = 0f;

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot inventorySlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (inventorySlot == null)
        {
            Debug.LogError("Drop failed: pointerDrag is not an InventorySlot.");
            return;
        }

        if (inventorySlot.currentItem == null)
        {
            Debug.LogError("Drop failed: inventory slot has no item.");
            return;
        }

        DropItemOnGround(inventorySlot.currentItem);
        inventorySlot.ClearSlot();
    }

    void DropItemOnGround(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("Drop failed: item is null.");
            return;
        }

        if (item.worldLootPrefab == null)
        {
            Debug.LogError("Drop failed: " + item.itemName + " has no World Loot Prefab assigned.");
            return;
        }

        if (player == null)
        {
            Debug.LogError("Drop failed: Player is not assigned on WorldItemDropZone.");
            return;
        }

        Vector3 dropPosition = player.position + player.forward * dropDistanceFromPlayer;
        dropPosition.y = dropHeight;

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
        }

        Debug.Log("Dropped item on ground: " + item.itemName);
    }
}