using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRotate : MonoBehaviour
{
    public float speed = 20;

    void Update()
    {
        // NO MORE RPCs! Just spin the weapon locally. This stops the invisible error.
        transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}
