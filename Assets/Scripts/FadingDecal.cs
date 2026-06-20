using System.Collections.Generic;
using UnityEngine;

// Fades a decal's renderers out over its lifetime, then destroys it.
public class FadingDecal : MonoBehaviour
{
    public float lifetime  = 4f;
    public float fadeStart = 0.5f; // fraction of lifetime when fading begins

    private readonly List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();
    private readonly List<Renderer>       _meshRenderers    = new List<Renderer>();
    private float _elapsed;

    void Start()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is SpriteRenderer sr) _spriteRenderers.Add(sr);
            else _meshRenderers.Add(r);
        }
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float fadeBeginTime = lifetime * fadeStart;

        if (_elapsed >= fadeBeginTime)
        {
            float t     = Mathf.Clamp01((_elapsed - fadeBeginTime) / Mathf.Max(0.01f, lifetime - fadeBeginTime));
            float alpha = 1f - t;

            foreach (SpriteRenderer sr in _spriteRenderers)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            foreach (Renderer mr in _meshRenderers)
            {
                Color c = mr.material.color;
                c.a = alpha;
                mr.material.color = c;
            }
        }

        if (_elapsed >= lifetime)
            Destroy(gameObject);
    }
}
