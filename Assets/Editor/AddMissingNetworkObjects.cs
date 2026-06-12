using UnityEditor;
using UnityEngine;
using FishNet.Object;

public static class AddMissingNetworkObjects
{
    [MenuItem("Tools/Fix Missing NetworkObjects")]
    static void Fix()
    {
        string[] paths =
        {
            "Assets/Characters/Healer Prefab.prefab",
            "Assets/EnemyPrefabs/Enemy 1.prefab",
            "Assets/EnemyPrefabs/Meshy_AI_Arms_Wide_in_Desolati_0517012835_texture.prefab",
            "Assets/Enemies/Meshy_AI_Arms_Wide_in_Desolati_0517012835_texture.prefab",
            "Assets/Items/Loot/LootBagPrefab.prefab",
        };

        int fixed = 0;
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Fix] Prefab not found: {path}");
                continue;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root.GetComponent<NetworkObject>() == null)
                {
                    root.AddComponent<NetworkObject>();
                    Debug.Log($"[Fix] Added NetworkObject to {path}");
                    fixed++;
                }
                else
                {
                    Debug.Log($"[Fix] Already has NetworkObject: {path}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Fix] Done — added NetworkObject to {fixed} prefab(s).");
    }
}
