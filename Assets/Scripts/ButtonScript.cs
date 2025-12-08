using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ButtonScript : MonoBehaviour
{
    private GameObject[] players;
    private int myID;
    private GameObject panel;

    private void Start()
    {
        Cursor.visible = true;
        panel = GameObject.Find("ChoosePanel");
    }

    public void SelectButton(int buttonNumber)
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            // Added check to prevent crash if PhotonView is missing
            if (players[i].GetComponent<PhotonView>() != null)
            {
                if (players[i].GetComponent<PhotonView>().IsMine == true)
                {
                    myID = players[i].GetComponent<PhotonView>().ViewID;
                    break;
                }
            }
        }

        GetComponent<PhotonView>().RPC("SelectedColor", RpcTarget.AllBuffered, buttonNumber, myID);
        Cursor.visible = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    [PunRPC]
    void SelectedColor(int buttonNumber, int myID)
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            // Added check to prevent crash if DisplayColor script is missing
            if (players[i].GetComponent<DisplayColor>() != null)
            {
                players[i].GetComponent<DisplayColor>().viewID[buttonNumber] = myID;
                players[i].GetComponent<DisplayColor>().ChooseColor();
            }
        }
        this.transform.gameObject.SetActive(false);
    }
}