using UnityEngine;
using TMPro;

public class FloatingCombatText : MonoBehaviour
{
    public TextMeshPro text;
    public float floatSpeed = 1.5f;
    public float lifetime = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Camera.main.transform.up * floatSpeed * Time.deltaTime;

        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }

    public void SetText(string value)
    {
        if (text != null)
            text.text = value;
    }
}