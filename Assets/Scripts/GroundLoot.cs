using UnityEngine;
using TMPro;

public class GroundLoot : MonoBehaviour
{
    public ItemData itemData;

    [Header("Pickup Rules")]
    public bool canAutoPickup = true;
    public float pickupDelay = 0.5f;

    [Header("Manual Pickup")]
    public bool canClickPickup = true;
    public KeyCode pickupKey = KeyCode.E;
    public float manualPickupRange = 3f;

    [Header("Despawn")]
    public float despawnTime = 180f;

    [Header("Visual Label")]
    public GameObject labelRoot;
    public TMP_Text itemNameText;

    [Header("Motion")]
    public bool rotate = false;
    public float rotateSpeed = 60f;

    public bool bob = false;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    private float spawnTime;
    private Vector3 startPosition;

    void Start()
    {
        spawnTime = Time.time;
        startPosition = transform.position;

        RefreshVisuals();
        HideLabel();

        Destroy(gameObject, despawnTime);
    }

    void Update()
    {
        if (rotate)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        if (bob)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        if (Input.GetKeyDown(pickupKey))
        {
            TryManualPickup();
        }
    }

    public void SetItem(ItemData item)
    {
        itemData = item;
        RefreshVisuals();
        HideLabel();
    }

    void RefreshVisuals()
    {
        if (itemData == null)
            return;

        if (itemNameText != null)
            itemNameText.text = itemData.itemName;
    }

    void OnMouseDown()
    {
        if (!canClickPickup)
            return;

        TryManualPickup();
    }

    void TryManualPickup()
    {
        PlayerLootPickup playerPickup = FindObjectOfType<PlayerLootPickup>();

        if (playerPickup == null)
            return;

        float distance = Vector3.Distance(playerPickup.transform.position, transform.position);

        if (distance > manualPickupRange)
            return;

        playerPickup.TryPickupSpecificLoot(this);
    }

    void OnMouseEnter()
    {
        ShowLabel();
    }

    void OnMouseExit()
    {
        HideLabel();
    }

    public void ShowLabel()
    {
        if (labelRoot != null)
            labelRoot.SetActive(true);
    }

    public void HideLabel()
    {
        if (labelRoot != null)
            labelRoot.SetActive(false);
    }

    public bool CanBeAutoPickedUp()
    {
        return canAutoPickup &&
               Time.time >= spawnTime + pickupDelay;
    }
}