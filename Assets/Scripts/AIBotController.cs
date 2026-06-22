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

    [Header("Bot Settings")]
    public float botSpeed = 5.0f; // NEW: Change this in Inspector to make them faster/slower!

    [Header("Weapons (Gun 1, Gun 2, Gun 3)")]
    public GameObject[] weaponMeshes;
    public int[] currentAmmo = { 60, 0, 0 };
    public float[] weaponDamages = { 0.1f, 0.25f, 0.4f };
    public float[] fireRates = { 1.5f, 0.8f, 2.0f };
    private int currentWeaponIndex = 0;
    private float nextFireTime;

    [Header("Dodging & Movement")]
    public float dodgeTimer = 2f; // Made dodge slightly faster
    private float nextDodgeTime;
    public float attackRange = 15f;
    private Transform targetPickup;
    private float currentSpeed = 0f;

    private string botName;
    private bool isRespawning = false;
    private NickNameScript nns;
    private SpawnCharacters spawnManager;

    private static int globalBotSpawnCounter = 0;

    void Start()
    {
        int myBotIndex = globalBotSpawnCounter;
        globalBotSpawnCounter++;

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        displayColor = GetComponent<DisplayColor>();
        botName = "Bot " + photonView.ViewID;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (agent != null)
        {
            agent.speed = botSpeed; // NEW: Uses the variable so you can change it
            agent.stoppingDistance = 0.5f; // Lowered so they can strafe smoothly
        }

        GameObject namesBG = GameObject.Find("NamesBG");
        if (namesBG != null) nns = namesBG.GetComponent<NickNameScript>();

        spawnManager = FindAnyObjectByType<SpawnCharacters>();
        if (spawnManager == null)
        {
            GameObject spawnObj = GameObject.Find("SpawnScript");
            if (spawnObj != null) spawnManager = spawnObj.GetComponent<SpawnCharacters>();
        }

        if (spawnManager != null && spawnManager.spawnPoints.Length > 0)
        {
            int humanCount = PhotonNetwork.CurrentRoom.PlayerCount;
            int finalIndex = (myBotIndex + humanCount) % spawnManager.spawnPoints.Length;

            Vector3 exactPos = spawnManager.spawnPoints[finalIndex].position;
            Vector3 safeSpread = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Vector3 testPos = exactPos + safeSpread;

            if (agent != null) agent.enabled = false;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPos, out hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            else
            {
                transform.position = exactPos;
            }

            transform.rotation = spawnManager.spawnPoints[finalIndex].rotation;
            if (agent != null) agent.enabled = true;
        }

        currentAmmo = new int[] { 60, 0, 0 };
        weaponDamages = new float[] { 0.1f, 0.25f, 0.4f };
        fireRates = new float[] { 1.5f, 0.8f, 2.0f };
        gameObject.tag = "Player";

        if (!PhotonNetwork.IsMasterClient)
        {
            if (agent != null) agent.enabled = false;
            return;
        }

        EquipBestWeapon();
        StartCoroutine(AutoPickColor());
        InvokeRepeating("FindClosestPlayer", 0.5f, 0.5f);
        InvokeRepeating("FindClosestPickup", 1f, 1f);
    }
    // --- NEW FIX: HOST MIGRATION WAKE-UP & UI REFRESH ---
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            // 1. Turn the NavMeshAgent (movement) back on!
            if (agent != null) agent.enabled = true;

            // 2. Restart the AI thinking loops!
            InvokeRepeating("FindClosestPlayer", 0.5f, 0.5f);
            InvokeRepeating("FindClosestPickup", 1f, 1f);

            // 3. FORCE THE UI TO TURN THE HEALTH BARS BACK ON!
            int mySlot = -1;
            DisplayColor myDC = GetComponent<DisplayColor>();
            if (myDC != null)
            {
                for (int i = 0; i < myDC.viewID.Length; i++)
                {
                    if (myDC.viewID[i] == photonView.ViewID)
                    {
                        mySlot = i;
                        break;
                    }
                }
                // Resend the buffered RPC so the new host keeps the UI alive
                if (mySlot != -1)
                {
                    photonView.RPC("BotClaimColor", RpcTarget.AllBuffered, mySlot, photonView.ViewID);
                }
            }

            Debug.Log("The Master Client left! I am taking control and fixing the UI!");
        }
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
        if (!PhotonNetwork.IsMasterClient || agent == null || anim == null) return;

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
                SmoothMove();
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
                // ALWAYS LOOK AT THE PLAYER
                Vector3 direction = targetPlayer.position - transform.position;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
                }

                // SHOOT LOGIC
                if (Time.time >= nextFireTime)
                {
                    ShootPlayer();
                    currentAmmo[currentWeaponIndex]--;
                    if (currentAmmo[currentWeaponIndex] <= 0) EquipBestWeapon();
                    nextFireTime = Time.time + fireRates[currentWeaponIndex];
                }

                // NEW DODGE LOGIC: Strafe side-to-side instead of freezing
                if (Time.time >= nextDodgeTime)
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;

                        // Pick a random side (Left or Right) to strafe
                        Vector3 randomDir = transform.right * (Random.value > 0.5f ? 1f : -1f);
                        // Add a tiny bit of forward/backward so they don't get stuck in corners
                        Vector3 forwardMix = transform.forward * Random.Range(-0.5f, 0.5f);

                        Vector3 dodgePos = transform.position + (randomDir * 4f) + forwardMix;

                        NavMeshHit dodgeHit;
                        if (NavMesh.SamplePosition(dodgePos, out dodgeHit, 4f, NavMesh.AllAreas))
                        {
                            agent.SetDestination(dodgeHit.position);
                        }
                    }
                    nextDodgeTime = Time.time + dodgeTimer;
                }

                // Control Animations while dodging
                if (agent.isOnNavMesh)
                {
                    if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    {
                        agent.isStopped = true;
                        SmoothStop();
                    }
                    else
                    {
                        agent.isStopped = false;
                        SmoothMove();
                    }
                }
            }
            else
            {
                // Chase the player if they are too far away or hiding
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPlayer.position);
                    SmoothMove();
                }
            }
        }
        else
        {
            // Patrol if no player is found
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                SmoothMove();

                if (!agent.pathPending && agent.remainingDistance < 1f && spawnManager != null)
                {
                    int randomPoint = Random.Range(0, spawnManager.spawnPoints.Length);
                    agent.SetDestination(spawnManager.spawnPoints[randomPoint].position);
                }
            }
        }
    }

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
            Vector3 exactPos = spawnManager.spawnPoints[randomIndex].position;
            Vector3 safeSpread = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Vector3 testPos = exactPos + safeSpread;

            if (agent != null) agent.enabled = false;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPos, out hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            else
            {
                transform.position = exactPos;
            }

            if (agent != null) agent.enabled = true;
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

            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv == null) continue;

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
        // 1. CHECK IF WE HAVE "GOOD" AMMO IN ANY WEAPON
        bool hasGoodAmmo = false;

        // Gun 0 (e.g., Assault Rifle) - considers 15+ bullets "good"
        if (currentAmmo[0] > 15) hasGoodAmmo = true;

        // Gun 1 (e.g., Shotgun) - considers 4+ bullets "good"
        if (currentAmmo.Length > 1 && currentAmmo[1] > 4) hasGoodAmmo = true;

        // Gun 2 (e.g., Sniper/RPG) - considers 2+ bullets "good"
        if (currentAmmo.Length > 2 && currentAmmo[2] > 1) hasGoodAmmo = true;

        // If the bot has a healthy amount of ammo in ANY gun, keep fighting!
        if (hasGoodAmmo) return;

        // 2. IF AMMO IS CRITICAL, FIND THE CLOSEST PICKUP
        WeaponPickups[] pickups = FindObjectsByType<WeaponPickups>(FindObjectsInactive.Exclude);
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
        DisplayColor targetDC = targetPlayer.GetComponent<DisplayColor>();
        if (targetDC != null) targetDC.DeliverDamage(botName, "Target", weaponDamages[currentWeaponIndex]);
    }

    bool IsTeammate(GameObject potentialTarget)
    {
        if (nns == null || (!nns.teamMode && !nns.ctbMode)) return false;

        int mySlot = -1, theirSlot = -1;
        DisplayColor myDC = this.GetComponent<DisplayColor>();
        if (myDC == null) return false;

        PhotonView myPV = this.GetComponent<PhotonView>();
        PhotonView theirPV = potentialTarget.GetComponent<PhotonView>();

        if (myPV == null || theirPV == null) return false;

        int myViewID = myPV.ViewID;
        int theirViewID = theirPV.ViewID;

        for (int i = 0; i < myDC.viewID.Length; i++)
        {
            if (myDC.viewID[i] == myViewID) mySlot = i;
            if (myDC.viewID[i] == theirViewID) theirSlot = i;
        }

        if (mySlot != -1 && theirSlot != -1)
        {
            bool amIRed = (mySlot <= 2);
            bool areTheyRed = (theirSlot <= 2);
            if (amIRed == areTheyRed) return true;
        }

        return false;
    }
}