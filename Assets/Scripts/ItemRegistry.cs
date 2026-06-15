using UnityEngine;
using System.Collections.Generic;

public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance { get; private set; }

    [Header("All Items (optional — auto-discovered if empty)")]
    public ItemData[] allItems;

    private static readonly Dictionary<string, ItemData> _lookup = new Dictionary<string, ItemData>();

    void Awake()
    {
        Instance = this;
        BuildLookup();
    }

    // Called by Get() if no scene instance exists yet
    static void EnsureBuilt()
    {
        if (_lookup.Count > 0) return;
        foreach (ItemData item in Resources.FindObjectsOfTypeAll<ItemData>())
            if (item != null && !_lookup.ContainsKey(item.name))
                _lookup[item.name] = item;
    }

    void BuildLookup()
    {
        _lookup.Clear();
        // Manually listed items take priority
        if (allItems != null)
            foreach (ItemData item in allItems)
                if (item != null) _lookup[item.name] = item;

        // Fill remaining from all loaded assets
        foreach (ItemData item in Resources.FindObjectsOfTypeAll<ItemData>())
            if (item != null && !_lookup.ContainsKey(item.name))
                _lookup[item.name] = item;
    }

    public static ItemData Get(string itemName)
    {
        EnsureBuilt();
        _lookup.TryGetValue(itemName, out ItemData result);
        return result;
    }

    public static string GetKey(ItemData item)
    {
        if (item == null) return "";
        // Rolled items have a unique runtimeId stamped by ItemRoller — use that so
        // each rolled instance is stored and retrieved independently.
        string key = !string.IsNullOrEmpty(item.runtimeId) ? item.runtimeId : item.name;
        if (!_lookup.ContainsKey(key))
            _lookup[key] = item;
        return key;
    }
}
