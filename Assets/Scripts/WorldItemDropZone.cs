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
        if (eventData == null || eventData.pointerDrag == null)
            return;

        InventorySlot inventorySlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (inventorySlot == null)
            return;

        if (inventorySlot.currentItem == null)
            return;

        bool dropped = DropItemOnGround(inventorySlot.currentItem);

        if (dropped)
            inventorySlot.ClearSlot();
    }

    bool DropItemOnGround(ItemData item)
    {
        if (item == null)
            return false;

        if (item.worldLootPrefab == null)
            return false;

        if (player == null)
            return false;

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
            groundLoot.canClickPickup = true;
        }

        return true;
    }
}