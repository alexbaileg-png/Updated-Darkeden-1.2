using UnityEngine;

public class HolyRainEffect : MonoBehaviour
{
    public float lifetime = 1.25f;
    public float pulseSpeed = 6f;
    public float maxScale = 1.4f;

    private Vector3 startScale;
    private float timer;

    void Start()
    {
        startScale = transform.localScale;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        timer += Time.deltaTime;

        float pulse = 1f + Mathf.Sin(timer * pulseSpeed) * 0.15f;
        float grow = Mathf.Lerp(1f, maxScale, timer / lifetime);

        transform.localScale = startScale * pulse * grow;
    }
}