using UnityEngine;

/// <summary>
/// Gun stays rooted to the right hand bone.
/// While isShooting, rotates the gun so its barrel points toward the left hand bone.
/// gripLocalOffset fine-tunes the aim target in world space.
/// </summary>
public class WeaponFollowBone : MonoBehaviour
{
    [HideInInspector] public Transform bone;

    [Header("Base Offset")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Left Hand Grip (only active while shooting)")]
    public Transform leftHandBone;
    public Vector3   gripLocalOffset; // world-space nudge applied to the aim target

    [HideInInspector] public bool isShooting;

    void OnDestroy()
    {
        Debug.LogWarning($"[WeaponFollowBone] Gun visual destroyed: {gameObject.name}", gameObject);
    }

    void LateUpdate()
    {
        if (bone == null) return;

        transform.position = bone.TransformPoint(positionOffset);

        if (isShooting && leftHandBone != null)
        {
            Vector3 target    = leftHandBone.position + gripLocalOffset;
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction, bone.up);
        }
        else
        {
            transform.rotation = bone.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}
