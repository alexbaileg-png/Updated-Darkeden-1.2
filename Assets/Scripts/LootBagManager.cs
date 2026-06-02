using System.Collections.Generic;
using UnityEngine;

public class LootBagManager : MonoBehaviour
{
    public static LootBagManager Instance;

    [Header("Bag Prefab")]
    public GameObject lootBagPrefab;

    [Header("Grouping")]
    public float mergeRadius = 5f;
    public float mergeTimeWindow = 8f;
    public float dropHeight = 0f;
    public int maxUniqueItemsPerBag = 20;

    private List<LootBag> activeBags = new List<LootBag>();

    void Awake()
    {
        Instance = this;
    }

    public void AddLoot(ItemData item, Vector3 position)
    {
        if (item == null)
            return;

        LootBag bag = FindMergeBag(position, item);

        if (bag == null)
            bag = CreateBag(position);

        if (bag != null)
            bag.AddItem(item, 1);
    }

    LootBag FindMergeBag(Vector3 position, ItemData item)
    {
        activeBags.RemoveAll(bag => bag == null);

        foreach (LootBag bag in activeBags)
        {
            if (bag != null && bag.CanMerge(position, mergeRadius, mergeTimeWindow, item))
                return bag;
        }

        return null;
    }

    LootBag CreateBag(Vector3 position)
    {
        if (lootBagPrefab == null)
            return null;

        position.y = dropHeight;

        GameObject bagObject = Instantiate(
            lootBagPrefab,
            position,
            lootBagPrefab.transform.rotation
        );

        LootBag bag = bagObject.GetComponent<LootBag>();

        if (bag != null)
        {
            bag.maxUniqueItems = maxUniqueItemsPerBag;
            activeBags.Add(bag);
        }

        return bag;
    }
}