using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LookAt : MonoBehaviour
{
    private Vector3 worldPosition;
    public GameObject crosshair;
    [Header("Aim Settings")]
    public float heightOffset = 1.5f;
    void Start()
    {
        // Traps the cursor inside the game window so it NEVER disappears off the side!
        Cursor.lockState = CursorLockMode.Confined;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        // 1. Clamp the mouse coordinates so it physically cannot leave the screen edges
        Vector3 clampedMouse = Input.mousePosition;
        clampedMouse.x = Mathf.Clamp(clampedMouse.x, 0, Screen.width);
        clampedMouse.y = Mathf.Clamp(clampedMouse.y, 0, Screen.height);
        clampedMouse.z = 9f; // Distance from camera

        // 2. Convert to 3D space and apply the height fix for the supervisor
        worldPosition = Camera.main.ScreenToWorldPoint(clampedMouse);
        worldPosition.y += heightOffset;

        // 3. Smoothly move the aim target
        transform.position = Vector3.Lerp(transform.position, worldPosition, Time.deltaTime * 20f);

        // 4. Update the 2D crosshair UI
        crosshair.transform.position = clampedMouse;
    }

}
