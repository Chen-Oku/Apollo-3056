using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{

    public GameObject obstaculePrefab;
    public Vector3 spawnValues;
    public int obstaculeCount;
    public float spawnWait;
    public float startWait;
    public float waveWait;

    private int score;
    public TextMeshProUGUI scoreText;

    void Start()
    {
        score = 0;
        UpdateScore();
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(startWait);
        while (true)
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

    public void AddScore(int value)
    {
        score += value;
        UpdateScore();
    }
    
    void UpdateScore()
    {
        scoreText.text = "Score: " + score;
    }
}
