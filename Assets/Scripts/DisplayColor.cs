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
    private bool isDisconnecting = false;

    private void Start()
    {
        namesObject = GameObject.Find("NamesBG");
        waitForPlayers = GameObject.Find("WaitingBG");
        InvokeRepeating("CheckTime", 1, 1);

        if (namesObject != null)
        {
            teamMode = namesObject.GetComponent<NickNameScript>().teamMode;
            ctbMode = namesObject.GetComponent<NickNameScript>().ctbMode;
            isRespawn = namesObject.GetComponent<NickNameScript>().survival;
        }

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.noRespawn = isRespawn;
            if (pm.noRespawn == false)
            {
                pm.noRespawn = ctbMode;
            }
        }
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && !isDisconnecting)
        {
            if (GetComponent<PhotonView>().IsMine == true && waitForPlayers.activeInHierarchy == false)
            {
                isDisconnecting = true; // Lock the door so it can't run twice!
                RemoveData();
                RoomExit();
            }
        }
        if (this.GetComponent<Animator>().GetBool("Hit") == true)
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
        if (namesObject.GetComponent<Timer>().timeStop == true)
        {
            // SAFE CHECKS: Only shut down human scripts if they actually exist!
            if (this.gameObject.GetComponent<PlayerMovement>() != null)
            {
                this.gameObject.GetComponent<PlayerMovement>().isDead = true;
                this.gameObject.GetComponent<PlayerMovement>().gameOver = true;
            }
            if (this.gameObject.GetComponent<WeaponChangeAdvanced>() != null)
            {
                this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = true;
            }
            if (this.gameObject.GetComponentInChildren<AimLookAtRef>() != null)
            {
                this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = true;
            }

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
                this.gameObject.layer = LayerMask.NameToLayer("Default");
                namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.GetComponent<Image>().fillAmount = 1;

                // --- SAFE CHECKS: Only reset human scripts if they exist! ---
                if (this.gameObject.GetComponent<WeaponChangeAdvanced>() != null)
                {
                    this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = false;
                }

                if (this.gameObject.GetComponentInChildren<AimLookAtRef>() != null)
                {
                    this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = false;
                }
            }
        }
    }

    public void DeliverDamage(string shooterName,string name,float damageAmount)
    {
        string myRealName = "";

        // Check if I am a bot
        if (this.gameObject.GetComponent<AIBotController>() != null)
        {
            // Force my true bot name!
            myRealName = "Bot " + this.GetComponent<PhotonView>().ViewID;
        }
        else
        {
            // I am a human, use my real network name
            myRealName = this.GetComponent<PhotonView>().Owner.NickName;
        }
        GetComponent<PhotonView>().RPC("GunDamage",RpcTarget.AllBuffered,shooterName, myRealName, damageAmount);
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
        /*for (int i = 0; i < namesObject.GetComponent<NickNameScript>().names.Length; i++)
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
                    if (this.gameObject.GetComponent<PlayerMovement>() != null)
                    {
                        this.gameObject.GetComponent<PlayerMovement>().isDead = true;
                    }
                    if (this.gameObject.GetComponent<WeaponChangeAdvanced>() != null)
                    {
                        this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = true;
                    }
                    if (this.gameObject.GetComponentInChildren<AimLookAtRef>() != null)
                    {
                        this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = true;
                    }
                    namesObject.GetComponent<NickNameScript>().RunMessage(shooterName, name);
                    this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                }
            }
        }*/
        for (int i = 0; i < namesObject.GetComponent<NickNameScript>().names.Length; i++)
        {
            if (name == namesObject.GetComponent<NickNameScript>().names[i].text)
            {
                // Grab the health bar image so we don't have to type it out over and over
                Image healthBar = namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.GetComponent<Image>();

                // Calculate what their health WILL be after this shot hits
                float resultingHealth = healthBar.fillAmount - damageAmount;

                if (resultingHealth > 0f)
                {
                    // They survive the shot! Apply the damage and play the flinch animation
                    this.GetComponent<Animator>().SetBool("Hit", true);
                    healthBar.fillAmount = resultingHealth;
                }
                else
                {
                    // The shot is fatal! Force the health bar to 0 (completely empty) and kill them
                    healthBar.fillAmount = 0f;

                    this.GetComponent<Animator>().SetBool("Dead", true);

                    if (this.gameObject.GetComponent<PlayerMovement>() != null)
                    {
                        this.gameObject.GetComponent<PlayerMovement>().isDead = true;
                    }
                    if (this.gameObject.GetComponent<WeaponChangeAdvanced>() != null)
                    {
                        this.gameObject.GetComponent<WeaponChangeAdvanced>().isDead = true;
                    }
                    if (this.gameObject.GetComponentInChildren<AimLookAtRef>() != null)
                    {
                        this.gameObject.GetComponentInChildren<AimLookAtRef>().isDead = true;
                    }

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
        // --- Bulletproof Name Check ---
        string myName = "";
        if (this.gameObject.GetComponent<AIBotController>() != null)
        {
            myName = "Bot " + this.GetComponent<PhotonView>().ViewID;
        }
        else
        {
            myName = this.GetComponent<PhotonView>().Owner.NickName;
        }

        for (int i = 0; i < viewID.Length; i++)
        {
            // --- THE FIX: We put EVERYTHING inside this one single IF statement! ---
            // Now, it will only do these things if the ID matches the exact slot.
            if (this.GetComponent<PhotonView>().ViewID == viewID[i])
            {
                // 1. Hide ONLY the specific button that was picked
                NickNameScript nns = namesObject.GetComponent<NickNameScript>();
                if (nns.colorButtons != null && i < nns.colorButtons.Length)
                {
                    if (nns.colorButtons[i] != null)
                    {
                        nns.colorButtons[i].SetActive(false);
                    }
                }

                // 2. Assign the actual color
                if (teamMode == true)
                {
                    this.transform.GetChild(1).GetComponent<Renderer>().material.color = teamColors[i];
                }
                else if (ctbMode == true)
                {
                    this.transform.GetChild(1).GetComponent<Renderer>().material.color = ctbColor[i];
                }
                else if (teamMode == false && ctbMode == false)
                {
                    this.transform.GetChild(1).GetComponent<Renderer>().material.color = colors[i];
                }

                // 3. Turn on the UI bars and Text
                namesObject.GetComponent<NickNameScript>().names[i].gameObject.SetActive(true);
                namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.SetActive(true);
                namesObject.GetComponent<NickNameScript>().names[i].text = myName;
            }
        }
    }

    [PunRPC]
    void RemoveMe()
    {
        // --- NEW: Bulletproof Name Check for when they die/leave ---
        string myName = "";
        if (this.gameObject.GetComponent<AIBotController>() != null)
        {
            myName = "Bot " + this.GetComponent<PhotonView>().ViewID;
        }
        else
        {
            myName = this.GetComponent<PhotonView>().Owner.NickName;
        }

        for (int i = 0; i < namesObject.gameObject.GetComponent<NickNameScript>().names.Length; i++)
        {
            if (myName == namesObject.GetComponent<NickNameScript>().names[i].text)
            {
                namesObject.GetComponent<NickNameScript>().names[i].gameObject.SetActive(false);
                namesObject.GetComponent<NickNameScript>().healthbars[i].gameObject.SetActive(false);
            }
        }
    }
    IEnumerator GetReadyToLeave()
    {
        yield return new WaitForSeconds(1.0f);

        if (namesObject != null)
        {
            namesObject.GetComponent<NickNameScript>().Leaving();
        }

        Cursor.visible = true;

        // --- NEW FIX: Ask Photon if we are actually in a room before trying to leave! ---
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
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
