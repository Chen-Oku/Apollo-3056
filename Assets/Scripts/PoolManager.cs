using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        [Tooltip("If you want multiple models under the same tag, add them here. The single 'prefab' field is kept for backward compatibility.")]
        public List<GameObject> prefabs;
        public int size = 10;
    }

    public List<Pool> pools;

    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();

    foreach (var pool in pools)
        {
            // Basic validation
            if (string.IsNullOrEmpty(pool.tag))
            {
                Debug.LogWarning("PoolManager: encountered a pool with empty tag - skipping.");
                continue;
            }

            // Collect available prefabs for this pool (support both legacy single prefab and new list)
            var availablePrefabs = new List<GameObject>();
            if (pool.prefab != null) availablePrefabs.Add(pool.prefab);
            if (pool.prefabs != null)
            {
                foreach (var p in pool.prefabs)
                    if (p != null) availablePrefabs.Add(p);
            }

            if (availablePrefabs.Count == 0)
            {
                Debug.LogWarning($"PoolManager: no prefabs assigned for pool '{pool.tag}' - skipping.");
                continue;
            }

            if (poolDictionary.ContainsKey(pool.tag))
            {
                Debug.LogWarning($"PoolManager: duplicate pool tag '{pool.tag}' found - skipping duplicate.");
                continue;
            }

            var objectQueue = new Queue<GameObject>();
            for (int i = 0; i < Mathf.Max(1, pool.size); i++)
            {
                // Pick a prefab among available (randomize variety)
                var prefabToUse = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
                var obj = Instantiate(prefabToUse, transform);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectQueue);
        }
    }

    // Optional speedMultiplier is applied to a Mover component (if present) BEFORE the object is activated,
    // so that OnObjectSpawn/OnEnable see the updated speed.
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, float speedMultiplier = 1f)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist");
            return null;
        }

        var queue = poolDictionary[tag];

        GameObject objectToSpawn = null;

        // Try to find an inactive object in the queue
        int attempts = queue.Count;
        for (int i = 0; i < attempts; i++)
        {
            var obj = queue.Dequeue();
            queue.Enqueue(obj);
            if (!obj.activeInHierarchy)
            {
                objectToSpawn = obj;
                break;
            }
        }

        // If none available, instantiate a new one and add to the queue
        if (objectToSpawn == null)
        {
            // Find pool entry and pick a random prefab from its available prefabs
            var poolEntry = pools.Find(p => p.tag == tag);
            if (poolEntry == null)
            {
                Debug.LogWarning($"Pool entry for tag '{tag}' not found");
                return null;
            }

            var availablePrefabs = new List<GameObject>();
            if (poolEntry.prefab != null) availablePrefabs.Add(poolEntry.prefab);
            if (poolEntry.prefabs != null)
            {
                foreach (var p in poolEntry.prefabs) if (p != null) availablePrefabs.Add(p);
            }

            if (availablePrefabs.Count == 0)
            {
                Debug.LogWarning($"Prefab for pool '{tag}' not found");
                return null;
            }

            var prefabToInstantiate = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
            objectToSpawn = Instantiate(prefabToInstantiate, transform);
            queue.Enqueue(objectToSpawn);
        }

        // Apply speed multiplier to Mover (if present) BEFORE activation so OnObjectSpawn/OnEnable use it
        var mover = objectToSpawn.GetComponent<Mover>();
        if (mover != null)
        {
            mover.SetSpeedMultiplier(speedMultiplier);
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        var pooled = objectToSpawn.GetComponent<IPooledObject>();
        if (pooled != null) pooled.OnObjectSpawn();

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        var pooled = obj.GetComponent<IPooledObject>();
        if (pooled != null) pooled.OnObjectReturn();

        // Reset common physics state if present
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use standard Rigidbody API
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        obj.SetActive(false);
    }

    /// <summary>
    /// Returns true if a pool with the given tag exists.
    /// </summary>
    public bool HasPool(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        return poolDictionary != null && poolDictionary.ContainsKey(tag);
    }

    /// <summary>
    /// Returns all pool tags currently registered.
    /// </summary>
    public string[] GetAllPoolTags()
    {
        if (poolDictionary == null) return new string[0];
        var keys = new string[poolDictionary.Count];
        poolDictionary.Keys.CopyTo(keys, 0);
        return keys;
    }
}
