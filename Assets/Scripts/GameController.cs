using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{

    public GameObject obstaculePrefab;
    public Vector3 spawnValues;
    public int obstaculeCount;
    public float spawnWait;
    public float startWait;
    public float waveWait;
    
    void Start()
    {
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(startWait);
        while(true)
        {
            for (int i = 0; i < obstaculeCount; i++)
            {
                Vector3 spawnPosition = new Vector3(Random.Range(-spawnValues.x, spawnValues.x), spawnValues.y, spawnValues.z);
                Instantiate(obstaculePrefab, spawnPosition, Quaternion.identity);

                yield return new WaitForSeconds(spawnWait);
            }
            yield return new WaitForSeconds(waveWait);
        }
        
    }
}
