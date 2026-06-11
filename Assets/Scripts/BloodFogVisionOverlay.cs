using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen UI overlay that creates a tunnel-vision effect for Slayer players
/// caught inside a Blood Fog cloud.
///
/// Setup: Create a Canvas (Screen Space - Overlay, sort order 99) in your Game scene.
/// Add an Image child covering the full screen with a dark vignette sprite
/// (or use a solid dark color with low alpha ~180).
/// Add this component to the Canvas root. It starts disabled.
/// </summary>
public class BloodFogVisionOverlay : MonoBehaviour
{
    [Tooltip("The dark vignette image. Should be a radial gradient — dark edges, transparent center.")]
    public Image vignetteImage;

    [Tooltip("How quickly the overlay fades in/out.")]
    public float fadeSpeed = 2f;

    private float _targetAlpha = 1f;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;
    }

    void OnEnable()
    {
        _targetAlpha = 1f;
    }

    void OnDisable()
    {
        _targetAlpha = 0f;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha,
                                               fadeSpeed * Time.deltaTime);
    }
}
