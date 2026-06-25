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

    // Bolt: Intrusive linked list for O(1) active VFX management
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
        // Bolt: Optimized - traverse the full list to handle variable lifetimes and destroyed objects.
        // Using (object)cast to check CLR reference prevents stall if head is Unity-destroyed.
        while ((object)current != null)
        {
            PooledVFX next = current.next;

            if (current == null)
            {
                RemoveFromActiveList(current);
            }
            else if (now >= current.expireAt)
            {
                Return(null, current);
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

            // Bolt: Optimized - cache ParticleSystem during pre-warm
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
            pooled.sourcePool = queue;
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
        AddToActiveList(pooled);

        return pooled.gameObject;
    }

    private void AddToActiveList(PooledVFX pooled)
    {
        if (activeTail == null)
        {
            activeHead = activeTail = pooled;
            pooled.next = pooled.prev = null;
        }
        else
        {
            activeTail.next = pooled;
            pooled.prev = activeTail;
            pooled.next = null;
            activeTail = pooled;
        }
    }

    /// <summary>
    /// Manually return a VFX instance to its pool.
    /// </summary>
    public void Return(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        if (instance.TryGetComponent(out PooledVFX pooled))
        {
            Return(null, pooled);
        }
        else
        {
            // Fallback for objects without PooledVFX (shouldn't happen with current Get/Prewarm)
            instance.SetActive(false);
            Destroy(instance, 0.1f);
        }
    }

    /// <summary>
    /// Bolt: Optimized internal return that avoids GetComponent and uses cached pool reference.
    /// </summary>
    private void Return(GameObject prefab, PooledVFX pooled)
    {
        if (pooled == null || !pooled.gameObject.activeSelf) return;

        RemoveFromActiveList(pooled);

        pooled.gameObject.SetActive(false);

        if (pooled.sourcePool != null)
        {
            pooled.sourcePool.Enqueue(pooled);
        }
        else if (prefab != null)
        {
            // Fallback to dictionary lookup only if sourcePool is missing
            if (!pools.TryGetValue(prefab, out Queue<PooledVFX> queue))
            {
                queue = new Queue<PooledVFX>();
                pools[prefab] = queue;
            }
            queue.Enqueue(pooled);
        }
    }

    // Bolt: Internal to allow PooledVFX.OnDestroy to clean up the list
    internal void RemoveFromActiveList(PooledVFX pooled)
    {
        if (pooled.prev != null) pooled.prev.next = pooled.next;
        if (pooled.next != null) pooled.next.prev = pooled.prev;

        if (activeHead == pooled) activeHead = pooled.next;
        if (activeTail == pooled) activeTail = pooled.prev;

        pooled.next = pooled.prev = null;
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

    // Bolt: Optimized - cached pool reference and intrusive linked list for O(1) management
    [HideInInspector] public Queue<PooledVFX> sourcePool;
    [HideInInspector] public float expireAt;
    [HideInInspector] public PooledVFX next;
    [HideInInspector] public PooledVFX prev;

    private void OnDestroy()
    {
        // Reference check avoids Unity null-bypass during cleanup
        if (!ReferenceEquals(VFXPool.Instance, null))
        {
            VFXPool.Instance.RemoveFromActiveList(this);
        }
    }
}
