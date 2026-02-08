using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerCheck : MonoBehaviour
{
    public int maxPlayerInRoom = 4;
    public Text currentPlayer;
    public GameObject hint1;
    public GameObject hint2;
    public GameObject enterButton;
    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayerInRoom)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            hint1.SetActive(false);
            hint2.SetActive(false);
            enterButton.SetActive(true);
        }
        if (enterButton.activeInHierarchy != true)
        {
            currentPlayer.text = PhotonNetwork.CurrentRoom.PlayerCount + " / " + maxPlayerInRoom;
        }
        else
        {
            currentPlayer.text = "";
        }
    }

    public void EnterTheArena()
    {
        this.gameObject.SetActive(false);
    }
}
