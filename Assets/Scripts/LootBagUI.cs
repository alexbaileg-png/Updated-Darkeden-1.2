using UnityEngine;
using TMPro;

public class LootBagUI : MonoBehaviour
{
    public static LootBagUI Instance;

    [Header("UI")]
    public GameObject rootPanel;
    public TMP_Text titleText;
    public Transform slotParent;
    public GameObject lootBagSlotPrefab;

    [Header("Hover Info Panel")]
   public LootBagHoverInfoUI hoverInfoUI;

    private LootBag currentBag;
    private InventoryGrid currentInventory;

    void Awake()
    {
        Instance = this;
        CloseBag();
    }

    void Update()
    {
        if (rootPanel == null || !rootPanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            CloseBag();
    }

    public void OpenBag(LootBag bag, InventoryGrid inventoryGrid)
    {
        currentBag = bag;
        currentInventory = inventoryGrid;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        Refresh();
    }

    public void Refresh()
    {
        if (slotParent == null || lootBagSlotPrefab == null || currentBag == null)
            return;

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        if (titleText != null)
            titleText.text = "Loot Bag";

        for (int i = 0; i < currentBag.items.Count; i++)
        {
            LootBagEntry entry = currentBag.items[i];

            GameObject slotObject = Instantiate(lootBagSlotPrefab, slotParent);
            LootBagSlot slot = slotObject.GetComponent<LootBagSlot>();

            if (slot != null)
                slot.Setup(this, currentBag, i, entry.item, entry.quantity);
        }

        if (hoverInfoUI != null)
            hoverInfoUI.Hide();
    }

    public void ShowHoverInfo(ItemData item)
    {
        if (hoverInfoUI == null || item == null)
            return;

        hoverInfoUI.ShowItem(item);
    }

    public void HideHoverInfo()
    {
        if (hoverInfoUI != null)
            hoverInfoUI.Hide();
    }

    public void TakeSlot(int index)
    {
        if (currentBag == null || currentInventory == null)
            return;

        currentBag.TakeEntry(index, currentInventory);
        Refresh();
    }

    public void TakeEntireStack(int index)
    {
        if (currentBag == null || currentInventory == null)
            return;

        if (index < 0 || index >= currentBag.items.Count)
            return;

        LootBagEntry entry = currentBag.items[index];

        if (entry == null || entry.item == null)
            return;

        int amount = entry.quantity;

        for (int i = 0; i < amount; i++)
        {
            bool added = currentInventory.AddItem(entry.item, 1);

            if (!added)
                break;

            entry.quantity--;
        }

        if (entry.quantity <= 0)
            currentBag.items.RemoveAt(index);

        currentBag.CheckIfEmpty();
        Refresh();
    }

    public void TakeAll()
    {
        if (currentBag == null || currentInventory == null)
            return;

        currentBag.TakeAll(currentInventory);
        Refresh();
    }

    public void CloseBag()
    {
        currentBag = null;
        currentInventory = null;

        HideHoverInfo();

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}