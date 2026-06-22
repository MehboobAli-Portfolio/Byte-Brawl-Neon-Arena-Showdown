using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerCheck : MonoBehaviour
{
    public int maxPlayerInRoom = 6;
    public Text currentPlayer;
    public GameObject hint1;
    public GameObject hint2;
    public GameObject enterButton;

    [Header("Bot Settings")]
    private bool gameStarting = false;

    void Start()
    {
        // 1. The Host saves the exact Server Time when the room is born!
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            Hashtable hash = new Hashtable();
            hash.Add("StartTime", PhotonNetwork.Time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }
    void Update()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        // If the room fills up naturally with humans
        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayerInRoom && !gameStarting)
        {
            gameStarting = true;
            if (PhotonNetwork.IsMasterClient) PhotonNetwork.CurrentRoom.IsOpen = false;
            UnlockEnterButton();
        }

        // TIMER LOGIC - Synced across all computers!
        if (!gameStarting)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
            {
                // Calculate exactly how much time has passed on the Server
                double startTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["StartTime"];
                float elapsedTime = (float)(PhotonNetwork.Time - startTime);
                float timeRemaining = 60f - elapsedTime;

                if (timeRemaining <= 0) timeRemaining = 0;

                currentPlayer.text = "Starting in: " + Mathf.Ceil(timeRemaining).ToString() + "\n" + PhotonNetwork.CurrentRoom.PlayerCount + " / " + maxPlayerInRoom;

                if (timeRemaining <= 0)
                {
                    gameStarting = true;

                    // ONLY the Master Client spawns the bots
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.CurrentRoom.IsOpen = false;
                        SpawnBotsToFillRoom();
                    }

                    // ALL computers unlock the button instantly
                    UnlockEnterButton();
                }
            }
        }
    }

    void SpawnBotsToFillRoom()
    {
        int botsNeeded = maxPlayerInRoom - PhotonNetwork.CurrentRoom.PlayerCount;

        for (int i = 0; i < botsNeeded; i++)
        {
            PhotonNetwork.InstantiateRoomObject("AIBotPlayer", Vector3.zero, Quaternion.identity);
        }
    }

    void UnlockEnterButton()
    {
        hint1.SetActive(false);
        hint2.SetActive(false);
        enterButton.SetActive(true);
        currentPlayer.text = "Arena Ready!";
    }

    public void EnterTheArena()
    {
        this.gameObject.SetActive(false);
    }
}