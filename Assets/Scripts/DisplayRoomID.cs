using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DisplayRoomID : MonoBehaviour
{
    public Text idText; // Assign the UI Text here

    void Start()
    {
        // Check if we are connected to a room
        if (PhotonNetwork.CurrentRoom != null)
        {
            idText.text = "Room ID: " + PhotonNetwork.CurrentRoom.Name;
        }
        else
        {
            idText.text = "Room ID: Offline";
        }
    }
}