using UnityEngine;

public class SimpleProjectileVisual : MonoBehaviour
{
    public float speed    = 18f;
    public float lifetime = 1.5f;

    void Start()   => Destroy(gameObject, lifetime);
    void Update()  => transform.position += transform.forward * speed * Time.deltaTime;
}
