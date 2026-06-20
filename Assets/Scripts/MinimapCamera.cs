using UnityEngine;

/// <summary>
/// Attach to an orthographic Camera pointed straight down.
/// That camera renders to a RenderTexture assigned to a RawImage on the Canvas.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    [Header("Follow")]
    public float height       = 30f;
    public float smoothSpeed  = 10f;

    [Header("Zoom")]
    public float orthSizeDefault = 20f;
    public float orthSizeMin     = 8f;
    public float orthSizeMax     = 50f;
    public float zoomStep        = 5f;
    public KeyCode zoomInKey     = KeyCode.Minus;
    public KeyCode zoomOutKey    = KeyCode.Equals;

    [Header("Layer Visibility")]
    [Tooltip("Layers to HIDE on the minimap — add your Enemy and Player layers here")]
    public LayerMask excludeLayers;

    Camera _cam;
    Transform _target;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic     = true;
        _cam.orthographicSize = orthSizeDefault;
        transform.rotation    = Quaternion.Euler(90f, 0f, 0f);

        // Strip excluded layers from the culling mask
        _cam.cullingMask &= ~excludeLayers.value;
    }

    void LateUpdate()
    {
        // Resolve player lazily (spawned after scene load)
        if (_target == null)
        {
            PlayerStats ps = PlayerStats.LocalInstance;
            if (ps != null) _target = ps.transform;
        }

        if (_target == null) return;

        Vector3 goal = new Vector3(_target.position.x, _target.position.y + height, _target.position.z);

        transform.position = smoothSpeed > 0f
            ? Vector3.Lerp(transform.position, goal, Time.deltaTime * smoothSpeed)
            : goal;

        // Zoom
        if (Input.GetKeyDown(zoomInKey))
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - zoomStep, orthSizeMin, orthSizeMax);
        if (Input.GetKeyDown(zoomOutKey))
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + zoomStep, orthSizeMin, orthSizeMax);
    }
}
