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

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (this.gameObject.GetComponentInParent<PhotonView>().IsMine == true)
        {
            this.transform.position = LookAtObject.transform.position;
        }
    }
}
