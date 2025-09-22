using UnityEngine;
using Photon.Pun;
public class SpawnCharacters : MonoBehaviour
{
    public GameObject character;
    public Transform[] spawnPoints; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate(character.name, spawnPoints[PhotonNetwork.CurrentRoom.PlayerCount - 1].position, spawnPoints[PhotonNetwork.CurrentRoom.PlayerCount - 1].rotation);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
