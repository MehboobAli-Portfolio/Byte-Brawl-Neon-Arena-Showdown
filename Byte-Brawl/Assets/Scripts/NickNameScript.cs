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
    public GameObject[] colorButtons;
    private Color[] originalNameColors;
    private Color[] originalHealthColors;
    // This dictionary keeps track of the 2-minute doom timers for disconnected players
    private Dictionary<string, Coroutine> disconnectTimers = new Dictionary<string, Coroutine>();
    private void Start()
    {
        originalNameColors = new Color[names.Length];
        originalHealthColors = new Color[healthbars.Length];
        for (int i = 0; i < names.Length; i++)
        {
            originalNameColors[i] = names[i].color;
            originalHealthColors[i] = healthbars[i].color;
        }
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

    // --- RECONNECTION LOGIC: Handle Internet Loss vs Rage Quitting ---
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // 1. IsInactive means they LOST CONNECTION but the server is waiting for them
        if (otherPlayer.IsInactive)
        {
            displayPanel.SetActive(true);
            messageText.text = otherPlayer.NickName + " disconnected! 2 mins to rejoin...";
            StartCoroutine(SwitchOffMessage());

            // Turn their Name Gray and Healthbar Black, and start the timer
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].text == otherPlayer.NickName)
                {
                    names[i].color = Color.gray;
                    healthbars[i].color = Color.black;

                    Coroutine doomTimer = StartCoroutine(DisconnectCountdown(i, otherPlayer.NickName));
                    disconnectTimers[otherPlayer.NickName] = doomTimer;
                    break;
                }
            }
        }
        else
        {
            // 2. They intentionally clicked "Quit" or their 2 minutes ran out. Eliminate them instantly.
            ForceEliminatePlayer(otherPlayer.NickName);
        }
    }
    // --- RECONNECTION LOGIC: If they get their internet back and rejoin! ---
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (disconnectTimers.ContainsKey(newPlayer.NickName))
        {
            // Stop the 2-minute doom timer!
            StopCoroutine(disconnectTimers[newPlayer.NickName]);
            disconnectTimers.Remove(newPlayer.NickName);

            displayPanel.SetActive(true);
            messageText.text = newPlayer.NickName + " RECONNECTED!";
            StartCoroutine(SwitchOffMessage());

            // Restore their UI colors back to normal white
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].text == newPlayer.NickName)
                {
                    names[i].color = originalNameColors[i];
                    healthbars[i].color = originalHealthColors[i];
                    break;
                }
            }
        }
    }

    // --- The 2-Minute Waiting Timer ---
    IEnumerator DisconnectCountdown(int uiIndex, string droppedName)
    {
        // Wait exactly 2 minutes
        yield return new WaitForSeconds(120f);

        // If this code runs, they failed to reconnect in time. 
        ForceEliminatePlayer(droppedName);
    }

    // --- Force Elimination if time runs out or they quit ---
    void ForceEliminatePlayer(string droppedName)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].text == droppedName)
            {
                // Force health to 0 and hide their UI entirely
                healthbars[i].fillAmount = 0;
                names[i].gameObject.SetActive(false);
                healthbars[i].gameObject.SetActive(false);
                break;
            }
        }

        displayPanel.SetActive(true);
        messageText.text = droppedName + " abandoned the match.";
        StartCoroutine(SwitchOffMessage());

        // Now check if the remaining player is the last one standing for Survival/CTB!
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
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