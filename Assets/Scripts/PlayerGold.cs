using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public int gold = 0;

    public void AddGold(int amount)
    {
        gold += Mathf.Max(0, amount);
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
            return false;

        gold -= amount;
        return true;
    }
}