using UnityEngine;

public class VendorNPC : MonoBehaviour
{
    [Header("Merchant")]
    public string merchantName = "Merchant";

    [Header("Dialogue")]
    public MerchantDialogueUI merchantDialogueUI;

    [Header("Player")]
    public Transform player;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3f;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    void Update()
    {
        if (player == null || merchantDialogueUI == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactDistance && Input.GetKeyDown(interactKey))
        {
            merchantDialogueUI.OpenDialogue(merchantName);
        }

        if (distance > interactDistance)
        {
            merchantDialogueUI.CloseAll();
        }
    }
}