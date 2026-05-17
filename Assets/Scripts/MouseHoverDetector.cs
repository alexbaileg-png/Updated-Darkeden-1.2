using UnityEngine;

public class MouseHoverDetector : MonoBehaviour
{
    public static EnemyHealth CurrentTarget;

    public float hoverRadius = 2.5f;

    private EnemyHighlight currentHighlight;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
            return;

        Vector3 mouseWorld = ray.GetPoint(distance);
        mouseWorld.y = 0f;

        EnemyHighlight[] enemies = FindObjectsOfType<EnemyHighlight>();

        EnemyHighlight closestHighlight = null;
        EnemyHealth closestHealth = null;
        float closestDistance = hoverRadius;

        foreach (EnemyHighlight enemyHighlight in enemies)
        {
            Vector3 enemyPos = enemyHighlight.transform.position;
            enemyPos.y = 0f;

            float dist = Vector3.Distance(mouseWorld, enemyPos);

            if (dist <= closestDistance)
            {
                EnemyHealth health = enemyHighlight.GetComponent<EnemyHealth>();

                if (health != null)
                {
                    closestDistance = dist;
                    closestHighlight = enemyHighlight;
                    closestHealth = health;
                }
            }
        }

        if (currentHighlight != closestHighlight)
        {
            if (currentHighlight != null)
                currentHighlight.SetHighlight(false);

            currentHighlight = closestHighlight;
            CurrentTarget = closestHealth;

            if (currentHighlight != null)
                currentHighlight.SetHighlight(true);
        }
    }
}