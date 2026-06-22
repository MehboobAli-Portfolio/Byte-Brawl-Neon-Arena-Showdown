using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text minutesText;
    public Text secondsText;
    public int minutes = 4;
    public int seconds = 59;
    public GameObject Canvas;

    [HideInInspector]
    public bool timeStop = false;

    [HideInInspector]
    public int currentMatchID = -1;

    public async void BeginTimer()
    {
        minutes = 4;
        seconds = 59;
        if (PhotonNetwork.IsMasterClient)
        {
            int newMatchID = await CreateMatchSession();
            GetComponent<PhotonView>().RPC("SyncMatchID", RpcTarget.AllBuffered, newMatchID);
        }
        GetComponent<PhotonView>().RPC("Count", RpcTarget.AllBuffered);
    }

    private async System.Threading.Tasks.Task<int> CreateMatchSession()
    {
        try
        {
            var db = DatabaseManager.Instance.supabase;

            string mode = "Free For All";
            if (GetComponent<NickNameScript>().teamMode) mode = "Team Deathmatch";
            if (GetComponent<NickNameScript>().ctbMode) mode = "Capture The Byte";
            if (GetComponent<NickNameScript>().survival) mode = "Survival";

            var newMatch = new MatchSession
            {
                MapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                GameMode = mode,
                StartTime = System.DateTime.UtcNow
            };

            var response = await db.From<MatchSession>().Insert(newMatch);

            if (response.Models.Count > 0)
            {
                return response.Models[0].MatchID;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not create match session: " + e.Message);
        }
        return -1;
    }

    [PunRPC]
    void SyncMatchID(int matchID)
    {
        currentMatchID = matchID;
        Debug.Log("Supabase Match Started! Match ID: " + currentMatchID);
    }

    [PunRPC]
    void Count()
    {
        BeginCounting();
    }

    void BeginCounting()
    {
        CancelInvoke();
        InvokeRepeating("TimeCountDown", 1, 1);
    }

    // --- NEW: Team Wipe Checker ---
    void Update()
    {
        // Only let the Master Client check to save performance
        if (!PhotonNetwork.IsMasterClient || timeStop) return;

        NickNameScript nns = GetComponent<NickNameScript>();
        if (nns != null && (nns.teamMode || nns.ctbMode))
        {
            CheckForTeamWipe();
        }
    }

    void CheckForTeamWipe()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        int redTotal = 0, redDead = 0;
        int blueTotal = 0, blueDead = 0;

        foreach (GameObject p in players)
        {
            DisplayColor dc = p.GetComponent<DisplayColor>();
            PhotonView pv = p.GetComponent<PhotonView>();
            if (dc == null || pv == null) continue;

            // Figure out which team this player is on
            int mySlot = -1;
            for (int i = 0; i < dc.viewID.Length; i++)
            {
                if (dc.viewID[i] == pv.ViewID)
                {
                    mySlot = i;
                    break;
                }
            }

            if (mySlot == -1) continue;

            bool isRed = (mySlot <= 2); // Slots 0,1,2 are Red
            bool isDead = false;

            // Check if they are dead
            PlayerMovement pm = p.GetComponent<PlayerMovement>();
            if (pm != null) isDead = pm.isDead;
            else
            {
                Animator anim = p.GetComponent<Animator>();
                if (anim != null) isDead = anim.GetBool("Dead");
            }

            // Tally them up
            if (isRed)
            {
                redTotal++;
                if (isDead) redDead++;
            }
            else
            {
                blueTotal++;
                if (isDead) blueDead++;
            }
        }

        // If a team exists and is entirely wiped out, end the match!
        if (redTotal > 0 && redDead >= redTotal)
        {
            GetComponent<PhotonView>().RPC("TriggerEarlyEnd", RpcTarget.AllBuffered);
        }
        else if (blueTotal > 0 && blueDead >= blueTotal)
        {
            GetComponent<PhotonView>().RPC("TriggerEarlyEnd", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void TriggerEarlyEnd()
    {
        if (timeStop) return;

        timeStop = true;
        CancelInvoke("TimeCountDown");

        minutes = 0;
        seconds = 0;
        minutesText.text = "0";
        secondsText.text = "00";

        if (Canvas != null && Canvas.GetComponent<TeamKillCount>() != null)
        {
            Canvas.GetComponent<TeamKillCount>().countDown = false;
            Canvas.GetComponent<TeamKillCount>().TimeOver();
        }
    }

    void TimeCountDown()
    {
        if (this.gameObject.GetComponent<NickNameScript>().survival == false)
        {
            
            // 1. DO THE MATH FIRST
            if (seconds == 0 && minutes > 0)
            {
                minutes -= 1;
                seconds = 59;
            }
            else if (seconds > 0)
            {
                seconds -= 1;
            }

            // 2. UPDATE THE UI ONCE
            minutesText.text = minutes.ToString();

            if (seconds < 10)
            {
                secondsText.text = "0" + seconds.ToString();
            }
            else
            {
                secondsText.text = seconds.ToString();
            }
            if (seconds == 0 && minutes <= 0)
            {
                if (this.gameObject.GetComponent<NickNameScript>().teamMode == true || this.gameObject.GetComponent<NickNameScript>().ctbMode == true)
                {
                    Canvas.GetComponent<TeamKillCount>().countDown = false;
                    Canvas.GetComponent<TeamKillCount>().TimeOver();
                    timeStop = true;
                }
                if (this.gameObject.GetComponent<NickNameScript>().teamMode == false && this.gameObject.GetComponent<NickNameScript>().ctbMode == false)
                {
                    Canvas.GetComponent<KillCount>().countDown = false;
                    Canvas.GetComponent<KillCount>().TimeOver();
                    timeStop = true;
                }
            }
        }
        else
        {
            minutesText.text = "";
            secondsText.text = "";
        }
    }
}