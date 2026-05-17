using UnityEngine;

public class HoverHighlight : MonoBehaviour
{
    public GameObject outlineObject;

    public void SetHighlighted(bool highlighted)
    {
        if (outlineObject == null)
        {
            Debug.LogError(gameObject.name + " outlineObject is missing.");
            return;
        }

        Debug.LogError(gameObject.name + " turning outline " + highlighted);

        outlineObject.SetActive(highlighted);
    }
}