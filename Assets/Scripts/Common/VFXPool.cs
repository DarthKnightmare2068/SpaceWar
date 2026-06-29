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

    // Bolt: Intrusive linked list for O(1) active list management
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

    void Update()
    {
        float now = Time.time;
        PooledVFX current = activeHead;

        // Traverse the entire active list. Unlike bullets, VFX have variable lifetimes,
        // so we cannot early-exit. However, removals are now O(1).
        while (current != null)
        {
            PooledVFX next = current.Next;

            // Use (object) check for Unity null-destruction safety
            if ((object)current == null)
            {
                current = next;
                continue;
            }

            if (now >= current.expireAt)
            {
                ReturnToPool(current);
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

            // Bolt: Optimized - cache ParticleSystem and source pool during pre-warm
            var pooled = obj.AddComponent<PooledVFX>();
            pooled.ps = obj.GetComponent<ParticleSystem>();
            pooled.sourcePool = queue;

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
        }
        else
        {
            pooled.transform.SetPositionAndRotation(position, rotation);
        }

        pooled.sourcePool = queue;
        pooled.expireAt = Time.time + lifetime;
        pooled.gameObject.SetActive(true);

        if (pooled.ps != null)
        {
            pooled.ps.Clear();
            pooled.ps.Play();
            if (lifetime <= 0f)
                pooled.expireAt = Time.time + pooled.ps.main.duration + pooled.ps.main.startLifetime.constantMax;
        }

        InternalAddToActiveList(pooled);

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
            ReturnToPool(pooled);
        }
        else
        {
            // Fallback for objects without PooledVFX (shouldn't happen with current Get/Prewarm)
            instance.SetActive(false);
            Destroy(instance, 0.1f);
        }
    }

    /// <summary>
    /// Bolt: Optimized internal return that uses the cached source pool reference (O(1)).
    /// </summary>
    private void ReturnToPool(PooledVFX pooled)
    {
        if (pooled == null) return;

        InternalUnlink(pooled);
        pooled.gameObject.SetActive(false);

        if (pooled.sourcePool != null)
        {
            pooled.sourcePool.Enqueue(pooled);
        }
        else
        {
            // This should only happen if the object was created without a pool
            Destroy(pooled.gameObject);
        }
    }

    private void InternalAddToActiveList(PooledVFX pooled)
    {
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
    }

    internal void InternalUnlink(PooledVFX pooled)
    {
        if (pooled.Prev != null) pooled.Prev.Next = pooled.Next;
        if (pooled.Next != null) pooled.Next.Prev = pooled.Prev;

        if (activeHead == pooled) activeHead = pooled.Next;
        if (activeTail == pooled) activeTail = pooled.Prev;

        pooled.Next = pooled.Prev = null;
    }

    /// <summary>
    /// Convenience overload that uses defaultLifetime and identity rotation.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position)
    {
        return Get(prefab, position, Quaternion.identity, defaultLifetime);
    }
}

/// <summary>
/// Bolt: Optimized component cache for pooled VFX objects with intrusive linked-list support.
/// </summary>
public class PooledVFX : MonoBehaviour
{
    public ParticleSystem ps;
    [HideInInspector] public float expireAt;
    [HideInInspector] public Queue<PooledVFX> sourcePool;

    // Bolt: Intrusive linked list pointers for O(1) active list management
    [HideInInspector] public PooledVFX Next;
    [HideInInspector] public PooledVFX Prev;

    private void OnDestroy()
    {
        // Safety: If the object is destroyed externally, ensure the linked list is mended
        if (VFXPool.Instance != null)
        {
            VFXPool.Instance.InternalUnlink(this);
        }
        else
        {
            // If the pool is already gone, just mend the neighbors
            if (Prev != null) Prev.Next = Next;
            if (Next != null) Next.Prev = Prev;
        }
    }
}
