using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
public class TeamKillCount : MonoBehaviour
{
    public List<Kills> highestKills = new List<Kills>();
    public Text[] killAmts;
    private GameObject killCountPanel;
    private GameObject namesObject;
    private bool killCountOn = false;
    public bool countDown = true;
    public GameObject winnerPanel;
    public Text winnerText;
    private int RedTeamKills;
    private int BlueTeamKills;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        killCountPanel = GameObject.Find("KillCountPanel");
        namesObject = GameObject.Find("NamesBG");
        killCountPanel.SetActive(false);
        winnerPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && countDown == true)
        {
            if (killCountOn == false)
            {
                killCountPanel.SetActive(true);
                killCountOn = true;
                highestKills.Clear();
                for (int i = 0; i < 6; i++)
                {
                    highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
                }
                RedTeamKills = highestKills[0].playerKills + highestKills[1].playerKills + highestKills[2].playerKills;
                BlueTeamKills = highestKills[3].playerKills + highestKills[4].playerKills + highestKills[5].playerKills;
                killAmts[0].text = RedTeamKills.ToString();
                killAmts[1].text = BlueTeamKills.ToString();
            }
            else if (killCountOn == true)
            {
                killCountPanel.SetActive(false);
                killCountOn = false;
            }
        }
    }
    public void TimeOver()
    {
        killCountPanel.SetActive(true);
        winnerPanel.SetActive(true);
        killCountOn = true;
        highestKills.Clear();
        for (int i = 0; i < 6; i++)
        {
            highestKills.Add(new Kills(namesObject.GetComponent<NickNameScript>().names[i].text, namesObject.GetComponent<NickNameScript>().kills[i]));
        }
        RedTeamKills = highestKills[0].playerKills + highestKills[1].playerKills + highestKills[2].playerKills;
        BlueTeamKills = highestKills[3].playerKills + highestKills[4].playerKills + highestKills[5].playerKills;
        killAmts[0].text = RedTeamKills.ToString();
        killAmts[1].text = BlueTeamKills.ToString();
        if (RedTeamKills > BlueTeamKills)
        {
            winnerText.text = "Red Team Wins!";
        }
        else if (BlueTeamKills > RedTeamKills)
        {
            winnerText.text = "Blue Team Wins!";
        }
        else
        {
            winnerText.text = "It's a Tie!";
        }
    }
}
