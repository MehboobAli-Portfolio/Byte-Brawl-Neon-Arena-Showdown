using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
public class NickNameScript : MonoBehaviourPunCallbacks
{
    public Text[] names;
    public Image[] healthbars;
    private GameObject waitObject;
    public GameObject displayPanel;
    public Text messageText;
    public int[] kills;
    public int[] deaths;
    public bool teamMode = false;
    public bool ctbMode = false;
    public bool survival = false;
    public GameObject eliminationPanel;
    private void Start()
    {
        if (survival == true || ctbMode == true)
        {
            eliminationPanel.SetActive(false);
        }
        displayPanel.SetActive(false);
        for (int i = 0; i < names.Length; i++)
        {
            names[i].gameObject.SetActive(false);
            healthbars[i].gameObject.SetActive(false);
        }
        waitObject = GameObject.Find("WaitingBG");
        if (deaths.Length != names.Length)
        {
            deaths = new int[names.Length];
        }
    }

    public void Leaving()
    {
        StartCoroutine("BackToLobby");
    }
    IEnumerator BackToLobby()
    {
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.LoadLevel("Lobby");
    }


    //This is for the Waiting screen
    public void ReturnToLobby()
    {
        waitObject.SetActive(false);
        RoomExit();
    }
    void RoomExit()
    {
        StartCoroutine(ToLobby());   
    }

    public void RunMessage(string win , string losse)
    {
        this.GetComponent<PhotonView>().RPC("DisplayMessage", RpcTarget.All, win, losse);
        UpdateKills(win);
        UpdateDeaths(losse);
    }

    void UpdateKills(string win)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].text == win)
            {
                kills[i]++;
            }
        }
    }
    void UpdateDeaths(string losse)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].text == losse)
            {
                deaths[i]++;
            }
        }
    }

    [PunRPC]
    void DisplayMessage(string win, string losse)
    {
        displayPanel.SetActive(true);
        messageText.text = win + " killed " + losse;
        StartCoroutine(SwitchOffMessage());
    }

    IEnumerator SwitchOffMessage()
    {
        yield return new WaitForSeconds(2);
        this.GetComponent<PhotonView>().RPC("MessageOff", RpcTarget.All);
    }

    [PunRPC]
    void MessageOff()
    {
        displayPanel.SetActive(false);
    }

    IEnumerator ToLobby()
    {
        yield return new WaitForSeconds(0.1f);
        Cursor.visible = true;
        PhotonNetwork.LeaveRoom();
    }
    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("Lobby");
    }
    // --- NEW: Bulletproof Photon Server Trigger ---
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // When a defeated player is kicked from the room, check if we are the last one standing!
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            // Safely grab the Canvas from the Timer script attached to this exact same object
            GameObject myCanvas = this.GetComponent<Timer>().Canvas;

            if (myCanvas != null)
            {
                if (survival == true)
                {
                    myCanvas.GetComponent<KillCount>().SurvivalWinner(PhotonNetwork.LocalPlayer.NickName);
                }
                else if (ctbMode == true)
                {
                    myCanvas.GetComponent<TeamKillCount>().CTBWinner();
                }
            }
        }
    }
}