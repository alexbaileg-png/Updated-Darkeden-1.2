using UnityEngine;

// Pure visual — orbits around the target transform.
// All healing is handled server-side by HealerSkillCaster.
public class HealingOrbitBuff : MonoBehaviour
{
    public Transform target;
    public float orbitRadius = 1.5f;
    public float orbitSpeed  = 420f;
    public float duration    = 10f;

    private float _currentAngle;
    private float _endTime;

    void Start() => _endTime = Time.time + duration;

    void Update()
    {
        if (target == null || Time.time >= _endTime)
        {
            Destroy(gameObject);
            return;
        }

        _currentAngle += orbitSpeed * Time.deltaTime;
        if (_currentAngle >= 360f) _currentAngle -= 360f;

        float rad = _currentAngle * Mathf.Deg2Rad;
        transform.position = target.position + new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            1.1f,
            Mathf.Sin(rad) * orbitRadius
        );
    }
}
