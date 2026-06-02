using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class LootBagEntry
{
    public ItemData item;
    public int quantity;

    public LootBagEntry(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

public class LootBag : MonoBehaviour
{
    [Header("Loot")]
    public List<LootBagEntry> items = new List<LootBagEntry>();
    public int maxUniqueItems = 20;

    [Header("Pickup")]
    public KeyCode openKey = KeyCode.E;
    public float openRange = 3f;

    [Header("Despawn")]
    public float despawnTime = 180f;

    [Header("Visual Label")]
    public GameObject labelRoot;
    public TMP_Text labelText;

    [HideInInspector] public float lastAddedTime;

    void Start()
    {
        lastAddedTime = Time.time;

        RefreshLabel();
        HideLabel();

        Destroy(gameObject, despawnTime);
    }

    void Update()
    {
        if (Input.GetKeyDown(openKey))
            TryOpen();

        CheckIfEmpty();
    }

    public bool IsFullFor(ItemData item)
    {
        if (item == null)
            return true;

        if (item.isStackable)
        {
            foreach (LootBagEntry entry in items)
            {
                if (entry.item == item)
                    return false;
            }
        }

        return items.Count >= maxUniqueItems;
    }

    public bool CanMerge(Vector3 position, float radius, float timeWindow, ItemData item)
    {
        if (IsFullFor(item))
            return false;

        float distance = Vector3.Distance(transform.position, position);

        return distance <= radius;
    }

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null)
            return false;

        if (IsFullFor(item))
            return false;

        lastAddedTime = Time.time;

        if (item.isStackable)
        {
            foreach (LootBagEntry entry in items)
            {
                if (entry.item == item)
                {
                    entry.quantity += quantity;
                    RefreshLabel();
                    return true;
                }
            }
        }

        items.Add(new LootBagEntry(item, quantity));
        RefreshLabel();

        return true;
    }

    void OnMouseDown()
    {
        TryOpen();
    }

    void TryOpen()
    {
        PlayerLootPickup playerPickup = FindObjectOfType<PlayerLootPickup>();

        if (playerPickup == null)
            return;

        float distance = Vector3.Distance(playerPickup.transform.position, transform.position);

        if (distance > openRange)
            return;

        if (LootBagUI.Instance != null)
            LootBagUI.Instance.OpenBag(this, playerPickup.inventoryGrid);
    }
public bool TakeEntireStack(int index, InventoryGrid inventoryGrid)
{
    if (inventoryGrid == null)
        return false;

    if (index < 0 || index >= items.Count)
        return false;

    LootBagEntry entry = items[index];

    if (entry == null || entry.item == null)
        return false;

    while (entry.quantity > 0)
    {
        bool added = inventoryGrid.AddItem(entry.item, 1);

        if (!added)
            break;

        entry.quantity--;
    }

    if (entry.quantity <= 0)
        items.RemoveAt(index);

    RefreshLabel();
    CheckIfEmpty();

    return true;
}
    public bool TakeEntry(int index, InventoryGrid inventoryGrid)
    {
        if (inventoryGrid == null)
            return false;

        if (index < 0 || index >= items.Count)
            return false;

        LootBagEntry entry = items[index];

        if (entry == null || entry.item == null)
            return false;

        bool added = inventoryGrid.AddItem(entry.item, 1);

        if (!added)
            return false;

        entry.quantity--;

        if (entry.quantity <= 0)
            items.RemoveAt(index);

        RefreshLabel();
        CheckIfEmpty();

        return true;
    }

    public void TakeAll(InventoryGrid inventoryGrid)
    {
        if (inventoryGrid == null)
            return;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            LootBagEntry entry = items[i];

            if (entry == null || entry.item == null)
                continue;

            while (entry.quantity > 0)
            {
                bool added = inventoryGrid.AddItem(entry.item, 1);

                if (!added)
                    break;

                entry.quantity--;
            }

            if (entry.quantity <= 0)
                items.RemoveAt(i);
        }

        RefreshLabel();
        CheckIfEmpty();
    }

    public void CheckIfEmpty()
    {
        items.RemoveAll(entry =>
            entry == null ||
            entry.item == null ||
            entry.quantity <= 0
        );

        if (items.Count > 0)
            return;

        if (LootBagUI.Instance != null)
            LootBagUI.Instance.CloseBag();

        Destroy(gameObject);
    }

    void RefreshLabel()
    {
        if (labelText != null)
            labelText.text = "Loot Bag (" + items.Count + "/" + maxUniqueItems + ")";
    }

    void OnMouseEnter()
    {
        ShowLabel();
    }

    void OnMouseExit()
    {
        HideLabel();
    }

    public void ShowLabel()
    {
        if (labelRoot != null)
            labelRoot.SetActive(true);
    }

    public void HideLabel()
    {
        if (labelRoot != null)
            labelRoot.SetActive(false);
    }
}