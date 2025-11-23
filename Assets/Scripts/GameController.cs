using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{

    [Header("Legacy (optional)")]
    [Tooltip("If you still use a single prefab without PoolManager, assign it here. Otherwise leave empty and use pool tags.")]
    public GameObject obstaculePrefab;

    [Header("Pooling")]
    [Tooltip("List of pool tags to choose from when spawning obstacles. These tags must match PoolManager pools.")]
    public string[] obstaclePoolTags;
    public Vector3 spawnValues;
    public int obstaculeCount;
    public float spawnWait;
    public float startWait;
    public float waveWait;

    [Header("Difficulty Progression")]
    [Tooltip("Multiplier applied to mover speed after each completed wave (e.g. 1.1 = +10% per wave)")]
    public float speedMultiplierPerWave = 1.1f;
    [Tooltip("How many extra obstacles to add after each wave")]
    public int countIncreasePerWave = 1;
    [Tooltip("Maximum obstacles allowed (safety cap)")]
    public int maxObstaculeCount = 200;

    // runtime current speed multiplier (starts at 1)
    float _currentSpeedMultiplier = 1f;

    private int score;
    public TextMeshProUGUI scoreText;

    // Runtime filtered list of valid pool tags (non-empty and registered in PoolManager)
    string[] _validObstacleTags;

    void Start()
    {
        score = 0;
        UpdateScore();
        // Filter configured tags against PoolManager to avoid empty or missing tags
        if (obstaclePoolTags != null && obstaclePoolTags.Length > 0 && PoolManager.Instance != null)
        {
            var valid = new List<string>();
            foreach (var t in obstaclePoolTags)
            {
                if (string.IsNullOrEmpty(t)) continue;
                if (PoolManager.Instance.HasPool(t)) valid.Add(t);
                else Debug.LogWarning($"GameController: obstacle pool tag '{t}' not found in PoolManager and will be ignored.");
            }
            _validObstacleTags = valid.ToArray();
        }

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

                // If filtered pool tags provided, spawn from PoolManager choosing a random valid tag
                var tagsToUse = (_validObstacleTags != null && _validObstacleTags.Length > 0) ? _validObstacleTags : obstaclePoolTags;
                if (tagsToUse != null && tagsToUse.Length > 0 && PoolManager.Instance != null)
                {
                    string tag = tagsToUse[Random.Range(0, tagsToUse.Length)];
                    var obj = PoolManager.Instance.SpawnFromPool(tag, spawnPosition, Quaternion.identity, _currentSpeedMultiplier);
                    // SpawnFromPool applies the multiplier for pooled objects. If SpawnFromPool returned null, fallback to legacy instantiation.
                    if (obj == null)
                    {
                        if (obstaculePrefab != null)
                        {
                            var inst = Instantiate(obstaculePrefab, spawnPosition, Quaternion.identity);
                            var mover = inst.GetComponent<Mover>();
                            if (mover != null) mover.SetSpeedMultiplier(_currentSpeedMultiplier);
                        }
                    }
                }
                else
                {
                    // Legacy behavior: instantiate the assigned prefab directly
                    if (obstaculePrefab != null)
                        Instantiate(obstaculePrefab, spawnPosition, Quaternion.identity);
                    else
                        Debug.LogWarning("GameController: no obstaclePoolTags configured and no obstaculePrefab assigned.");
                }

                yield return new WaitForSeconds(spawnWait);
            }
            yield return new WaitForSeconds(waveWait);
            // After completing a wave, increase difficulty
            if (countIncreasePerWave != 0)
            {
                obstaculeCount = Mathf.Min(maxObstaculeCount, obstaculeCount + countIncreasePerWave);
            }
            if (speedMultiplierPerWave > 0f)
            {
                _currentSpeedMultiplier *= speedMultiplierPerWave;
            }
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

    public void OnObstacleHit(GameObject other)
    {
        var near = GetComponent<NearMissDetector>();
        if (near != null) near.MarkHit(other.gameObject);
    }
}
