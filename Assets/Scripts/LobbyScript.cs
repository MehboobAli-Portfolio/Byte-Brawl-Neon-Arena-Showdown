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

    public GameObject roomNumber;
    public Text roomNumber1;
    private string levelName = "";
    public InputField roomJoinInput;
    private void Start()
    {
        roomNumber.SetActive(false);
    }
     

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
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join Room Failed: " + message);

        if (roomNumber1 != null)
        {
            // Check if the failure was because the room is full
            if (returnCode == ErrorCode.GameFull)
            {
                roomNumber1.text = "Room is Full!";
            }
            // Check if the failure was because the room ID doesn't exist
            else if (returnCode == ErrorCode.GameDoesNotExist)
            {
                roomNumber1.text = "Room ID invalid!";
            }
            else
            {
                // Show any other error (like internet issues)
                roomNumber1.text = "Error: " + message;
            }
        }
    }
    public void JoinRoomByInput()
    {
        // Check if the input field is valid
        if (roomJoinInput != null && !string.IsNullOrEmpty(roomJoinInput.text))
        {
            // SAFE CHECK: Only change text if roomNumber1 is assigned
            if (roomNumber1 != null)
            {
                roomNumber1.text = "Connecting to custom Room";
            }

            levelName = "Floor";
            PhotonNetwork.JoinRoom(roomJoinInput.text);
        }
        else
        {
            // SAFE CHECK: Only change text if roomNumber1 is assigned
            if (roomNumber1 != null)
            {
                roomNumber1.text = "Please Enter Correct ID";
            }
            Debug.Log("Input field is empty or missing.");
        }
    }

    public override void OnJoinedRoom()
	{
        roomNumber.SetActive(true);
        PhotonNetwork.LoadLevel(levelName);
	}
}
