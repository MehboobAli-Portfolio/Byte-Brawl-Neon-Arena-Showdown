using UnityEngine;
using System.Collections;  
using UnityEngine.Animations.Rigging;
using Cinemachine;
using Photon.Pun;

public class WeaponChangeAdvance : MonoBehaviour
{
    public TwoBoneIKConstraint leftHand;
    public TwoBoneIKConstraint rightHand;
    public TwoBoneIKConstraint leftHandThumb;

    private CinemachineVirtualCamera cam;
    private GameObject camObject;

    public MultiAimConstraint[] aimObjects;
    private Transform aimTarget;

    public RigBuilder rig;
    public Transform[] leftTargets;
    public Transform[] rightTargets;
    public Transform[] leftThumbTargets;
    public GameObject[] weapons;
    public int weaponNumber = 0;
    private GameObject testForWeapons;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camObject = GameObject.Find("PlayerCam");
        aimTarget = GameObject.Find("AimRef").transform;
        if (this.gameObject.GetComponent<PhotonView>().IsMine)
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
        testForWeapons= GameObject.Find("Weapon1PickUp(Clone)");
        if (testForWeapons == null)
        {
            var spawner = GameObject.Find("SpawnScript");
            spawner.GetComponent<SpawnCharacters>().SpawnWeaponStart();
        }
    }
    /*void SetLookAt()
    {
        if(cam != null)
        {
            for(int i=0; i < aimObjects.Length ; i++)
            {
                var target = aimObjects[i].data.sourceObjects;
                target.SetTransform(0, aimTarget);
                aimObjects[i].data.sourceObjects=target;
            }
            rig.Build();
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        // Your update logic here
        if (Input.GetMouseButtonDown(1))
        {
            weaponNumber++;
            if (weaponNumber > weapons.Length -1)
            {
                weaponNumber = 0;
            }
            for (int i = 0; i < weapons.Length; i++)
            {
                if (i == weaponNumber)
                {
                    weapons[i].SetActive(true);
                    leftHand.data.target = leftTargets[i];
                    rightHand.data.target = rightTargets[i];
                    leftHandThumb.data.target = leftThumbTargets[i];
                }
                else
                {
                    weapons[i].SetActive(false);
                }
            }
            rig.Build();
        }
    }
}
