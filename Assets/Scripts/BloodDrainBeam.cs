using UnityEngine;

/// <summary>
/// Stretches a mesh-based beam prefab from caster to target.
/// Works with any primitive stretched along any axis (X, Y, or Z).
/// Self-destructs after duration expires.
/// </summary>
public class BloodDrainBeam : MonoBehaviour
{
    public Transform source;
    public Transform target;
    public float duration = 2f;

    private float _elapsed = 0f;
    private Vector3 _baseScale;
    private Vector3 _longAxis; // local axis the mesh was stretched along

    void Awake()
    {
        _baseScale = transform.localScale;

        // Detect which local axis is the longest — that's the beam direction
        if (_baseScale.z >= _baseScale.x && _baseScale.z >= _baseScale.y)
            _longAxis = Vector3.forward;
        else if (_baseScale.x >= _baseScale.y && _baseScale.x >= _baseScale.z)
            _longAxis = Vector3.right;
        else
            _longAxis = Vector3.up;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        if (source != null && target != null)
            StretchToTarget();

        if (_elapsed >= duration)
            Destroy(gameObject);
    }

    void StretchToTarget()
    {
        Vector3 from = source.position + Vector3.up * 1f;
        Vector3 to   = target.position + Vector3.up * 1f;
        Vector3 dir  = to - from;
        float dist   = dir.magnitude;

        if (dist < 0.01f) return;

        // Place at midpoint
        transform.position = (from + to) * 0.5f;

        // Rotate so the long axis points toward the target
        transform.rotation = Quaternion.FromToRotation(_longAxis, dir.normalized);

        // Stretch the long axis to cover the full distance, keep width/height as designed
        Vector3 newScale = _baseScale;
        if (_longAxis == Vector3.forward)
            newScale.z = dist;
        else if (_longAxis == Vector3.right)
            newScale.x = dist;
        else
            newScale.y = dist;

        transform.localScale = newScale;
    }
}
