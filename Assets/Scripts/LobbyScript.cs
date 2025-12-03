using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyScript : MonoBehaviourPunCallbacks
{
    TypedLobby TDM = new TypedLobby("TDM", LobbyType.Default);
    TypedLobby KOTH = new TypedLobby("KOTH", LobbyType.Default);
    TypedLobby CTB = new TypedLobby("CTB", LobbyType.Default);
    TypedLobby Survival = new TypedLobby("Survival", LobbyType.Default);

    public Text roomNumber;
    public string LevelName = "";

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void JoinLobbyKOTH()
    {
        LevelName= "Floor Layout";
        PhotonNetwork.JoinLobby(KOTH);
    }

    public void JoinLobbyTDM()
    {
        LevelName = "Floor Layout";
        PhotonNetwork.JoinLobby(TDM);
    }

    public void JoinLobbyCTB()
    {
        LevelName = "Floor Layout";
        PhotonNetwork.JoinLobby(CTB);
    }

    public void JoinLobbySurvival()
    {
        LevelName = "Floor Layout";
        PhotonNetwork.JoinLobby(Survival);
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Join Random Room Failed. Creating a new room...");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 6;
        PhotonNetwork.CreateRoom("Arena" + Random.Range(1, 1000), roomOptions);
    }

    public override void OnJoinedRoom()
    {
        roomNumber.text = PhotonNetwork.CurrentRoom.Name;
        PhotonNetwork.LoadLevel(LevelName);
    }
}
