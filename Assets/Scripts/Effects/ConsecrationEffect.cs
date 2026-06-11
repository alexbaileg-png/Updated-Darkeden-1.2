using System.Collections;
using UnityEngine;

/// <summary>
/// Ground ring that expands outward from the cast point then fades.
/// Attach to the Consecration VFX prefab root.
/// </summary>
public class ConsecrationEffect : MonoBehaviour
{
    [Header("Timing")]
    public float expandTime  = 0.3f;
    public float holdTime    = 0.15f;
    public float fadeTime    = 0.2f;

    [Header("Scale")]
    public float maxRadius   = 5f;    // should match consecrationRadius on SwordmasterSkillCaster

    [Header("References")]
    [Tooltip("The ring mesh child — scaled outward on X and Z only")]
    public Transform ringMesh;
    [Tooltip("Optional burst particles at center")]
    public ParticleSystem[] burstParticles;

    private Renderer[] _renderers;

    void Awake()
    {
        Transform target = ringMesh != null ? ringMesh : transform;
        target.localScale = Vector3.zero;

        _renderers = target.GetComponentsInChildren<Renderer>();

        // Disable gravity on any Rigidbody
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.useGravity  = false;
            rb.isKinematic = true;
        }

        // Fire burst particles
        foreach (ParticleSystem ps in burstParticles)
        {
            if (ps == null) continue;
            var main  = ps.main;
            main.loop = false;
            ps.Play();
        }

        Destroy(gameObject, expandTime + holdTime + fadeTime);
        StartCoroutine(Animate(target));
    }

    IEnumerator Animate(Transform target)
    {
        // ── Expand ring outward ───────────────────────────────────────────────
        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / expandTime);
            float r = Mathf.Lerp(0f, maxRadius, EaseOut(p));
            // Scale X and Z only — keep Y flat on the ground
            target.localScale = new Vector3(r, target.localScale.y > 0 ? target.localScale.y : 0.1f, r);
            yield return null;
        }
        target.localScale = new Vector3(maxRadius, target.localScale.y, maxRadius);

        // ── Hold ──────────────────────────────────────────────────────────────
        yield return new WaitForSeconds(holdTime);

        // ── Fade out ──────────────────────────────────────────────────────────
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeTime);
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
                    Color c = m.color; c.a = alpha; m.color = c;
                }
                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor"); c.a = alpha; m.SetColor("_BaseColor", c);
                }
            }
        }
    }

    float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
