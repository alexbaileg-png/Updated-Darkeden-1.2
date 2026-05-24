using UnityEngine;

public class CombatTextSpawner : MonoBehaviour
{
    public static CombatTextSpawner Instance;

    public GameObject combatTextPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnText(Vector3 position, string text)
    {
        if (combatTextPrefab == null)
            return;

        GameObject obj = Instantiate(
            combatTextPrefab,
            position,
            Quaternion.identity
        );

        FloatingCombatText floatingText = obj.GetComponent<FloatingCombatText>();

        if (floatingText != null)
            floatingText.SetText(text);
    }
}