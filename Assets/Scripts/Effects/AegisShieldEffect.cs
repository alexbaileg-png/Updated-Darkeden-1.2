using UnityEngine;

// Attach to the Aegis of Faith shield prefab.
// Set duration from SwordmasterSkillCaster after instantiation.
public class AegisShieldEffect : MonoBehaviour
{
    [Header("Blink Settings")]
    public float blinkWarningTime  = 10f;  // seconds remaining when blinking starts
    public float blinkRateStart    = 0.6f; // seconds per blink cycle at warning start
    public float blinkRateEnd      = 0.1f; // seconds per blink cycle at expiry

    [HideInInspector] public float duration = 30f;

    private Renderer[] _renderers;
    private float _elapsed;
    private float _blinkTimer;
    private bool  _visible = true;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float remaining = duration - _elapsed;

        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (remaining <= blinkWarningTime)
        {
            // Interpolate blink rate: slow at warning start, fast near expiry
            float t        = 1f - (remaining / blinkWarningTime);
            float blinkRate = Mathf.Lerp(blinkRateStart, blinkRateEnd, t);

            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= blinkRate)
            {
                _blinkTimer = 0f;
                _visible = !_visible;
                SetVisible(_visible);
            }
        }
        else
        {
            if (!_visible)
            {
                _visible = true;
                SetVisible(true);
            }
        }
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in _renderers)
            r.enabled = visible;
    }
}
