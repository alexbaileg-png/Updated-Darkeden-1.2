using UnityEngine;

public class VialHotbar : MonoBehaviour
{
    public static VialHotbar Instance;

    [Header("Panel Root")]
    public GameObject vialPanelRoot;

    [Header("Player")]
    public PlayerStats playerStats;

    [Header("Equipment")]
    public EquipmentManager equipmentManager;

    [Header("Slots")]
    public VialHotbarSlot slot1;
    public VialHotbarSlot slot2;
    public VialHotbarSlot slot3;
    public VialHotbarSlot slot4;

    private VialHotbarSlot[] allSlots;
    private ItemData lastBelt;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        allSlots = new VialHotbarSlot[]
        {
            slot1,
            slot2,
            slot3,
            slot4
        };

        RefreshBeltState();
    }

    void Update()
    {
        RefreshBeltState();

        if (!HasBeltEquipped())
            return;

        if (Input.GetKeyDown(KeyCode.F1)) UseSlot(slot1);
        if (Input.GetKeyDown(KeyCode.F2)) UseSlot(slot2);
        if (Input.GetKeyDown(KeyCode.F3)) UseSlot(slot3);
        if (Input.GetKeyDown(KeyCode.F4)) UseSlot(slot4);
    }

    void UseSlot(VialHotbarSlot slot)
    {
        if (slot == null)
            return;

        slot.Use(playerStats);
        SaveSlotsToBelt();
    }

    bool HasBeltEquipped()
    {
        return equipmentManager != null &&
               equipmentManager.beltSlot != null &&
               equipmentManager.beltSlot.currentItem != null;
    }

    void RefreshBeltState()
    {
        bool hasBelt = HasBeltEquipped();

        if (vialPanelRoot != null)
            vialPanelRoot.SetActive(hasBelt);

        if (!hasBelt)
        {
            lastBelt = null;
            ClearVisualSlots();
            return;
        }

        ItemData currentBelt = equipmentManager.beltSlot.currentItem;

        if (currentBelt != lastBelt)
        {
            lastBelt = currentBelt;
            LoadSlotsFromBelt();
        }
    }

    void ClearVisualSlots()
    {
        if (allSlots == null)
            return;

        foreach (VialHotbarSlot slot in allSlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }
    }

    void LoadSlotsFromBelt()
    {
        if (!HasBeltEquipped())
            return;

        ItemData belt = equipmentManager.beltSlot.currentItem;

        if (belt == null || belt.beltSlots == null)
            return;

        for (int i = 0; i < allSlots.Length; i++)
        {
            if (allSlots[i] == null)
                continue;

            if (i >= belt.beltSlots.Length || belt.beltSlots[i] == null)
            {
                allSlots[i].ClearSlot();
                continue;
            }

            SavedItemData saved = belt.beltSlots[i];

            if (ItemDatabase.Instance == null)
            {
                allSlots[i].ClearSlot();
                continue;
            }

            ItemData item = ItemDatabase.Instance.GetItemById(saved.baseItemId);

            if (item == null)
            {
                allSlots[i].ClearSlot();
                continue;
            }

            allSlots[i].SetItem(item, saved.quantity);
        }
    }

    public void SaveSlotsToBelt()
    {
        if (!HasBeltEquipped())
            return;

        ItemData belt = equipmentManager.beltSlot.currentItem;

        if (belt == null)
            return;

        belt.beltSlots = new SavedItemData[4];

        for (int i = 0; i < allSlots.Length; i++)
        {
            VialHotbarSlot slot = allSlots[i];

            if (slot == null || !slot.HasItem())
                continue;

            SavedItemData save = new SavedItemData();
            save.baseItemId = slot.currentItem.itemId;
            save.quantity = slot.quantity;
            save.rarity = slot.currentItem.rarity;

            belt.beltSlots[i] = save;
        }

        if (InventoryPersistenceManager.Instance != null)
            InventoryPersistenceManager.Instance.SaveAfterChange();
    }
}