using UnityEngine;
using System.Collections.Generic;

public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance;

    [Header("All Items")]
    public ItemData[] allItems;

    private Dictionary<string, ItemData> _lookup = new Dictionary<string, ItemData>();

    void Awake()
    {
        Instance = this;
        foreach (ItemData item in allItems)
            if (item != null) _lookup[item.name] = item;
    }

    public ItemData Get(string itemName)
    {
        _lookup.TryGetValue(itemName, out ItemData result);
        return result;
    }

    public string GetKey(ItemData item) => item != null ? item.name : "";
}
