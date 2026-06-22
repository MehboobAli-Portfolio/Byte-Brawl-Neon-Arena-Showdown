using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnCharacters : MonoBehaviour
{
    public GameObject character;
    public Transform[] spawnPoints;
    public GameObject[] weapons;
    public Transform[] weaponSpawnPoints;
    public float weaponRespawnTime = 10;
    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            // 1. Every human player spawns their own character based on ActorNumber
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Vector3 finalPos = spawnPoints[spawnIndex].position;
            PhotonNetwork.Instantiate(character.name, finalPos, spawnPoints[spawnIndex].rotation);

            // 2. CRITICAL FIX: Only let the Master Client spawn weapons once
            // Removing 'GameObject.Find' avoids race conditions during fast connections
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnWeaponsStart();
            }
        }
    }
    public void SpawnWeaponsStart()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null && weaponSpawnPoints.Length > i)
            {
                PhotonNetwork.Instantiate(weapons[i].name, weaponSpawnPoints[i].position, weaponSpawnPoints[i].rotation);
            }
        }
    }
}