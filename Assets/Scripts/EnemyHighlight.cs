using UnityEngine;

public class EnemyHighlight : MonoBehaviour
{
    public GameObject outlineObject;

    public void SetHighlight(bool highlighted)
    {
        if (outlineObject != null)
        {
            outlineObject.SetActive(highlighted);
        }
    }
}