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
            var objectQueue = new Queue<GameObject>();
            for (int i = 0; i < Mathf.Max(1, pool.size); i++)
            {
                var obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectQueue);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
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
            var prefab = pools.Find(p => p.tag == tag)?.prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab for pool '{tag}' not found");
                return null;
            }
            objectToSpawn = Instantiate(prefab);
            queue.Enqueue(objectToSpawn);
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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        obj.SetActive(false);
    }
}
