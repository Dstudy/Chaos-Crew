using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalItemPool : MonoBehaviour
{
    public static LocalItemPool singleton;

    [Header("Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 50;

    [Header("Debug")]
    [SerializeField] private int currentCount;
    [SerializeField] private int activeCount;
    
    private Stack<GameObject> pool = new Stack<GameObject>();
    private Transform poolParent;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            poolParent = transform;
            InitializePool();
        }
        else if (singleton != this)
        {
            Debug.LogWarning("Multiple LocalItemPool instances found! Destroying duplicate.");
            Destroy(this);
        }
    }

    public void SetPrefab(GameObject itemPrefab)
    {
        if (prefab == null && itemPrefab != null)
        {
            prefab = itemPrefab;
            InitializePool();
        }
    }

    private void InitializePool()
    {
        if (prefab == null)
        {
            Debug.LogWarning("LocalItemPool: Prefab is null, cannot initialize pool.");
            return;
        }

        // Clear existing pool
        while (pool.Count > 0)
        {
            GameObject obj = pool.Pop();
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        currentCount = 0;
        activeCount = 0;

        // Pre-populate pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = CreateNew();
            pool.Push(obj);
        }
    }

    private GameObject CreateNew()
    {
        if (prefab == null)
        {
            Debug.LogError("LocalItemPool: Cannot create new object, prefab is null!");
            return null;
        }

        GameObject next = Instantiate(prefab, poolParent);
        next.name = $"{prefab.name}_pooled_{currentCount}";
        next.SetActive(false);
        currentCount++;
        return next;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        // Get from pool or create new
        if (pool.Count > 0)
        {
            obj = pool.Pop();
        }
        else
        {
            obj = CreateNew();
        }

        if (obj != null)
        {
            // Set position/rotation and activate
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            activeCount++;
        }

        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("LocalItemPool: Attempted to return null object to pool.");
            return;
        }

        // Don't return if pool is at max size (to prevent memory bloat)
        if (pool.Count >= maxPoolSize)
        {
            Destroy(obj);
            activeCount--;
            return;
        }

        // Deactivate and return to pool
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        pool.Push(obj);
        activeCount--;
    }

    public void Clear()
    {
        // Destroy all pooled objects
        while (pool.Count > 0)
        {
            GameObject obj = pool.Pop();
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        currentCount = 0;
        activeCount = 0;
    }

    private void OnDestroy()
    {
        if (singleton == this)
        {
            Clear();
            singleton = null;
        }
    }
}

