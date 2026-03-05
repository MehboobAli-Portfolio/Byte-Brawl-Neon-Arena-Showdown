using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
using Supabase;
public class KillCount : MonoBehaviour
{
    public List<Kills> highestKills = new List<Kills>();
    public Text[] names;
    public Text[] killAmts; 
    private GameObject killCountPanel;
    private GameObject namesObject;
    private bool killCountOn = false;
    public bool countDown = true;
    public GameObject winnerPanel;
    public Text winnerText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        killCountPanel= GameObject.Find("KillCountPanel");
        namesObject = GameObject.Find("NamesBG");
        killCountPanel.SetActive(false);
        winnerPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K) && countDown == true)
        {
            if (killCountOn == false)
            {
                killCountPanel.SetActive(true);
                killCountOn= true;
                highestKills.Clear();
                for (int i = 0; i < names.Length; i++)
                {
                    highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
                }
                highestKills.Sort();
                for(int i = 0; i < names.Length; i++)
                {
                    names[i].text=highestKills[i].playerName;
                    killAmts[i].text = highestKills[i].playerKills.ToString();
                }
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].text == "Name ")
                    {
                        names[i].text = "";
                        killAmts[i].text = "";
                    }
                }
            }
            else if (killCountOn == true)
            {
                killCountPanel.SetActive(false);
                killCountOn = false;
            }
        }
    }
    public void TimeOver()
    {
        killCountPanel.SetActive(true);
        winnerPanel.SetActive(true);
        killCountOn = true;
        highestKills.Clear();
        for (int i = 0; i < names.Length; i++)
        {
            highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
        }
        highestKills.Sort();
        winnerText.text =highestKills[0].playerName;
        for (int i = 0; i < names.Length; i++)
        {
            names[i].text = highestKills[i].playerName;
            killAmts[i].text = highestKills[i].playerKills.ToString();
        }
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].text == "Name ")
            {
                names[i].text = "";
                killAmts[i].text = "";
            }
        }
        // --- NEW: Trigger the Database Save ---
        bool amITheWinner = (highestKills[0].playerName == PhotonNetwork.LocalPlayer.NickName);
        SaveMyStats(amITheWinner);
    }
    public void SurvivalWinner(string name)
    {
        winnerPanel.SetActive(true);
        winnerText.text=name;
        // --- NEW: Trigger the Database Save ---
        bool amITheWinner = (name == PhotonNetwork.LocalPlayer.NickName);
        SaveMyStats(amITheWinner);
    }
    /*
    // --- NEW: The Grand Finale Database Function ---
    private async void SaveMyStats(bool isWinner)
    {
        try
        {
            var db = DatabaseManager.Instance.supabase;
            string myName = PhotonNetwork.LocalPlayer.NickName;
            int myKills = 0;
            int myDeaths = 0;

            // 1. Find my specific kills and deaths from the NickNameScript
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

            // 2. Grab the Match ID from the Timer script
            int matchID = namesObject.GetComponent<Timer>().currentMatchID;
            if (matchID == -1) return; // Failsafe if match wasn't tracked

            // 3. Download my lifetime profile
            var profile = await db.From<PlayerAccount>()
                                  .Where(x => x.Username == myName)
                                  .Single();

            if (profile != null)
            {
                // 4. Save this match's combat stats to Player_Match_Stats
                var matchStats = new PlayerMatchStats
                {
                    PlayerID = profile.PlayerID,
                    MatchID = matchID,
                    Kills = myKills,
                    Deaths = myDeaths,
                    Score = (myKills * 100) + (isWinner ? 500 : 0) // XP Calculation!
                };
                await db.From<PlayerMatchStats>().Insert(matchStats);

                // 5. Update Lifetime Stats & Level Up!
                profile.TotalKills += myKills;
                profile.TotalDeaths += myDeaths;

                // Level Up Logic: Start at Level 1, +1 level every 10 kills
                profile.PlayerLevel = 1 + (profile.TotalKills / 10);

                // 6. Push the updated profile back to Supabase
                await profile.Update<PlayerAccount>();
                Debug.Log("Game Over! Stats, XP, and Level saved perfectly.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error saving final stats: " + e.Message);
        }
    }*/
    // --- FIX 1: Made this PUBLIC so dying players can trigger it ---

    public async void SaveMyStats(bool isWinner)
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
                // --- NEW MMR MATH LOGIC ---
                // +100 for every kill. -50 for every death.
                int matchScore = (myKills * 100) - (myDeaths * 50);

                // +500 if you win. -250 if you lose.
                if (isWinner) matchScore += 500;
                else matchScore -= 250;

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
                if (profile.TotalScore >= 1500) profile.RankTierID = 4;      // Gold
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
