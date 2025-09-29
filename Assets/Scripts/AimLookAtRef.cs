using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AimLookAtRef : MonoBehaviour
{
    private GameObject LookAtObject;

    void Start()
    {
        LookAtObject = GameObject.Find("AimRef");
        if (LookAtObject == null)
        {
            Debug.LogWarning("AimRef GameObject not found!");
        }
    }

    void FixedUpdate()
    {
        if(this.gameObject.GetComponentInParent<PhotonView>().IsMine)
        {
            this.transform.position = LookAtObject.transform.position;
        }
    }
}
