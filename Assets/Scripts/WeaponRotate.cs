using UnityEngine;

public class WeaponRotate : MonoBehaviour
{
    public float rotationSpeed = 30f; // Speed of rotation in degrees per second
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
