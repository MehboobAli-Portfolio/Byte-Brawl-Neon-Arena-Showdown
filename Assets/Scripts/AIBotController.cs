using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;

public class AIBotController : MonoBehaviourPunCallbacks
{
    private NavMeshAgent agent;
    private Animator anim;
    private Transform targetPlayer;
    private DisplayColor displayColor;

    [Header("Weapons (Gun 1, Gun 2, Gun 3)")]
    public GameObject[] weaponMeshes;
    public int[] currentAmmo = { 60, 0, 0 };
    public float[] weaponDamages = { 0.1f, 0.25f, 0.4f };
    public float[] fireRates = { 1.5f, 0.8f, 2.0f };
    private int currentWeaponIndex = 0;
    private float nextFireTime;

    [Header("Dodging")]
    public float dodgeTimer = 2f;
    private float nextDodgeTime;
    public float attackRange = 15f;
    private Transform targetPickup;

    private string botName;
    private bool isRespawning = false;
    private NickNameScript nns;
    private SpawnCharacters spawnManager;
    private Timer gameTimer;

    void Start()
    {
        // --- NEW FIX: Teleport to a proper spawn point! ---
        SpawnCharacters spawner = GameObject.FindObjectOfType<SpawnCharacters>();
        if (spawner != null && spawner.spawnPoints.Length > 0)
        {
            // Pick a unique spawn point so they don't overlap
            int myIndex = GetComponent<PhotonView>().ViewID % spawner.spawnPoints.Length;

            // If the bot uses a NavMeshAgent, we have to Warp it safely
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(spawner.spawnPoints[myIndex].position);
            }
            else
            {
                this.transform.position = spawner.spawnPoints[myIndex].position;
            }

            this.transform.rotation = spawner.spawnPoints[myIndex].rotation;
        }
        currentAmmo = new int[] { 60, 0, 0 };
        weaponDamages = new float[] { 0.1f, 0.25f, 0.4f };
        fireRates = new float[] { 1.5f, 0.8f, 2.0f };
        gameObject.tag = "Player";
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        displayColor = GetComponent<DisplayColor>();

        botName = "Bot " + photonView.ViewID;

        GameObject namesBG = GameObject.Find("NamesBG");
        if (namesBG != null)
        {
            nns = namesBG.GetComponent<NickNameScript>();
            gameTimer = namesBG.GetComponent<Timer>();
        }

        GameObject spawnObj = GameObject.Find("SpawnScript");
        if (spawnObj != null) spawnManager = spawnObj.GetComponent<SpawnCharacters>();

        if (!PhotonNetwork.IsMasterClient)
        {
            if (agent != null) agent.enabled = false;
            return;
        }

        EquipBestWeapon();

        // Go back to the old, reliable way that worked yesterday!
        StartCoroutine(AutoPickColor());

        InvokeRepeating("FindClosestPlayer", 1f, 1f);
        InvokeRepeating("FindClosestPickup", 1f, 1f);
    }

    public void RefillAmmo(int weaponIndex, int amount)
    {
        currentAmmo[weaponIndex] += amount;
        targetPickup = null;
        EquipBestWeapon();
    }

    void EquipBestWeapon()
    {
        int best = 0;
        if (currentAmmo[0] > 0) best = 0;
        if (currentAmmo[1] > 0) best = 1;
        if (currentAmmo[2] > 0) best = 2;

        if (currentWeaponIndex != best || (weaponMeshes != null && weaponMeshes.Length > best && !weaponMeshes[best].activeSelf))
        {
            currentWeaponIndex = best;
            photonView.RPC("SyncBotWeapon", RpcTarget.AllBuffered, currentWeaponIndex);
        }
    }

    [PunRPC]
    void SyncBotWeapon(int wIndex)
    {
        if (weaponMeshes == null || weaponMeshes.Length == 0) return;
        for (int i = 0; i < weaponMeshes.Length; i++)
        {
            if (weaponMeshes[i] != null) weaponMeshes[i].SetActive(false);
        }
        if (weaponMeshes[wIndex] != null) weaponMeshes[wIndex].SetActive(true);
    }

    bool HasAnyAmmo()
    {
        return currentAmmo[0] > 0 || currentAmmo[1] > 0 || currentAmmo[2] > 0;
    }

    IEnumerator AutoPickColor()
    {
        // 1. PERFECT STAGGER: Use their unique Photon ViewID to space them out by 0.5 seconds each!
        // Example: ViewID 1001 waits 3.5s. ViewID 1002 waits 4.0s. ViewID 1003 waits 4.5s.
        float myUniqueDelay = 3.0f + ((photonView.ViewID % 100) * 0.5f);
        yield return new WaitForSeconds(myUniqueDelay);

        int mySlot = -1;

        // 2. Safely find the first empty color slot
        for (int i = 0; i < displayColor.viewID.Length; i++)
        {
            bool isSlotTaken = false;

            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in allPlayers)
            {
                DisplayColor dc = p.GetComponent<DisplayColor>();
                if (dc != null && dc.viewID[i] != 0)
                {
                    isSlotTaken = true;
                    break;
                }
            }

            // Claim it if nobody else has it!
            if (isSlotTaken == false)
            {
                mySlot = i;
                //displayColor.viewID[i] = photonView.ViewID;
                break;
            }
        }

        // 3. Announce it to the UI
        if (mySlot != -1)
        {
            photonView.RPC("BotClaimColor", RpcTarget.AllBuffered, mySlot, photonView.ViewID);
        }
    }

    [PunRPC]
    void BotClaimColor(int slotIndex, int botViewID)
    {
        // 1. Tell EVERY player's brain that this slot is now claimed
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        /*DisplayColor myDC = this.GetComponent<DisplayColor>();
        if (myDC != null)
        {
            myDC.viewID[slotIndex] = botViewID;
            // Force the UI to update so the color and name appear instantly!
            myDC.GetComponent<PhotonView>().RPC("AssignColor", RpcTarget.AllBuffered);
        }*/
        for (int i = 0; i < players.Length; i++)
        {
            DisplayColor dc = players[i].GetComponent<DisplayColor>();
            if (dc != null)
            {
                dc.viewID[slotIndex] = botViewID;
            }
        }

        // 2. Safely tell the UI to update for this specific bot!
        DisplayColor myDC = this.GetComponent<DisplayColor>();
        if (myDC != null)
        {
            myDC.ChooseColor();
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (gameTimer != null && gameTimer.timeStop == true)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            anim.SetFloat("BlendV", 0);
            return;
        }

        if (anim.GetBool("Dead") == true)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (!isRespawning)
            {
                bool noRespawn = (nns != null) && (nns.survival || nns.ctbMode);
                if (!noRespawn) StartCoroutine(BotRespawnRoutine());
            }
            return;
        }

        if (!HasAnyAmmo())
        {
            if (targetPickup != null)
            {
                if (agent.isOnNavMesh) agent.isStopped = false;
                agent.SetDestination(targetPickup.position);
                anim.SetFloat("BlendV", 1);
            }
            else
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                anim.SetFloat("BlendV", 0);
            }
            return;
        }

        bool isTargetDead = false;
        if (targetPlayer != null)
        {
            PlayerMovement targetPM = targetPlayer.GetComponent<PlayerMovement>();
            if (targetPM != null) isTargetDead = targetPM.isDead;
            else
            {
                Animator targetAnim = targetPlayer.GetComponent<Animator>();
                if (targetAnim != null) isTargetDead = targetAnim.GetBool("Dead");
            }
        }

        if (targetPlayer != null && !isTargetDead)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            if (distance <= attackRange && CanSeeTarget())
            {
                Vector3 direction = (targetPlayer.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

                if (Time.time >= nextDodgeTime)
                {
                    if (agent.isOnNavMesh) agent.isStopped = false;
                    anim.SetFloat("BlendV", 1);
                    Vector3 randomDir = transform.right * (Random.value > 0.5f ? 1f : -1f);
                    agent.SetDestination(transform.position + (randomDir * 4f));
                    nextDodgeTime = Time.time + dodgeTimer;
                }

                if (Time.time >= nextFireTime)
                {
                    ShootPlayer();
                    currentAmmo[currentWeaponIndex]--;
                    if (currentAmmo[currentWeaponIndex] <= 0) EquipBestWeapon();
                    nextFireTime = Time.time + fireRates[currentWeaponIndex];
                }
            }
            else
            {
                if (agent.isOnNavMesh) agent.isStopped = false;
                agent.SetDestination(targetPlayer.position);
                anim.SetFloat("BlendV", 1);
            }
        }
        else
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            anim.SetFloat("BlendV", 0);
        }
    }

    bool CanSeeTarget()
    {
        if (targetPlayer == null) return false;
        Vector3 rayStart = transform.position + Vector3.up * 1.0f;
        Vector3 rayEnd = targetPlayer.position + Vector3.up * 1.0f;
        Vector3 direction = (rayEnd - rayStart).normalized;
        float distanceToTarget = Vector3.Distance(rayStart, rayEnd);

        RaycastHit hit;
        if (Physics.Raycast(rayStart, direction, out hit, distanceToTarget))
        {
            if (!hit.collider.CompareTag("Player")) return false;
        }
        return true;
    }

    IEnumerator BotRespawnRoutine()
    {
        isRespawning = true;
        yield return new WaitForSeconds(3f);

        if (spawnManager != null && spawnManager.spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnManager.spawnPoints.Length);
            agent.Warp(spawnManager.spawnPoints[randomIndex].position);
        }

        anim.SetBool("Dead", false);
        displayColor.Respawn(botName);

        currentAmmo[0] = 60;
        currentAmmo[1] = 0;
        currentAmmo[2] = 0;
        EquipBestWeapon();

        isRespawning = false;
    }

    void FindClosestPlayer()
    {
        if (!HasAnyAmmo()) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (GameObject player in players)
        {
            if (player == this.gameObject) continue;

            bool isDead = false;
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null) isDead = pm.isDead;
            else
            {
                Animator playerAnim = player.GetComponent<Animator>();
                if (playerAnim != null) isDead = playerAnim.GetBool("Dead");
            }

            if (isDead) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = player.transform;
            }
        }
        targetPlayer = bestTarget;
    }

    void FindClosestPickup()
    {
        if (HasAnyAmmo()) return;

        WeaponPickups[] pickups = FindObjectsOfType<WeaponPickups>();
        float closestDistance = Mathf.Infinity;
        Transform bestPickup = null;

        foreach (WeaponPickups pickup in pickups)
        {
            Collider col = pickup.GetComponent<Collider>();
            if (col != null && !col.enabled) continue;

            float distance = Vector3.Distance(transform.position, pickup.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestPickup = pickup.transform;
            }
        }
        targetPickup = bestPickup;
    }

    void ShootPlayer()
    {
        PhotonView targetView = targetPlayer.GetComponent<PhotonView>();
        if (targetView != null)
        {
            string targetName = "";
            if (targetPlayer.GetComponent<AIBotController>() != null) targetName = "Bot " + targetView.ViewID;
            else targetName = targetView.Owner.NickName;

            displayColor.DeliverDamage(botName, targetName, weaponDamages[currentWeaponIndex]);
        }
    }
}