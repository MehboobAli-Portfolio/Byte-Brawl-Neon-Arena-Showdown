using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject nameGraphic;
    public InputField usernameInput; // Used ONLY for Register
    public InputField emailInput;    // Used for Login & Register
    public InputField passwordInput; // Used for Login & Register
    public Text messageText;

    private void Start()
    {
        messageText.text = "";
        usernameInput.gameObject.SetActive(false);
        if (nameGraphic != null) nameGraphic.SetActive(false);
    }

    // --- REGISTRATION (Needs Username, Email, and Password) ---
    public async void OnRegisterClicked()
    {
        if (usernameInput.gameObject.activeSelf == false)
        {
            // Show BOTH the field and the graphic, then wait for the user to type
            usernameInput.gameObject.SetActive(true);
            if (nameGraphic != null) nameGraphic.SetActive(true);

            messageText.color = Color.cyan;
            messageText.text = "Please enter a Username and click Register again!";
            return;
        }
        messageText.color = Color.yellow;
        messageText.text = "Registering...";

        try
        {
            var db = DatabaseManager.Instance.supabase;

            // 1. Create the secure login
            var session = await db.Auth.SignUp(emailInput.text, passwordInput.text);

            if (session != null)
            {
                // 2. Save the Username and Email to your public Player_Account table
                var newProfile = new PlayerAccount
                {
                    Username = usernameInput.text,
                    Email = emailInput.text,
                    RankTierID = 1,     // <-- FIX: Explicitly set to 1 (Unranked)
                    PlayerLevel = 1
                };
                await db.From<PlayerAccount>().Insert(newProfile);

                messageText.color = Color.green;
                messageText.text = "Account Created! Please click Login.";
            }
        }
        catch (System.Exception e)
        {
            messageText.color = Color.red;
            messageText.text = "Register Error: " + e.Message;
        }
    }

    // --- LOGIN (Needs Email and Password only) ---
    public async void OnLoginClicked()
    {
        usernameInput.gameObject.SetActive(false);
        if (nameGraphic != null) nameGraphic.SetActive(false); 

        messageText.color = Color.yellow;
        messageText.text = "Logging in...";

        try
        {
            var db = DatabaseManager.Instance.supabase;

            // 1. Verify Email and Password
            var session = await db.Auth.SignIn(emailInput.text, passwordInput.text);

            if (session != null)
            {
                messageText.text = "Fetching profile...";

                // 2. Look up the Username in the database using the Email
                var profile = await db.From<PlayerAccount>()
                                      .Where(x => x.Email == emailInput.text)
                                      .Single();

                // If a profile is found, grab the username. If not, default to "Player".
                string myUsername = profile != null ? profile.Username : "Player";

                messageText.color = Color.green;
                messageText.text = "Welcome, " + myUsername + "!";

                // 3. Connect to Photon
                ConnectToPhoton(myUsername);
            }
        }
        catch (System.Exception)
        {
            messageText.color = Color.red;
            messageText.text = "Login Failed: Wrong Email or Password.";
        }
    }

    private void ConnectToPhoton(string playerName)
    {
        PhotonNetwork.LocalPlayer.NickName = playerName;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        SceneManager.LoadScene("Lobby");
    }
    public void OnExitClicked()
    {
        Application.Quit();
    }
}
