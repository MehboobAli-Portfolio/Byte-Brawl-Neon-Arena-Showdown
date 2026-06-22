using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class RespawnTimer : MonoBehaviour
{
    public Text spawnTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        StartCoroutine(SpawnStarting());
    }
    IEnumerator SpawnStarting()
    {
        if (spawnTime != null) spawnTime.text = "3";
        yield return new WaitForSeconds(1);
        if (spawnTime != null) spawnTime.text = "2";
        yield return new WaitForSeconds(1);
        if (spawnTime != null) spawnTime.text = "1";
        yield return new WaitForSeconds(1);
        this.gameObject.SetActive(false);
    }
}
