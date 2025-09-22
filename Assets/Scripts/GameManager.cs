using UnityEngine;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to server!");
        PhotonNetwork.JoinRandomRoom();
    }   
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Floor Layout");
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
       // Debug.Log("Failed to join a room, creating a new room...");
        //PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions { MaxPlayers = 4 });
        //base.OnJoinRandomFailed(returnCode, message);
        PhotonNetwork.CreateRoom("Arena1");
    }
}
