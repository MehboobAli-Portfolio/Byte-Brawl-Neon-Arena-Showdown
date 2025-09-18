using UnityEngine;
using System.Collections;  
using UnityEngine.Animations.Rigging;

public class WeaponChangeAdvance : MonoBehaviour
{
    public TwoBoneIKConstraint leftHand;
    public TwoBoneIKConstraint rightHand;
    public RigBuilder rig;
    public Transform[] leftTargets;
    public Transform[] rightTargets;
    public GameObject[] weapons;
    private int weaponNumber = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

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
