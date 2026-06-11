using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Assigned automatically by NetworkPlayerController on the local owner.
    // Can still be set manually in the Inspector for single-player testing.
    public Transform target;

    public Vector3 offset = new Vector3(0f, 12f, -10f);

    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        if (target == null)
            return;

        transform.position = target.position + offset;
    }
}