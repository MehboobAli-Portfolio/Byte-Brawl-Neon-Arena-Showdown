using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;

public class DisplayColor : MonoBehaviourPunCallbacks
{
    public int[] buttonNumbers;
    public int[] viewID;
    public Color32[] colors;

    public void ChooseColor()
    {
        GetComponent<PhotonView>().RPC("AssignColor",RpcTarget.AllBuffered);
    }
    [PunRPC]
    void AssignColor()
    {
        for (int i = 0; i < viewID.Length; i++)
        {
            if (this.GetComponent<PhotonView>().ViewID == viewID[i])
            {
                this.transform.GetChild(1).GetComponent<Renderer>().material.color = colors[i];
            }
        }
    }


}
