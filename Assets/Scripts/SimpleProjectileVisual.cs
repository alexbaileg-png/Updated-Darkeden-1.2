using UnityEngine;

public class SimpleProjectileVisual : MonoBehaviour
{
    public float speed    = 18f;
    public float lifetime = 1.5f;

    private Vector3 _direction = Vector3.forward;

    // Set explicitly by the spawner — travel direction is independent of the mesh's visual rotation.
    public void SetDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
            _direction = direction.normalized;
    }

    void Start()   => Destroy(gameObject, lifetime);
    void Update()  => transform.position += _direction * speed * Time.deltaTime;
}
