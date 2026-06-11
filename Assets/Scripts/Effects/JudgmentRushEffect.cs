using System.Collections;
using UnityEngine;

/// <summary>
/// Spawned along the JudgmentRush dash path.
/// Stretches itself to match the dash distance, expands outward briefly, then fades out.
/// Attach to the JudgmentRush VFX prefab.
/// </summary>
public class JudgmentRushEffect : MonoBehaviour
{
    [Header("Timing")]
    public float lifetime     = 0.6f;
    public float expandTime   = 0.08f;   // time to reach full scale
    public float holdTime     = 0.15f;   // time at full scale
    // remainder of lifetime is spent fading/shrinking

    [Header("Scale")]
    public float widthScale   = 1.2f;    // X scale of the slash beam
    public float heightScale  = 1.8f;    // Y scale

    [Header("References")]
    [Tooltip("The main slash mesh child. Leave null to use this object's transform.")]
    public Transform slashMesh;

    [Tooltip("Optional glow/trail particles parented to this prefab.")]
    public ParticleSystem[] burstParticles;

    // Set by SwordmasterSkillCaster after Instantiate
    [HideInInspector] public float rushDistance = 8f;

    private Vector3 _fullScale;
    private Renderer[] _renderers;
    private float _timer;

    void Awake()
    {
        Transform target = slashMesh != null ? slashMesh : transform;

        // Length (Z) matches the dash distance; width and height are fixed
        _fullScale = new Vector3(widthScale, heightScale, rushDistance);
        target.localScale = Vector3.zero;

        _renderers = target.GetComponentsInChildren<Renderer>();

        // Fire burst particles immediately
        foreach (var ps in burstParticles)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                ps.Play();
            }
        }

        Destroy(gameObject, lifetime);
        StartCoroutine(AnimateEffect(target));
    }

    IEnumerator AnimateEffect(Transform target)
    {
        // ── Expand ────────────────────────────────────────────────────────────
        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / expandTime);
            target.localScale = Vector3.LerpUnclamped(Vector3.zero, _fullScale,
                                                       EaseOutBack(p));
            yield return null;
        }
        target.localScale = _fullScale;

        // ── Hold ──────────────────────────────────────────────────────────────
        yield return new WaitForSeconds(holdTime);

        // ── Fade out ──────────────────────────────────────────────────────────
        float fadeTime = lifetime - expandTime - holdTime;
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeTime);

            // Shrink width/height but keep length so it looks like it dissipates
            float w = Mathf.Lerp(widthScale,  0f, p);
            float h = Mathf.Lerp(heightScale, 0f, p);
            target.localScale = new Vector3(w, h, rushDistance);

            // Fade renderer alpha
            SetAlpha(1f - p);

            yield return null;
        }
    }

    void SetAlpha(float alpha)
    {
        foreach (Renderer r in _renderers)
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
                // URP/HDRP base color
                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = alpha;
                    m.SetColor("_BaseColor", c);
                }
            }
        }
    }

    // Slight overshoot on expand for a snappy feel
    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
