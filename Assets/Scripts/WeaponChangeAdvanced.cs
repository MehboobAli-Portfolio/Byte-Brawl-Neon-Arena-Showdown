using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Cinemachine;
using UnityEngine.UI;
using Unity.Burst.CompilerServices;

public class WeaponChangeAdvanced : MonoBehaviour
{
	public TwoBoneIKConstraint leftHand;
	public TwoBoneIKConstraint rightHand;
	public TwoBoneIKConstraint leftThumb;


    private CinemachineVirtualCamera cam;
    private GameObject camObject;


    public MultiAimConstraint[] aimObjects;
    private Transform aimTarget;

	public RigBuilder rig;
    public Transform[] leftTargets;
    public Transform[] rightTargets;
	public Transform[] thumbTargets;
	public GameObject[] weapons;
    private int weaponNumber = 0;
    private GameObject testForWeapons;

    private Image weaponIcon;
    private Text ammoAmtText;
    public Sprite[] weaponIcons;
    public int[] AmmoAmts;
    public GameObject[] muzzleFlash;
    private string shooterName;
    private string gotShootName;
    public float[] damageAmts;
    public bool isDead= false;
    private GameObject choosePanel;
    // Start is called before the first frame update
    void Start()
	{
        choosePanel = GameObject.Find("ChoosePanel");
        weaponIcon = GameObject.Find("WeaponUI").GetComponent<Image>();
        ammoAmtText = GameObject.Find("AmmoAmt").GetComponent<Text>();
        camObject = GameObject.Find("PlayerCam");
        AmmoAmts[0] = 60;
        AmmoAmts[1] = 0;
        AmmoAmts[2] = 0;
        ammoAmtText.text = AmmoAmts[0].ToString();
        //aimTarget = GameObject.Find("AimRef").transform;
        if (this.gameObject.GetComponent<PhotonView>().IsMine == true)
        {
           cam = camObject.GetComponent<CinemachineVirtualCamera>();
           cam.Follow = this.gameObject.transform;
           cam.LookAt = this.gameObject.transform;
           //Invoke("SetLookAt", 0.1f);
        }
        else
        {
            this.gameObject.GetComponent<PlayerMovement>().enabled = false;
        }

        testForWeapons = GameObject.Find("Weapon1Pickup(Clone)");
        if (testForWeapons == null)
        {
            if (this.gameObject.GetComponent<PhotonView>().Owner.IsMasterClient == true)
            {
                var spawner = GameObject.Find("SpawnScript");
                spawner.GetComponent<SpawnCharacters>().SpawnWeaponsStart();
            }
        }
	}


    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && this.gameObject.GetComponent<PhotonView>().IsMine && isDead == false && choosePanel.activeInHierarchy == false && AmmoAmts[weaponNumber]>0)
        {
            AmmoAmts[weaponNumber]--;
            ammoAmtText.text = AmmoAmts[weaponNumber].ToString();
            GetComponent<DisplayColor>().PlayGunshot(GetComponent<PhotonView>().Owner.NickName,weaponNumber);
            this.GetComponent<PhotonView>().RPC("GunMuzzleFlash",RpcTarget.All);
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            this.gameObject.layer=LayerMask.NameToLayer("Ignore Raycast");
            if (Physics.Raycast(ray, out hit, 500f))
            {
                PhotonView targetView = hit.transform.gameObject.GetComponent<PhotonView>();
                if (targetView != null)
                {
                    // --- NEW: Bot Check Logic! ---
                    if (hit.transform.gameObject.GetComponent<AIBotController>() != null)
                    {
                        // We shot a Bot! Get its Bot ID.
                        gotShootName = "Bot " + targetView.ViewID;
                    }
                    else
                    {
                        // We shot a Human! Get their Owner NickName.
                        gotShootName = targetView.Owner.NickName;
                    }
                }
                
                if(hit.transform.gameObject.GetComponent<DisplayColor>() != null)
                {
                    hit.transform.gameObject.GetComponent<DisplayColor>().DeliverDamage(this.GetComponent<PhotonView>().Owner.NickName,gotShootName, damageAmts[weaponNumber]);
                }
                shooterName = GetComponent<PhotonView>().Owner.NickName;
            }
            this.gameObject.layer = LayerMask.NameToLayer("Default");
        }
         if (Input.GetMouseButtonDown(1) && this.gameObject.GetComponent<PhotonView>().IsMine && isDead == false)
        {
            //weaponNumber++;
            this.GetComponent<PhotonView>().RPC("Change", RpcTarget.AllBuffered);
            if (weaponNumber > weapons.Length - 1)
            {
                weaponIcon.GetComponent<Image>().sprite= weaponIcons[0];
                ammoAmtText.text = AmmoAmts[0].ToString();
                weaponNumber = 0;
            }
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].SetActive(false);
            }
            weapons[weaponNumber].SetActive(true);
            weaponIcon.GetComponent<Image>().sprite = weaponIcons[weaponNumber];
            ammoAmtText.text = AmmoAmts[weaponNumber].ToString();
            leftHand.data.target = leftTargets[weaponNumber];
            rightHand.data.target = rightTargets[weaponNumber];
            leftThumb.data.target = thumbTargets[weaponNumber];
            rig.Build();
        }
    }


    public void UpdatePickup()
    {
        ammoAmtText.text = AmmoAmts[weaponNumber].ToString();

    }

    [PunRPC]
    public void GunMuzzleFlash()
    {
        muzzleFlash[weaponNumber].SetActive(true);
        StartCoroutine(MuzzleOff());
    }

    [PunRPC]
	public void Change()
	{
		weaponNumber++;
		if (weaponNumber > weapons.Length - 1)
		{
			weaponNumber = 0;
		}
		for (int i = 0; i < weapons.Length; i++)
		{
			weapons[i].SetActive(false);
		}
		weapons[weaponNumber].SetActive(true);
		leftHand.data.target = leftTargets[weaponNumber];
		rightHand.data.target = rightTargets[weaponNumber];
		leftThumb.data.target = thumbTargets[weaponNumber];
		rig.Build();
	}
    IEnumerator MuzzleOff()
    {
        yield return new WaitForSeconds(0.03f);
        this.GetComponent<PhotonView>().RPC("MuzzleFlashOff",RpcTarget.All);
    }
    [PunRPC]
    public void MuzzleFlashOff()
    {
        muzzleFlash[weaponNumber].SetActive(false);
    }
}
