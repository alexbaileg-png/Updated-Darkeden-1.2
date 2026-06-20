using UnityEngine;

/// <summary>
/// Expands the orbital impact VFX on X/Z, holds briefly, then fades out all renderers.
/// </summary>
public class OrbitalImpactExpand : MonoBehaviour
{
    public float startScale     = 0.1f;
    public float targetScale    = 1f;
    public float expandDuration = 0.4f;
    public float holdDuration   = 0.3f;  // time at full size before fading
    public float fadeDuration   = 0.6f;  // seconds to fade to invisible

    private float    _elapsed;
    private Renderer[] _renderers;
    private float[]  _startAlphas;

    void Start()
    {
        Vector3 s = transform.localScale;
        s.x = startScale;
        s.z = startScale;
        transform.localScale = s;

        // Cache all renderers and their original alpha values
        _renderers  = GetComponentsInChildren<Renderer>(true);
        _startAlphas = new float[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material mat = _renderers[i].material;
            _startAlphas[i] = mat.HasProperty("_Color") ? mat.color.a : 1f;
        }
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        float expandEnd = expandDuration;
        float holdEnd   = expandEnd + holdDuration;
        float fadeEnd   = holdEnd + fadeDuration;

        if (_elapsed <= expandEnd)
        {
            float t = Mathf.Clamp01(_elapsed / expandDuration);
            float current = Mathf.Lerp(startScale, targetScale, t);
            Vector3 s = transform.localScale;
            s.x = current;
            s.z = current;
            transform.localScale = s;
        }
        else if (_elapsed <= holdEnd)
        {
            // hold at full size — nothing to do
        }
        else if (_elapsed <= fadeEnd)
        {
            float t = Mathf.Clamp01((_elapsed - holdEnd) / fadeDuration);
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material mat = _renderers[i].material;
                if (!mat.HasProperty("_Color")) continue;
                Color c = mat.color;
                c.a = Mathf.Lerp(_startAlphas[i], 0f, t);
                mat.color = c;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
