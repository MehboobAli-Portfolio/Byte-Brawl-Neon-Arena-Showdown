using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class WeaponPickups : MonoBehaviour
{
    private AudioSource audioPlayer;
    public float respawnTime = 5f;
    public int weaponType=1;

    void Start()
    {
        audioPlayer = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            this.GetComponent<PhotonView>().RPC("PlayPickupAudio", RpcTarget.All);
            this.GetComponent<PhotonView>().RPC("TurnOff", RpcTarget.All);
        }
    }

    [PunRPC]
    void PlayPickupAudio()
    {
        if (audioPlayer != null)
        {
            audioPlayer.Play();
        }
    }

    [PunRPC]
    void TurnOff()
    {
        if (weaponType == 1)
        {
            this.transform.gameObject.GetComponent<Renderer>().enabled = false;
            this.transform.gameObject.GetComponent<Collider>().enabled = false;
        }
        else
        {
            this.transform.GetChild(0).gameObject.SetActive(false);
            this.transform.gameObject.GetComponent<Collider>().enabled = false;
        }
        StartCoroutine(WaitToRespawn());
    }

    [PunRPC]
    void TurnOn()
    {
        if (weaponType == 1)
        {
            this.transform.gameObject.GetComponent<Renderer>().enabled = true;
            this.transform.gameObject.GetComponent<Collider>().enabled = true;
        }
        else
        {
            this.transform.GetChild(0).gameObject.SetActive(true);
            this.transform.gameObject.GetComponent<Collider>().enabled = true;
        }
    }

    IEnumerator WaitToRespawn()
    {
        yield return new WaitForSeconds(respawnTime);
        this.GetComponent<PhotonView>().RPC("TurnOn", RpcTarget.All);
    }
}