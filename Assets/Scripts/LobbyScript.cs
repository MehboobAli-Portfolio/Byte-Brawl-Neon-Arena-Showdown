using System.Collections;
using System.Collections.Generic;
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
    private string levelName = "";

	public void BackToMenu()
    {
		PhotonNetwork.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    public void JoinLobbyKOTH()
    {
        levelName = "Floor";
        PhotonNetwork.JoinLobby(KOTH);
    }

    public void JoinLobbyTDM()
    {
        levelName = "Floor";
        PhotonNetwork.JoinLobby(TDM);
    }

    public void JoinLobbyCTB()
    {
        levelName = "Floor";
        PhotonNetwork.JoinLobby(CTB);
    }

    public void JoinLobbySurvival()
    {
        levelName = "Floor";
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
        PhotonNetwork.LoadLevel(levelName);
	}
}
