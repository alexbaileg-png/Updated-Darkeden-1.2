using UnityEngine;

public class SpinForever : MonoBehaviour
{
    public float degreesPerSecond = 90f;
    public Vector3 axis = Vector3.up;

    void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.World);
}
