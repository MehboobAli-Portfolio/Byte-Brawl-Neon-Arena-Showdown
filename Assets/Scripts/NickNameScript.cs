using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
public class NickNameScript : MonoBehaviour
{
    public Text[] names;
    public Image[] healthbars;
    private void Start()
    {
        for (int i = 0; i < names.Length; i++)
        {
            names[i].gameObject.SetActive(false);
            healthbars[i].gameObject.SetActive(false);
        }
    }
}
