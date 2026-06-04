using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LootBagSelectorButton : MonoBehaviour
{
    public TMP_Text labelText;
    public Button button;

    private LootBag lootBag;

    public void Setup(
        LootBag bag,
        string label)
    {
        lootBag = bag;

        if (labelText != null)
            labelText.text = label;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenBag);
        }
    }

    void OpenBag()
{
    Debug.Log("CLICKED BAG BUTTON");

    if (lootBag == null)
        return;

    PlayerLootPickup pickup = FindObjectOfType<PlayerLootPickup>();

    if (pickup == null)
        return;

    if (LootBagUI.Instance != null)
        LootBagUI.Instance.OpenBag(lootBag, pickup.inventoryGrid);

    if (LootBagSelectorUI.Instance != null)
        LootBagSelectorUI.Instance.Close();
}
}