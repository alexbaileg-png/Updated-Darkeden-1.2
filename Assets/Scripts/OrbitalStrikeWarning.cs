using UnityEngine;

/// <summary>
/// Drives the Orbital Strike warning prefab:
///   Phase 1 — orbits the perimeter of the AoE radius
///   Phase 2 — spirals inward to the center
/// Destroy this object when done; the caster spawns the impact separately.
/// </summary>
public class OrbitalStrikeWarning : MonoBehaviour
{
    [HideInInspector] public Vector3 center;
    [HideInInspector] public float   radius;
    [HideInInspector] public float   duration;

    // Fraction of total duration spent orbiting before spiraling in
    [Range(0f, 1f)]
    public float orbitFraction  = 0.6f;
    public float orbitSpeed     = 540f;  // degrees per second during orbit phase
    public float spiralExtraRot = 720f;  // extra degrees rotated during spiral-in

    private float _elapsed;
    private float _angle;

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);

        float orbitEnd = orbitFraction;

        if (t <= orbitEnd)
        {
            // ── Phase 1: orbit perimeter ──────────────────────────────────────
            float dt     = Time.deltaTime;
            _angle      += orbitSpeed * dt;
            float rad    = _angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
        }
        else
        {
            // ── Phase 2: spiral inward ────────────────────────────────────────
            float spiralT  = Mathf.Clamp01((t - orbitEnd) / (1f - orbitEnd));
            float curRadius = Mathf.Lerp(radius, 0f, spiralT);
            _angle         += (orbitSpeed + spiralExtraRot * spiralT) * Time.deltaTime;
            float rad       = _angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(rad) * curRadius, 0f, Mathf.Sin(rad) * curRadius);
        }

        if (_elapsed >= duration)
            Destroy(gameObject);
    }
}
