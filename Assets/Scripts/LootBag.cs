using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using UnityEngine;
using TMPro;

[System.Serializable]
public struct NetworkLootEntry
{
    public string itemName;
    public int quantity;
}

// Keep for UI compatibility
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

public class LootBag : NetworkBehaviour
{
    [Header("Pickup")]
    public KeyCode openKey = KeyCode.E;
    public float openRange = 3f;

    [Header("Despawn")]
    public float despawnTime = 180f;

    [Header("Visual Label")]
    public GameObject labelRoot;
    public TMP_Text labelText;

    public int maxUniqueItems = 20;

    // Server-authoritative item list — syncs to all clients
    private readonly SyncList<NetworkLootEntry> _networkItems = new SyncList<NetworkLootEntry>();

    // Local cache rebuilt from SyncList for UI use
    public List<LootBagEntry> items = new List<LootBagEntry>();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _networkItems.OnChange += OnNetworkItemsChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _networkItems.OnChange -= OnNetworkItemsChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RebuildLocalCache();
        HideLabel();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Invoke(nameof(ServerDespawn), despawnTime);
    }

    void ServerDespawn()
    {
        if (IsServerStarted)
            ServerManager.Despawn(GetComponent<NetworkObject>());
    }

    void OnNetworkItemsChanged(SyncListOperation op, int index, NetworkLootEntry oldItem, NetworkLootEntry newItem, bool asServer)
    {
        RebuildLocalCache();
        RefreshLabel();

        if (LootBagUI.Instance != null && LootBagUI.Instance.IsShowingBag(this))
            LootBagUI.Instance.Refresh();

        if (_networkItems.Count == 0)
        {
            if (LootBagUI.Instance != null) LootBagUI.Instance.CloseBag();
            if (IsServerStarted) ServerManager.Despawn(GetComponent<NetworkObject>());
        }
    }

    void RebuildLocalCache()
    {
        items.Clear();
        foreach (NetworkLootEntry entry in _networkItems)
        {
            ItemData data = ItemRegistry.Get(entry.itemName);
            if (data != null)
                items.Add(new LootBagEntry(data, entry.quantity));
        }
    }

    // ── Server-side item management ───────────────────────────────────────────

    public bool ServerAddItem(ItemData item, int quantity = 1)
    {
        if (!IsServerStarted || item == null) return false;

        string key = ItemRegistry.GetKey(item);

        if (item.isStackable)
        {
            for (int i = 0; i < _networkItems.Count; i++)
            {
                if (_networkItems[i].itemName == key)
                {
                    _networkItems[i] = new NetworkLootEntry { itemName = key, quantity = _networkItems[i].quantity + quantity };
                    return true;
                }
            }
        }

        if (_networkItems.Count >= maxUniqueItems) return false;
        _networkItems.Add(new NetworkLootEntry { itemName = key, quantity = quantity });
        return true;
    }

    public bool IsFullFor(ItemData item)
    {
        if (item == null) return true;
        string key = ItemRegistry.GetKey(item);
        if (item.isStackable)
            foreach (NetworkLootEntry e in _networkItems)
                if (e.itemName == key) return false;
        return _networkItems.Count >= maxUniqueItems;
    }

    public bool CanMerge(Vector3 position, float radius, ItemData item)
    {
        if (IsFullFor(item)) return false;
        return Vector3.Distance(transform.position, position) <= radius;
    }

    // ── Client requests ───────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void ServerTakeEntry(int index, NetworkConnection sender = null)
    {
        if (index < 0 || index >= _networkItems.Count) return;

        NetworkLootEntry entry = _networkItems[index];
        ItemData item = ItemRegistry.Get(entry.itemName);
        if (item == null) return;

        if (entry.quantity > 1)
            _networkItems[index] = new NetworkLootEntry { itemName = entry.itemName, quantity = entry.quantity - 1 };
        else
            _networkItems.RemoveAt(index);

        TargetAddItemToInventory(sender, entry.itemName, 1);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerTakeEntireStack(int index, NetworkConnection sender = null)
    {
        ServerTakeEntry(index, sender);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerTakeAll(NetworkConnection sender = null)
    {
        List<NetworkLootEntry> snapshot = new List<NetworkLootEntry>(_networkItems);
        _networkItems.Clear();

        foreach (NetworkLootEntry entry in snapshot)
            TargetAddItemToInventory(sender, entry.itemName, entry.quantity);
    }

    [TargetRpc]
    void TargetAddItemToInventory(NetworkConnection conn, string itemName, int quantity)
    {
        ItemData item = ItemRegistry.Get(itemName);
        if (item == null) return;

        InventoryGrid grid = FindObjectOfType<InventoryGrid>(true);
        if (grid == null) return;

        for (int i = 0; i < quantity; i++)
            grid.AddItem(item, 1);
    }

    // ── UI interaction ────────────────────────────────────────────────────────

    void Update()
    {
        if (!IsOwner && !IsClientStarted) return;

        if (Input.GetKeyDown(openKey))
            TryOpen();
    }

    void OnMouseDown() => TryOpen();

    void TryOpen()
    {
        PlayerLootPickup playerPickup = FindObjectOfType<PlayerLootPickup>();
        if (playerPickup == null) return;
        if (Vector3.Distance(playerPickup.transform.position, transform.position) > openRange) return;
        if (LootBagUI.Instance != null)
            LootBagUI.Instance.OpenBag(this, playerPickup.inventoryGrid);
    }

    void RefreshLabel()
    {
        if (labelText != null)
            labelText.text = "Loot Bag (" + _networkItems.Count + "/" + maxUniqueItems + ")";
    }

    void OnMouseEnter() { if (labelRoot != null) labelRoot.SetActive(true); }
    void OnMouseExit() { if (labelRoot != null) labelRoot.SetActive(false); }
    public void ShowLabel() { if (labelRoot != null) labelRoot.SetActive(true); }
    public void HideLabel() { if (labelRoot != null) labelRoot.SetActive(false); }
    public void CheckIfEmpty() { } // kept for UI compatibility — SyncList handles this now
}
