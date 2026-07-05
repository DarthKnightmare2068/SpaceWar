using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton pool for frequently-spawned VFX (explosions, death effects, etc.).
/// Eliminates per-spawn Instantiate/Destroy allocation overhead.
/// </summary>
public class VFXPool : MonoBehaviour
{
    public static VFXPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private float defaultLifetime = 2f;

    // Bolt: Optimized - store PooledVFX component to avoid per-spawn GetComponent lookups
    private readonly Dictionary<GameObject, Queue<PooledVFX>> pools = new Dictionary<GameObject, Queue<PooledVFX>>();

    // Bolt: Intrusive doubly linked list for O(1) removals and traversal
    private PooledVFX activeHead = null;
    private PooledVFX activeTail = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float now = Time.time;
        PooledVFX current = activeHead;

        // Bolt: Chronological early-exit is NOT valid here because VFX have variable lifetimes.
        // We must traverse the entire list to find all expired items.
        while ((object)current != null)
        {
            PooledVFX next = current.Next;
            if (now >= current.expireAt)
            {
                ReturnInternal(current);
            }
            current = next;
        }
    }

    /// <summary>
    /// Pre-warm a specific prefab with N instances ready to go.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;
        if (!pools.TryGetValue(prefab, out Queue<PooledVFX> queue))
        {
            queue = new Queue<PooledVFX>();
            pools[prefab] = queue;
        }
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, transform);

            // Bolt: Optimized - cache ParticleSystem and sourceQueue during pre-warm
            var pooled = obj.AddComponent<PooledVFX>();
            pooled.ps = obj.GetComponent<ParticleSystem>();
            pooled.sourceQueue = queue;

            obj.SetActive(false);
            queue.Enqueue(pooled);
        }
    }

    /// <summary>
    /// Get a VFX instance from the pool, auto-returning after lifetime seconds.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = -1f)
    {
        if (prefab == null) return null;
        if (lifetime < 0f) lifetime = defaultLifetime;

        if (!pools.TryGetValue(prefab, out Queue<PooledVFX> queue))
        {
            queue = new Queue<PooledVFX>();
            pools[prefab] = queue;
        }

        PooledVFX pooled = null;
        while (queue.Count > 0 && pooled == null)
            pooled = queue.Dequeue();

        if (pooled == null)
        {
            GameObject instance = Instantiate(prefab, position, rotation);
            pooled = instance.AddComponent<PooledVFX>();
            pooled.ps = instance.GetComponent<ParticleSystem>();
            pooled.sourceQueue = queue;
        }
        else
        {
            pooled.transform.SetPositionAndRotation(position, rotation);
        }

        pooled.gameObject.SetActive(true);

        if (pooled.ps != null)
        {
            pooled.ps.Clear();
            pooled.ps.Play();
            if (lifetime <= 0f)
                lifetime = pooled.ps.main.duration + pooled.ps.main.startLifetime.constantMax;
        }

        pooled.expireAt = Time.time + lifetime;
        pooled.isActive = true;

        // Add to active linked list
        if (activeTail == null)
        {
            activeHead = activeTail = pooled;
            pooled.Next = pooled.Prev = null;
        }
        else
        {
            activeTail.Next = pooled;
            pooled.Prev = activeTail;
            pooled.Next = null;
            activeTail = pooled;
        }

        return pooled.gameObject;
    }

    /// <summary>
    /// Manually return a VFX instance to its pool.
    /// </summary>
    public void Return(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        if (instance.TryGetComponent(out PooledVFX pooled))
        {
            ReturnInternal(pooled);
        }
        else
        {
            // Fallback for objects without PooledVFX
            instance.SetActive(false);
            Destroy(instance, 0.1f);
        }
    }

    /// <summary>
    /// Bolt: Optimized internal return that avoids GetComponent and Dictionary lookups.
    /// </summary>
    private void ReturnInternal(PooledVFX pooled)
    {
        if (pooled == null || !pooled.isActive) return;

        pooled.isActive = false;

        Unlink(pooled);

        pooled.gameObject.SetActive(false);

        if (pooled.sourceQueue != null)
        {
            pooled.sourceQueue.Enqueue(pooled);
        }
        else
        {
            Destroy(pooled.gameObject);
        }
    }

    /// <summary>
    /// Convenience overload that uses defaultLifetime and identity rotation.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position)
    {
        return Get(prefab, position, Quaternion.identity, defaultLifetime);
    }

    private void Unlink(PooledVFX pooled)
    {
        if (pooled.Prev != null) pooled.Prev.Next = pooled.Next;
        if (pooled.Next != null) pooled.Next.Prev = pooled.Prev;

        if (activeHead == pooled) activeHead = pooled.Next;
        if (activeTail == pooled) activeTail = pooled.Prev;

        pooled.Next = pooled.Prev = null;
    }

    // Called by PooledVFX.OnDestroy to ensure list integrity
    internal void UnlinkOnDestroy(PooledVFX pooled)
    {
        Unlink(pooled);
    }
}

/// <summary>
/// Bolt: Optimized component cache for pooled VFX objects with intrusive linked list support.
/// </summary>
public class PooledVFX : MonoBehaviour
{
    public ParticleSystem ps;
    [HideInInspector] public PooledVFX Next;
    [HideInInspector] public PooledVFX Prev;
    [HideInInspector] public float expireAt;
    [HideInInspector] public bool isActive;
    [HideInInspector] public Queue<PooledVFX> sourceQueue;

    private void OnDestroy()
    {
        if (VFXPool.Instance != null)
        {
            VFXPool.Instance.UnlinkOnDestroy(this);
        }
    }
}
