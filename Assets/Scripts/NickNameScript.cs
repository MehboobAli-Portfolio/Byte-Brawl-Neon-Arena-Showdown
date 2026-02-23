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
    public bool teamMode = false;
    public bool ctbMode = false;
    public bool survival = false;
    public GameObject eliminationPanel;
    private void Start()
    {
        eliminationPanel.SetActive(false);
        displayPanel.SetActive(false);
        for (int i = 0; i < names.Length; i++)
        {
            names[i].gameObject.SetActive(false);
            healthbars[i].gameObject.SetActive(false);
        }
        waitObject = GameObject.Find("WaitingBG");
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
}