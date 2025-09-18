using UnityEngine;
using System.Collections;
public class LookAt : MonoBehaviour
{
    private Vector3 worldPosition;
    private Vector3 screenPosition;
    public GameObject crosshair;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        screenPosition = Input.mousePosition;
        screenPosition.z = 3f;
        screenPosition.x -= 6;

        worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        transform.position = worldPosition;
        crosshair.transform.position = Input.mousePosition;
    }
}
