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

    // Bolt: Intrusive doubly linked list for O(1) active management
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
        while (current != null)
        {
            // Cache next before potentially returning/unlinking current
            PooledVFX next = current.Next;

            // Use Object.ReferenceEquals to check for real null, but allow Unity's
            // null-check for destroyed objects.
            if (current == null)
            {
                // This shouldn't happen often due to OnDestroy unlinking, but if it does,
                // we must clean up the list to avoid a permanent stall.
                Unlink(current);
            }
            else if (now >= current.ExpireAt)
            {
                Return(null, current);
            }

            current = next;
        }
    }

    /// <summary>
    /// Bolt: O(1) removal from the active linked list.
    /// </summary>
    public void Unlink(PooledVFX vfx)
    {
        if (vfx.Prev != null) vfx.Prev.Next = vfx.Next;
        if (vfx.Next != null) vfx.Next.Prev = vfx.Prev;

        if (activeHead == vfx) activeHead = vfx.Next;
        if (activeTail == vfx) activeTail = vfx.Prev;

        vfx.Next = vfx.Prev = null;
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

            // Bolt: Optimized - cache ParticleSystem during pre-warm
            var pooled = obj.AddComponent<PooledVFX>();
            pooled.ps = obj.GetComponent<ParticleSystem>();

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

        pooled.SourceQueue = queue;
        pooled.gameObject.SetActive(true);

        if (pooled.ps != null)
        {
            pooled.ps.Clear();
            pooled.ps.Play();
            if (lifetime <= 0f)
                lifetime = pooled.ps.main.duration + pooled.ps.main.startLifetime.constantMax;
        }

        pooled.ExpireAt = Time.time + lifetime;

        // Bolt: Link to active list
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
            Return(prefab, pooled);
        }
        else
        {
            // Fallback for objects without PooledVFX (shouldn't happen with current Get/Prewarm)
            instance.SetActive(false);
            Destroy(instance, 0.1f);
        }
    }

    /// <summary>
    /// Bolt: Optimized internal return that avoids GetComponent and uses cached SourceQueue.
    /// </summary>
    private void Return(GameObject prefab, PooledVFX pooled)
    {
        if (pooled == null) return;

        // Bolt: Always unlink even if already inactive to ensure list integrity
        Unlink(pooled);

        if (!pooled.gameObject.activeSelf) return;

        pooled.gameObject.SetActive(false);

        var queue = pooled.SourceQueue;
        if (queue == null && prefab != null)
        {
            if (!pools.TryGetValue(prefab, out queue))
            {
                queue = new Queue<PooledVFX>();
                pools[prefab] = queue;
            }
            pooled.SourceQueue = queue;
        }

        if (queue != null)
        {
            queue.Enqueue(pooled);
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
}

/// <summary>
/// Bolt: Optimized component cache for pooled VFX objects.
/// </summary>
public class PooledVFX : MonoBehaviour
{
    public ParticleSystem ps;
    [System.NonSerialized] public float ExpireAt;
    [System.NonSerialized] public Queue<PooledVFX> SourceQueue;
    [System.NonSerialized] public PooledVFX Next;
    [System.NonSerialized] public PooledVFX Prev;

    private void OnDestroy()
    {
        if (VFXPool.Instance != null)
        {
            VFXPool.Instance.Unlink(this);
        }
    }
}
