using FishNet.Object;
using UnityEngine;

public class HoverDetector : NetworkBehaviour
{
    public static HoverHighlight CurrentHover;
    public static EnemyHealth CurrentEnemyTarget;

    public float hoverRadius = 20f;

    private HoverHighlight currentHighlight;

    void Update()
    {
        // Only the local owner should detect hovers
        if (!IsOwner) return;

        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
            return;

        Vector3 mouseWorld = ray.GetPoint(distance);
        mouseWorld.y = 0f;

        HoverHighlight[] highlights = FindObjectsOfType<HoverHighlight>();

        HoverHighlight closest = null;
        float closestDistance = hoverRadius;

        foreach (HoverHighlight h in highlights)
        {
            Vector3 objectPosition = h.transform.position;
            objectPosition.y = 0f;

            float dist = Vector3.Distance(mouseWorld, objectPosition);

            if (dist <= closestDistance)
            {
                closestDistance = dist;
                closest = h;
            }
        }

        if (currentHighlight != closest)
        {
            if (currentHighlight != null)
                currentHighlight.SetHighlighted(false);

            currentHighlight = closest;
            CurrentHover = currentHighlight;
            CurrentEnemyTarget = null;

            if (currentHighlight != null)
            {
                Debug.Log("Highlighting: " + currentHighlight.gameObject.name);
                currentHighlight.SetHighlighted(true);
                CurrentEnemyTarget = currentHighlight.GetComponent<EnemyHealth>();
            }
        }
    }
}