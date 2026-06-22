using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
using Supabase;
public class TeamKillCount : MonoBehaviour
{
    public List<Kills> highestKills = new List<Kills>();
    public Text[] killAmts;
    private GameObject killCountPanel;
    private GameObject namesObject;
    private bool killCountOn = false;
    public bool countDown = true;
    public GameObject winnerPanel;
    public Text winnerText;
    private int RedTeamKills;
    private int BlueTeamKills;
    private bool hasSavedStats = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        killCountPanel = GameObject.Find("KillCountPanel");
        namesObject = GameObject.Find("NamesBG");
        killCountPanel.SetActive(false);
        winnerPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && countDown == true)
        {
            if (killCountOn == false)
            {
                killCountPanel.SetActive(true);
                killCountOn = true;
                highestKills.Clear();
                for (int i = 0; i < 6; i++)
                {
                    highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
                }
                RedTeamKills = highestKills[0].playerKills + highestKills[1].playerKills + highestKills[2].playerKills;
                BlueTeamKills = highestKills[3].playerKills + highestKills[4].playerKills + highestKills[5].playerKills;
                killAmts[0].text = RedTeamKills.ToString();
                killAmts[1].text = BlueTeamKills.ToString();
            }
            else if (killCountOn == true)
            {
                killCountPanel.SetActive(false);
                killCountOn = false;
            }
        }
        if(countDown == true)
        {
            highestKills.Clear();
            for (int i = 0; i < 6; i++)
            {
                if (namesObject.GetComponent<NickNameScript>().names[i] != null)
                {
                    highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
                }
            }
            RedTeamKills = highestKills[0].playerKills + highestKills[1].playerKills + highestKills[2].playerKills;
            BlueTeamKills = highestKills[3].playerKills + highestKills[4].playerKills + highestKills[5].playerKills;
        }
    }
    public void TimeOver()
    {
        killCountPanel.SetActive(true);
        winnerPanel.SetActive(true);
        killCountOn = true;
        highestKills.Clear();
        for (int i = 0; i < 6; i++)
        {
            highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
        }
        RedTeamKills = highestKills[0].playerKills + highestKills[1].playerKills + highestKills[2].playerKills;
        BlueTeamKills = highestKills[3].playerKills + highestKills[4].playerKills + highestKills[5].playerKills;
        killAmts[0].text = RedTeamKills.ToString();
        killAmts[1].text = BlueTeamKills.ToString();
        if (RedTeamKills > BlueTeamKills)
        {
            winnerText.text = "Red Team Wins!";
        }
        else if (BlueTeamKills > RedTeamKills)
        {
            winnerText.text = "Blue Team Wins!";
        }
        else
        {
            winnerText.text = "It's a Tie!";
        }
        CheckWinnerAndSave();
    }
    public void CTBWinner()
    {
        killCountPanel.SetActive(true);
        winnerPanel.SetActive(true);
        killCountOn = true;
        killAmts[0].text = RedTeamKills.ToString();
        killAmts[1].text = BlueTeamKills.ToString();
        if (RedTeamKills > BlueTeamKills)
        {
            winnerText.text = "Red Team Wins!";
        }
        else if (BlueTeamKills > RedTeamKills)
        {
            winnerText.text = "Blue Team Wins!";
        }
        else
        {
            winnerText.text = "It's a Tie!";
        }
        CheckWinnerAndSave();
    }
    private void CheckWinnerAndSave()
    {
        int myTeamIndex = -1;
        NickNameScript nns = namesObject.GetComponent<NickNameScript>();

        // Find out which team I am on (0,1,2 = Red. 3,4,5 = Blue)
        for (int i = 0; i < 6; i++)
        {
            if (nns.names[i].text == PhotonNetwork.LocalPlayer.NickName)
            {
                myTeamIndex = i;
                break;
            }
        }

        bool iAmRedTeam = (myTeamIndex <= 2 && myTeamIndex != -1);
        bool amITheWinner = false;

        if (RedTeamKills > BlueTeamKills)
        {
            winnerText.text = "Red Team Wins!";
            amITheWinner = iAmRedTeam;
        }
        else if (BlueTeamKills > RedTeamKills)
        {
            winnerText.text = "Blue Team Wins!";
            amITheWinner = !iAmRedTeam;
        }
        else
        {
            winnerText.text = "It's a Tie!";
            amITheWinner = false;
        }

        // --- NEW: Trigger the Database Save ---
        SaveMyStats(amITheWinner);
    }/*
    // --- NEW: The Grand Finale Database Function ---
    private async void SaveMyStats(bool isWinner)
    {
        try
        {
            var db = DatabaseManager.Instance.supabase;
            string myName = PhotonNetwork.LocalPlayer.NickName;
            int myKills = 0;
            int myDeaths = 0;

            NickNameScript nns = namesObject.GetComponent<NickNameScript>();
            for (int i = 0; i < nns.names.Length; i++)
            {
                if (nns.names[i].text == myName)
                {
                    myKills = nns.kills[i];
                    myDeaths = nns.deaths[i];
                    break;
                }
            }

            int matchID = namesObject.GetComponent<Timer>().currentMatchID;
            if (matchID == -1) return;

            var profile = await db.From<PlayerAccount>().Where(x => x.Username == myName).Single();

            if (profile != null)
            {
                var matchStats = new PlayerMatchStats
                {
                    PlayerID = profile.PlayerID,
                    MatchID = matchID,
                    Kills = myKills,
                    Deaths = myDeaths,
                    Score = (myKills * 100) + (isWinner ? 500 : 0)
                };
                await db.From<PlayerMatchStats>().Insert(matchStats);

                profile.TotalKills += myKills;
                profile.TotalDeaths += myDeaths;
                profile.PlayerLevel = 1 + (profile.TotalKills / 10);

                await profile.Update<PlayerAccount>();
                Debug.Log("Game Over! Team Stats, XP, and Level saved perfectly.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error saving final team stats: " + e.Message);
        }
    }*/
    // --- FIX 1: Made this PUBLIC ---
    public async void SaveMyStats(bool isWinner)
    {
        if (hasSavedStats == true)
        {
            return; // We already saved! Stop the code here.
        }

        hasSavedStats = true;
        try
        {
            var db = DatabaseManager.Instance.supabase;
            string myName = PhotonNetwork.LocalPlayer.NickName;
            int myKills = 0;
            int myDeaths = 0;

            NickNameScript nns = namesObject.GetComponent<NickNameScript>();
            for (int i = 0; i < nns.names.Length; i++)
            {
                if (nns.names[i].text == myName)
                {
                    myKills = nns.kills[i];
                    myDeaths = nns.deaths[i];
                    break;
                }
            }

            int matchID = namesObject.GetComponent<Timer>().currentMatchID;
            if (matchID == -1) return;

            var profile = await db.From<PlayerAccount>().Where(x => x.Username == myName).Single();

            if (profile != null)
            {
                // Base combat score
                int baseScore = (myKills * 30) - (myDeaths * 20);

            

                int matchScore = baseScore;

                if (isWinner)
                {
                    matchScore += 50; // A small flat bonus for surviving
                    matchScore = Mathf.RoundToInt(matchScore * 1.5f); // Multiply their total by 1.5x for winning!
                }
                else
                {
                    matchScore -= 25; // Small penalty for losing
                }

                // Save to match history
                var matchStats = new PlayerMatchStats
                {
                    PlayerID = profile.PlayerID,
                    MatchID = matchID,
                    Kills = myKills,
                    Deaths = myDeaths,
                    Score = matchScore // Save the exact score you got this match
                };
                await db.From<PlayerMatchStats>().Insert(matchStats);

                // Update Lifetime Stats
                profile.TotalKills += myKills;
                profile.TotalDeaths += myDeaths;

                // Apply the score change to their lifetime MMR
                profile.TotalScore += matchScore;

                // Prevent the score from dropping below 0
                if (profile.TotalScore < 0) profile.TotalScore = 0;

                // --- NEW RANKING LOGIC (Based strictly on TotalScore) ---
                if (profile.TotalScore >= 2000) profile.RankTierID = 5;
                else if (profile.TotalScore >= 1500) profile.RankTierID = 4;      // Gold
                else if (profile.TotalScore >= 1000) profile.RankTierID = 3; // Silver
                else if (profile.TotalScore >= 500) profile.RankTierID = 2;  // Bronze
                else profile.RankTierID = 1;                                 // Unranked


                await profile.Update<PlayerAccount>();
                Debug.Log("Dynamic MMR Score and Rank saved perfectly.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error saving stats: " + e.Message);
        }
    }
}
