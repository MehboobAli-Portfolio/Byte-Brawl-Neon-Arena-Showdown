using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;
public class Timer : MonoBehaviour
{
    public Text minutesText;
    public Text secondsText;
    public int minutes = 4;
    public int seconds = 59;
    public GameObject Canvas;
    [HideInInspector]
    public bool timeStop = false;

    public void BeginTimer()
    {
        GetComponent<PhotonView>().RPC("Count", RpcTarget.AllBuffered);
    }
    [PunRPC]
    void Count()
    {
        BeginCounting();
    }
    void BeginCounting()
    {
        CancelInvoke();
        InvokeRepeating("TimeCountDown", 1, 1);
    }
    void TimeCountDown()
    {
        if(seconds > 10)
        {
            seconds -= 1;
            secondsText.text = seconds.ToString();
        }
        else if(seconds > 0 && seconds < 11)
        {
            seconds -= 1;
            secondsText.text = "0" + seconds.ToString();
        }
        else if (seconds ==  0 && minutes > 0 )
        {
            secondsText.text = "0" + seconds.ToString();
            minutes -= 1;
            seconds = 59;
            minutesText.text = minutes.ToString();
            secondsText.text = seconds.ToString();
        }
        if(seconds == 0 && minutes <= 0)
        {
            if (this.gameObject.GetComponent<NickNameScript>().teamMode == true)
            {
                Canvas.GetComponent<TeamKillCount>().countDown = false;
                Canvas.GetComponent<TeamKillCount>().TimeOver();
                timeStop = true;
            }
            if (this.gameObject.GetComponent<NickNameScript>().ctbMode == true)
            {
                Canvas.GetComponent<TeamKillCount>().countDown = false;
                Canvas.GetComponent<TeamKillCount>().TimeOver();
                timeStop = true;
            }
            if (this.gameObject.GetComponent<NickNameScript>().teamMode == false && this.gameObject.GetComponent<NickNameScript>().ctbMode == false)
            {
                Canvas.GetComponent<KillCount>().countDown = false;
                Canvas.GetComponent<KillCount>().TimeOver();
                timeStop = true;
            }
        }

    }
}
