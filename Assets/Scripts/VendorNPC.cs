using UnityEngine;

public class VendorNPC : MonoBehaviour
{
    [Header("Merchant")]
    public string merchantName = "Merchant";

    [Header("Dialogue")]
    public MerchantDialogueUI merchantDialogueUI;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3f;

    void Update()
    {
        // Always resolve from LocalInstance — works with network-spawned players
        PlayerStats localPlayer = PlayerStats.LocalInstance;
        if (localPlayer == null || merchantDialogueUI == null)
            return;

        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);

        if (distance <= interactDistance && Input.GetKeyDown(interactKey))
            merchantDialogueUI.OpenDialogue(merchantName);

        if (distance > interactDistance)
            merchantDialogueUI.CloseAll();
    }
}
