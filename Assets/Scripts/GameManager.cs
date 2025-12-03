using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviourPunCallbacks
{
    public InputField playerNickName;
    private string setName="";
    public GameObject connection;
    // Start is called before the first frame update
    void Start()
    {
        connection.SetActive(false);
    }

    // Update is called once per frame
    public void UpdateText()
    {
        setName = playerNickName.text;
        PhotonNetwork.LocalPlayer.NickName = setName;
    }

    public void EnterButton()
    {
        if(setName!="")
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
            connection.SetActive(true);
        }
    }
    public void ExitButton()
    {
        Application.Quit();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to server!");
        SceneManager.LoadScene("Lobby");
        //PhotonNetwork.JoinRandomRoom();
    }   
    /*public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Floor Layout");
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
       // Debug.Log("Failed to join a room, creating a new room...");
        //PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions { MaxPlayers = 4 });
        //base.OnJoinRandomFailed(returnCode, message);
        PhotonNetwork.CreateRoom("Arena1");
    }*/
}
