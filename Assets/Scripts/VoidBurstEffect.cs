using UnityEngine;

/// <summary>
/// Attach to the VoidBurst root prefab.
/// Expands the ring outward on X/Z over expandTime, then destroys after totalLifetime.
/// </summary>
public class VoidBurstEffect : MonoBehaviour
{
    [Header("Ring Expansion")]
    public float maxScale    = 12f;   // max XZ diameter in world units
    public float expandTime  = 0.5f;  // seconds to reach full size
    public float totalLifetime = 1.5f; // total seconds before the GameObject is destroyed

    [Header("Ease")]
    public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ── private ───────────────────────────────────────────────────────────────

    private float     _elapsed;
    private float     _startScaleY;
    private Transform _ring;

    void Awake()
    {
        // Find the Cylinder ring — search all children for a MeshRenderer
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>(true);
        if (mr != null)
        {
            _ring        = mr.transform;
            _startScaleY = _ring.localScale.y;
            _ring.localScale = new Vector3(0f, _startScaleY, 0f);
        }
        else
        {
            // Fallback: scale the root itself
            _ring        = transform;
            _startScaleY = transform.localScale.y;
            transform.localScale = new Vector3(0f, _startScaleY, 0f);
        }

        // Stop particle system from looping and let it play once
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            var main  = ps.main;
            main.loop = false;
            ps.Play();
        }

        // Hard destroy after totalLifetime regardless of particle state
        Destroy(gameObject, totalLifetime);
    }

    void Update()
    {
        if (_ring == null) return;
        if (_elapsed >= expandTime) return;

        _elapsed += Time.deltaTime;
        float t     = Mathf.Clamp01(_elapsed / expandTime);
        float eased = expandCurve.Evaluate(t);
        float s     = Mathf.Lerp(0f, maxScale, eased);
        _ring.localScale = new Vector3(s, _startScaleY, s);
    }
}
