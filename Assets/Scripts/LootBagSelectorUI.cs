using System.Collections.Generic;
using UnityEngine;

public class LootBagSelectorUI : MonoBehaviour
{
    public static LootBagSelectorUI Instance;

    [Header("UI")]
    public GameObject rootPanel;
    public Transform buttonParent;
    public GameObject buttonPrefab;

    [Header("Settings")]
    public float detectionRange = 10f;

    [Header("Player")]
    public PlayerLootPickup playerLootPickup;

    private bool isOpen = false;

    void Awake()
    {
        Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            isOpen = !isOpen;

            if (rootPanel != null)
                rootPanel.SetActive(isOpen);

            if (isOpen)
                RefreshNearbyBags();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Close()
    {
        isOpen = false;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void RefreshNearbyBags()
    {
        if (playerLootPickup == null)
            return;

        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        LootBag[] allBags = FindObjectsOfType<LootBag>();

        foreach (LootBag bag in allBags)
        {
            if (bag == null)
                continue;

            float distance = Vector3.Distance(
                playerLootPickup.transform.position,
                bag.transform.position
            );

            if (distance > detectionRange)
                continue;

            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);

            LootBagSelectorButton button =
                buttonObj.GetComponent<LootBagSelectorButton>();

            if (button != null)
            {
                button.Setup(
                    bag,
                    "Loot Bag (" + bag.items.Count + " items)"
                );
            }
        }
    }
}