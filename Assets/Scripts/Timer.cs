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
        // NEW: Only the Host creates the Match Session in the database to prevent duplicates
        if (PhotonNetwork.IsMasterClient)
        {
            int newMatchID = await CreateMatchSession();
            GetComponent<PhotonView>().RPC("SyncMatchID", RpcTarget.AllBuffered, newMatchID);
        }
        GetComponent<PhotonView>().RPC("Count", RpcTarget.AllBuffered);
    }
    // --- NEW: Database Function to create the match ---
    private async System.Threading.Tasks.Task<int> CreateMatchSession()
    {
        try
        {
            var db = DatabaseManager.Instance.supabase;

            // Determine the game mode string to save to the database
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

            // Insert into Supabase and grab the automatically returned data
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
    // --- NEW: RPC to share the Match ID with the other players ---
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
    void TimeCountDown()
    {
        if (this.gameObject.GetComponent<NickNameScript>().survival == false)
        {

            if (seconds > 10)
            {
                seconds -= 1;
                secondsText.text = seconds.ToString();
            }
            else if (seconds > 0 && seconds < 11)
            {
                seconds -= 1;
                secondsText.text = "0" + seconds.ToString();
            }
            else if (seconds == 0 && minutes > 0)
            {
                secondsText.text = "0" + seconds.ToString();
                minutes -= 1;
                seconds = 59;
                minutesText.text = minutes.ToString();
                secondsText.text = seconds.ToString();
            }
            if (seconds == 0 && minutes <= 0)
            {
                if (this.gameObject.GetComponent<NickNameScript>().teamMode == true)
                {
                    Canvas.GetComponent<TeamKillCount>().countDown = false;
                    Canvas.GetComponent<TeamKillCount>().TimeOver();
                    timeStop = true;
                }
                if (this.gameObject.GetComponent<NickNameScript>().ctbMode == true)
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
