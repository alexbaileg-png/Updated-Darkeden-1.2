using UnityEngine;

public class VendorNPC : MonoBehaviour
{
    [Header("UI")]
    public GameObject vendorPanel;

    [Header("Player")]
    public Transform player;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3f;

    void Start()
    {
        if (vendorPanel != null)
            vendorPanel.SetActive(false);

        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    void Update()
    {
        if (player == null || vendorPanel == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactDistance && Input.GetKeyDown(interactKey))
        {
            vendorPanel.SetActive(!vendorPanel.activeSelf);
        }

        if (distance > interactDistance && vendorPanel.activeSelf)
        {
            vendorPanel.SetActive(false);
        }
    }
}