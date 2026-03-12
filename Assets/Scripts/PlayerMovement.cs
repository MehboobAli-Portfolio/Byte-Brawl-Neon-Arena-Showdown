using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotateSpeed = 100.0f;
    private Rigidbody rb;
    private Animator anim;
    private bool canJump = true;
    public bool isDead = false;
    private Vector3 startPos;
    private bool respawned = false;
    private GameObject respawnPanel;
    public bool gameOver = false;
    public bool noRespawn;
    public bool startChecking = false;
    private GameObject Canvas;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>(); 
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;
        startPos = transform.position;
        respawnPanel = GameObject.Find("RespawnPanel");
        Canvas = GameObject.Find("Canvas");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isDead == false)
        {
            respawnPanel.SetActive(false);
            Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

            Vector3 rotateY = new Vector3(0, Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime, 0);
            if (movement != Vector3.zero)
            {
                rb.MoveRotation(rb.rotation * Quaternion.Euler(rotateY));
            }
            rb.MovePosition(rb.position + transform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime + transform.right * Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime);

            anim.SetFloat("BlendV", Input.GetAxis("Vertical"));
            anim.SetFloat("BlendH", Input.GetAxis("Horizontal"));
        }
	}

	private void Update()
	{
        if (isDead == false)
        {
            if (Input.GetButtonDown("Jump") && canJump == true)
            {
                canJump = false;
                rb.AddForce(Vector3.up * 130 * Time.deltaTime, ForceMode.VelocityChange);
                StartCoroutine(JumpAgain());
            }
        }
        if(isDead == true && respawned == false && gameOver == false && noRespawn == false)
        {
            respawned = true;
            respawnPanel.SetActive(true);
            respawnPanel.GetComponent<RespawnTimer>().enabled = true;
            StartCoroutine(RespawnWait());
        }
        if (isDead == true && respawned == false && gameOver == false && noRespawn == true)
        {
            respawned = true;
            GetComponent<DisplayColor>().NoRespawnExit();
        }
        if(PhotonNetwork.CurrentRoom.PlayerCount>=1 && startChecking == false)
        {
            startChecking = true;
            InvokeRepeating("CheckforWinner", 10, 3);
        }
    }
    void CheckforWinner()
    {
        if (GameObject.Find("WaitingBG") != null || GameObject.Find("ChoosePanel") != null)
        {
            return;
        }

        GameObject namesBG = GameObject.Find("NamesBG");
        if (namesBG != null)
        {
            NickNameScript nns = namesBG.GetComponent<NickNameScript>();

            if ((nns.survival == true || nns.ctbMode == true) && noRespawn == true)
            {
                GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
                if (allPlayers.Length <= 1) return;

                int aliveCount = 0;
                string lastAliveName = "";

                foreach (GameObject p in allPlayers)
                {
                    // 1. Wait for everyone to get their colors
                    DisplayColor dc = p.GetComponent<DisplayColor>();
                    if (dc != null)
                    {
                        bool hasAssignedColor = false;
                        PhotonView pView = p.GetComponent<PhotonView>();
                        if (pView != null)
                        {
                            for (int i = 0; i < dc.viewID.Length; i++)
                            {
                                if (dc.viewID[i] == pView.ViewID) hasAssignedColor = true;
                            }
                        }
                        if (!hasAssignedColor) return;
                    }

                    // --- NEW FIX: Bulletproof Alive Check! ---
                    bool pIsDead = false;

                    if (p.GetComponent<AIBotController>() != null)
                    {
                        // IT IS A BOT! Only trust its Animator to see if it is dead.
                        Animator pAnim = p.GetComponent<Animator>();
                        if (pAnim != null) pIsDead = pAnim.GetBool("Dead");
                    }
                    else
                    {
                        // IT IS A HUMAN! Trust the PlayerMovement script.
                        PlayerMovement pm = p.GetComponent<PlayerMovement>();
                        if (pm != null) pIsDead = pm.isDead;
                    }

                    // 3. Count who is actually alive
                    if (!pIsDead)
                    {
                        aliveCount++;

                        PhotonView pv = p.GetComponent<PhotonView>();
                        if (pv != null)
                        {
                            if (p.GetComponent<AIBotController>() != null)
                                lastAliveName = "Bot " + pv.ViewID;
                            else if (pv.Owner != null)
                                lastAliveName = pv.Owner.NickName;
                        }
                    }
                }

                // 4. Declare the Winner if only 1 person is left!
                if (aliveCount <= 1)
                {
                    if (nns.survival == true) Canvas.GetComponent<KillCount>().SurvivalWinner(lastAliveName);
                    else if (nns.ctbMode == true) Canvas.GetComponent<TeamKillCount>().CTBWinner();
                }
            }
        }
    }

    IEnumerator JumpAgain()
    {
        yield return new WaitForSeconds(1);
        canJump = true;
    }
    IEnumerator RespawnWait()
    {
        yield return new WaitForSeconds(3);
        transform.position = startPos;
        isDead = false;
        respawned = false;
        transform.position = startPos;
        GetComponent<DisplayColor>().Respawn(GetComponent<PhotonView>().Owner.NickName);
    }
}
