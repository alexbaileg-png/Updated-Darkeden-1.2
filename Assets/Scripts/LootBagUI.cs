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

    public bool IsShowingBag(LootBag bag) => currentBag == bag && rootPanel != null && rootPanel.activeSelf;

    public void TakeSlot(int index)
    {
        if (currentBag == null) return;
        currentBag.ServerTakeEntry(index);
    }

    public void TakeEntireStack(int index)
    {
        if (currentBag == null || index < 0 || index >= currentBag.items.Count) return;
        currentBag.ServerTakeEntireStack(index);
    }

    public void TakeAll()
    {
        if (currentBag == null) return;
        currentBag.ServerTakeAll();
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