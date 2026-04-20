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

    [Header("Dodging & Movement")]
    public float dodgeTimer = 2f;
    private float nextDodgeTime;
    public float attackRange = 15f;
    private Transform targetPickup;
    private float currentSpeed = 0f; // to make animation smooth when starting and stopping movement

    private string botName;
    private bool isRespawning = false;
    private NickNameScript nns;
    private SpawnCharacters spawnManager;
    private Timer gameTimer;

    void Start()
    {
        SpawnCharacters spawner = GameObject.FindObjectOfType<SpawnCharacters>();
        if (spawner != null && spawner.spawnPoints.Length > 0)
        {
            int myIndex = GetComponent<PhotonView>().ViewID % spawner.spawnPoints.Length;
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.Warp(spawner.spawnPoints[myIndex].position);
            else this.transform.position = spawner.spawnPoints[myIndex].position;

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
        float myUniqueDelay = 3.0f + ((photonView.ViewID % 100) * 0.5f);
        yield return new WaitForSeconds(myUniqueDelay);

        int mySlot = -1;
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
            if (isSlotTaken == false)
            {
                mySlot = i;
                break;
            }
        }
        if (mySlot != -1) photonView.RPC("BotClaimColor", RpcTarget.AllBuffered, mySlot, photonView.ViewID);
    }

    [PunRPC]
    void BotClaimColor(int slotIndex, int botViewID)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            DisplayColor dc = players[i].GetComponent<DisplayColor>();
            if (dc != null) dc.viewID[slotIndex] = botViewID;
        }

        DisplayColor myDC = this.GetComponent<DisplayColor>();
        if (myDC != null) myDC.ChooseColor();
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (gameTimer != null && gameTimer.timeStop == true)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            SmoothStop(); // NEW: Smoothly stop animating
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
                SmoothMove(); // NEW: Smoothly start running
            }
            else
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                SmoothStop();
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
                // --- NEW BENDING FIX FOR AI SPINE ---
                Vector3 direction = (targetPlayer.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                Vector3 euler = targetRotation.eulerAngles;

                if (euler.x > 180) euler.x -= 360;
                euler.x = Mathf.Clamp(euler.x, -30f, 30f); // Prevents spine snapping

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, euler.y, 0), Time.deltaTime * 5f);
                // ------------------------------------

                if (Time.time >= nextDodgeTime)
                {
                    if (agent.isOnNavMesh) agent.isStopped = false;
                    SmoothMove();
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
                SmoothMove();
            }
        }
        else
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            SmoothStop();
        }
    }

    // --- NEW: Custom Methods to make movement look perfectly natural ---
    void SmoothMove()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, 1f, Time.deltaTime * 5f);
        anim.SetFloat("BlendV", currentSpeed);
    }

    void SmoothStop()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 8f);
        anim.SetFloat("BlendV", currentSpeed);
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

    // --- NEW TARGETING FIX: Bots will always hunt humans first! ---
    void FindClosestPlayer()
    {
        if (!HasAnyAmmo()) return;

        // This line grabs EVERYONE (Humans and Bots)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (GameObject player in players)
        {
            if (player == this.gameObject) continue;

            // Keep the Team check! We still don't want them shooting teammates.
            if (IsTeammate(player)) continue;

            bool isDead = false;
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null) isDead = pm.isDead;
            else
            {
                Animator playerAnim = player.GetComponent<Animator>();
                if (playerAnim != null) isDead = playerAnim.GetBool("Dead");
            }

            if (isDead) continue;

            // Pure distance check: It doesn't matter who or what it is, just how close it is!
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = player.transform;
            }
        }

        // Set the target to whoever was closest
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
        // 1. Grab the DisplayColor script of the ENEMY we are aiming at
        DisplayColor targetDC = targetPlayer.GetComponent<DisplayColor>();

        if (targetDC != null)
        {
            // 2. Tell the ENEMY to take damage from this bot!
            targetDC.DeliverDamage(botName, "Target", weaponDamages[currentWeaponIndex]);
        }
    }
    // --- NEW: Teammate Recognition Logic ---
    bool IsTeammate(GameObject potentialTarget)
    {
        // 1. If we are not in a team mode, everyone is an enemy!
        if (nns == null || (!nns.teamMode && !nns.ctbMode)) return false;

        int mySlot = -1;
        int theirSlot = -1;

        DisplayColor myDC = this.GetComponent<DisplayColor>();

        int myViewID = this.GetComponent<PhotonView>().ViewID;
        int theirViewID = potentialTarget.GetComponent<PhotonView>().ViewID;

        // 2. Find which UI slot we both belong to
        for (int i = 0; i < myDC.viewID.Length; i++)
        {
            if (myDC.viewID[i] == myViewID) mySlot = i;
            if (myDC.viewID[i] == theirViewID) theirSlot = i;
        }

        // 3. If slots are valid, check if we are on the same team (0,1,2 = Red | 3,4,5 = Blue)
        if (mySlot != -1 && theirSlot != -1)
        {
            bool amIRed = (mySlot <= 2);
            bool areTheyRed = (theirSlot <= 2);

            if (amIRed == areTheyRed) return true; // We are on the same team!
        }

        return false;
    }
}