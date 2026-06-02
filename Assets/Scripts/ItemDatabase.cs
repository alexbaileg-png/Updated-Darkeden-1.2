using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("All Item Templates")]
    public ItemData[] allItems;

    private Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();

    void Awake()
    {
        Instance = this;
        BuildDatabase();
    }

    void BuildDatabase()
    {
        itemLookup.Clear();

        foreach (ItemData item in allItems)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.itemId))
            {
                Debug.LogWarning("Item missing ID: " + item.name);
                continue;
            }

            if (itemLookup.ContainsKey(item.itemId))
            {
                Debug.LogError("Duplicate item ID found: " + item.itemId);
                continue;
            }

            itemLookup.Add(item.itemId, item);
        }

        Debug.Log("ItemDatabase loaded " + itemLookup.Count + " items.");
    }

    public ItemData GetItemById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (itemLookup.TryGetValue(itemId, out ItemData item))
            return item;

        Debug.LogWarning("No item found with ID: " + itemId);
        return null;
    }
}