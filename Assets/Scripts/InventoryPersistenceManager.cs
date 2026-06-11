using UnityEngine;
using System.IO;

public class InventoryPersistenceManager : MonoBehaviour
{
    public static InventoryPersistenceManager Instance;

    [Header("Save Settings")]
    public string saveFileName = "player_inventory.json";

    private bool isLoading = false;

    private string SavePath
    {
        get
        {
            string prefix = GameSession.Instance?.SelectedCharacter?.characterId ?? "";
            string file = string.IsNullOrEmpty(prefix) ? saveFileName : prefix + "_" + saveFileName;
            return Path.Combine(Application.persistentDataPath, file);
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void OnApplicationQuit()
    {
        SaveInventory();
    }

    public void SaveAfterChange()
    {
        if (isLoading)
            return;

        SaveInventory();
    }

    public void SaveInventory()
    {
        if (InventorySaveManager.Instance == null)
        {
            Debug.LogWarning("Cannot save inventory: InventorySaveManager missing.");
            return;
        }

        InventorySaveData saveData = InventorySaveManager.Instance.CreateSaveData();
        string json = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);

        Debug.Log("Inventory saved to: " + SavePath);
    }

    public void LoadInventory()
    {
        if (InventorySaveManager.Instance == null)
        {
            Debug.LogWarning("Cannot load inventory: InventorySaveManager missing.");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log("No inventory save found yet.");
            return;
        }

        isLoading = true;

        string json = File.ReadAllText(SavePath);
        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

        InventorySaveManager.Instance.LoadSaveData(saveData);

        isLoading = false;

        Debug.Log("Inventory loaded from: " + SavePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Inventory save deleted.");
        }
    }
}