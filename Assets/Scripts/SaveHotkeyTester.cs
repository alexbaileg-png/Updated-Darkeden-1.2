using UnityEngine;

public class SaveHotkeyTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (InventoryPersistenceManager.Instance != null)
            {
                InventoryPersistenceManager.Instance.SaveInventory();
                Debug.Log("Manual save triggered.");
            }
            else
            {
                Debug.LogError("No InventoryPersistenceManager found.");
            }
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (InventoryPersistenceManager.Instance != null)
            {
                InventoryPersistenceManager.Instance.LoadInventory();
                Debug.Log("Manual load triggered.");
            }
            else
            {
                Debug.LogError("No InventoryPersistenceManager found.");
            }
        }
    }
}