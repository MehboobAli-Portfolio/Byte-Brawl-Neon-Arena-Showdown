using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;

public class DisplayColor : MonoBehaviourPunCallbacks
{
    public int[] buttonNumbers;
    public int[] viewID;
    public Color32[] colors;
    public Color32[] teamColors;
    public Color32[] ctbColor;
    private bool teamMode = false;
    private bool ctbMode = false;
    private GameObject namesObject;
    private GameObject waitForPlayers;

    public AudioClip[] gunShotSounds;
    
    public bool isRespawn = false;

    private void Start()
    {
        namesObject = GameObject.Find("NamesBG");
        waitForPlayers = GameObject.Find("WaitingBG");
        InvokeRepeating("CheckTime", 1, 1);
        teamMode = namesObject.GetComponent<NickNameScript>().teamMode;
        ctbMode = namesObject.GetComponent<NickNameScript>().ctbMode;
        isRespawn = namesObject.GetComponent<NickNameScript>().survival;
        GetComponent<PlayerMovement>().noRespawn = isRespawn;
        if (GetComponent<PlayerMovement>().noRespawn == false)
        {
            GetComponent<PlayerMovement>().noRespawn = ctbMode;
        }
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
           if(GetComponent<PhotonView>().IsMine == true && waitForPlayers.activeInHierarchy==false)
           {
                RemoveData();
                RoomExit();
           }
        }
        if(this.GetComponent<Animator>().GetBool("Hit") == true)
        {
            StartCoroutine(Recover());
        }
    }

    public void NoRespawnExit()
    {
        namesObject.GetComponent<NickNameScript>().eliminationPanel.SetActive(true);
        // --- NEW: Save the loser's stats BEFORE they disconnect! ---
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            if (ctbMode == true)
            {
                canvas.GetComponent<TeamKillCount>().SaveMyStats(false);
            }
            else
            {
                canvas.GetComponent<KillCount>().SaveMyStats(false);
            }
        }
        StartCoroutine(WaitToExit());
    }
    
    void CheckTime()
    {
        if(namesObject.GetComponent<Timer>().timeStop == true)
        {
            this.gameObject.GetComponent<PlayerMovement>().isDead = true;
            this.gameObject.GetComponent<PlayerMovement>().gameOver = true;
            this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = true;
            this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = true;
            this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
    }
    public void Respawn(string name)
    {
        GetComponent<PhotonView>().RPC("ResetForReplay",RpcTarget.AllBuffered,name);
    }

    [PunRPC]
    void ResetForReplay(string name)
    {
        for (int i = 0; i < namesObject.GetComponent<NickNameScript>().names.Length; i++)
        {
            if (name == namesObject.GetComponent<NickNameScript>().names[i].text)
            {
                this.GetComponent<Animator>().SetBool("Dead", false);
                this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = false;
                this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = false;
                this.gameObject.layer = LayerMask.NameToLayer("Default");
                namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.GetComponent<Image>().fillAmount = 1;

            }
        }
    }

    public void DeliverDamage(string shooterName,string name,float damageAmount)
    {
        GetComponent<PhotonView>().RPC("GunDamage",RpcTarget.AllBuffered,shooterName,name,damageAmount);
    }

    [PunRPC]
    void GunDamage(string shooterName,string name,float damageAmount)
    {
        if (teamMode == true || ctbMode == true)
        {
            int shooterIndex = -1;
            int targetIndex = -1;

            // Find the UI index for both the shooter and the target
            for (int j = 0; j < namesObject.GetComponent<NickNameScript>().names.Length; j++)
            {
                if (namesObject.GetComponent<NickNameScript>().names[j].text == shooterName)
                {
                    shooterIndex = j;
                }
                if (namesObject.GetComponent<NickNameScript>().names[j].text == name)
                {
                    targetIndex = j;
                }
            }

            // If both players were found, check their teams
            if (shooterIndex != -1 && targetIndex != -1)
            {
                // Indices 0, 1, 2 are Red Team. Indices 3, 4, 5 are Blue Team.
                bool shooterIsRedTeam = (shooterIndex <= 2);
                bool targetIsRedTeam = (targetIndex <= 2);
                bool shooterIsBlueTeam = (shooterIndex > 2);
                bool targetIsBlueTeam = (targetIndex > 2);
                // If they are on the same team, exit without applying damage
                if ((shooterIsRedTeam && targetIsRedTeam) || (shooterIsBlueTeam && targetIsBlueTeam))
                {
                    return;
                }
            }
        }
        for (int i = 0; i < namesObject.GetComponent<NickNameScript>().names.Length; i++)
        {
            if (name == namesObject.GetComponent<NickNameScript>().names[i].text)
            {
                if (namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.GetComponent<Image>().fillAmount > 0.1f)
                {
                    this.GetComponent<Animator>().SetBool("Hit", true);
                    namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.GetComponent<Image>().fillAmount -= damageAmount;
                }
                else
                {
                    namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.GetComponent<Image>().fillAmount = 0;
                    this.GetComponent<Animator>().SetBool("Dead",true);
                    this.gameObject.GetComponent<PlayerMovement>().isDead = true;
                    this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = true;
                    this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = true;
                    namesObject.GetComponent<NickNameScript>().RunMessage(shooterName, name);
                    this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                }
            }
        }
    }

    void RemoveData()
    {
        GetComponent<PhotonView>().RPC("RemoveMe",RpcTarget.AllBuffered);

    }
    void RoomExit()
    {
        StartCoroutine(GetReadyToLeave());
    }
    public void ChooseColor()
    {
        GetComponent<PhotonView>().RPC("AssignColor",RpcTarget.AllBuffered);
    }
    public void PlayGunshot(string name,int weaponNumber)
    {
        GetComponent<PhotonView>().RPC("PlayGunSound",RpcTarget.All,name,weaponNumber);
    }
    [PunRPC]
    void PlayGunSound(string name,int weaponNumber)
    {
        for (int i = 0; i < namesObject.GetComponent<NickNameScript>().names.Length; i++)
        {
            if (name == this.GetComponent<PhotonView>().Owner.NickName)
            {
                GetComponent<AudioSource>().clip = gunShotSounds[weaponNumber];
                GetComponent<AudioSource>().Play();
            }
        }
        
    }

    [PunRPC]
    void AssignColor()
    {
        for (int i = 0; i < viewID.Length; i++)
        {
            if(teamMode == true)
            {
                if (this.GetComponent<PhotonView>().ViewID == viewID[i])
                {
                    this.transform.GetChild(1).GetComponent<Renderer>().material.color = teamColors[i];
                    namesObject.GetComponent<NickNameScript>().names[i].gameObject.SetActive(true);
                    namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.SetActive(true);
                    namesObject.GetComponent<NickNameScript>().names[i].text = this.GetComponent<PhotonView>().Owner.NickName;
                }
            }
            else if(ctbMode == true)
            {
                if (this.GetComponent<PhotonView>().ViewID == viewID[i])
                {
                    this.transform.GetChild(1).GetComponent<Renderer>().material.color = ctbColor[i];
                    namesObject.GetComponent<NickNameScript>().names[i].gameObject.SetActive(true);
                    namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.SetActive(true);
                    namesObject.GetComponent<NickNameScript>().names[i].text = this.GetComponent<PhotonView>().Owner.NickName;
                }
            }
            else if (teamMode == false && ctbMode == false)
            {
                if (this.GetComponent<PhotonView>().ViewID == viewID[i])
                {
                    this.transform.GetChild(1).GetComponent<Renderer>().material.color = colors[i];
                    namesObject.GetComponent<NickNameScript>().names[i].gameObject.SetActive(true);
                    namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.SetActive(true);
                    namesObject.GetComponent<NickNameScript>().names[i].text = this.GetComponent<PhotonView>().Owner.NickName;
                }
            }
        }
    }
    [PunRPC]
    void RemoveMe()
    {
        for (int i = 0; i < namesObject.gameObject.GetComponent<NickNameScript>().names.Length; i++)
        {
            if (this.GetComponent<PhotonView>().Owner.NickName==namesObject.GetComponent<NickNameScript>().names[i].text)
            {
                namesObject.GetComponent<NickNameScript>().names[i].gameObject.SetActive(false);
                namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.SetActive(false);
            }
        }
    }
    IEnumerator GetReadyToLeave()
    {
        yield return new WaitForSeconds(1.0f);
        namesObject.GetComponent<NickNameScript>().Leaving();
        Cursor.visible = true;
        PhotonNetwork.LeaveRoom();

    }
    IEnumerator Recover()
    {
        yield return new WaitForSeconds(0.03f);
        this.GetComponent<Animator>().SetBool("Hit", false);
    }
    IEnumerator WaitToExit()
    {
        yield return new WaitForSeconds(3);
        RemoveData();
        RoomExit();
    }
}
