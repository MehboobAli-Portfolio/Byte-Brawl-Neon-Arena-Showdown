using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Supabase;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Profile UI")]
    public Text usernameText;
    public Text levelText;

    [Header("Stats UI")]
    public Text killsText;
    public Text deathsText;
    public Text kdText;
    public Text pingText;

    private async void Start()
    {
        usernameText.text = PhotonNetwork.LocalPlayer.NickName;

        // 2. Ask Supabase for the rest of the stats
        await LoadPlayerStats();
    }

    private void Update()
    {
        // 3. Constantly update the live Ping from the Photon Server
        pingText.text = "Ping: " + PhotonNetwork.GetPing() + "ms";
    }

    private async System.Threading.Tasks.Task LoadPlayerStats()
    {
        try
        {
            var db = DatabaseManager.Instance.supabase;

            // Search the database for the row matching this player's username
            var profile = await db.From<PlayerAccount>()
                                  .Where(x => x.Username == PhotonNetwork.LocalPlayer.NickName)
                                  .Single();

            if (profile != null)
            {
                // Update the UI text with the database numbers
                killsText.text = "Kills: " + profile.TotalKills;
                deathsText.text = "Deaths: " + profile.TotalDeaths;
                // --- ALIGNED LOBBY UI LOGIC ---
                string rankName = "Unranked"; // Default to ID 1

                if (profile.RankTierID == 2) rankName = "Bronze";
                else if (profile.RankTierID == 3) rankName = "Silver";
                else if (profile.RankTierID == 4) rankName = "Gold";
                // Display the actual word in the UI
                levelText.text = "Rank: " + rankName;

                // Calculate the K/D Ratio safely
                float kd = 0;
                if (profile.TotalDeaths > 0)
                {
                    kd = (float)profile.TotalKills / profile.TotalDeaths;
                }
                else
                {
                    kd = profile.TotalKills; // If they have 0 deaths, their K/D is just their total kills
                }

                // "F2" formats the decimal so it looks clean (e.g., 1.50)
                kdText.text = "K/D Ratio: " + kd.ToString("F2");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not load stats: " + e.Message);
        }
    }
}