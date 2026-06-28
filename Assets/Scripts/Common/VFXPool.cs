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

    // Bolt: Intrusive doubly linked list for O(1) removals.
    // Unlike bullets which have identical lifetimes, VFX lifetimes vary wildly,
    // so we must still traverse the full active list in Update, but removals are now O(1).
    internal PooledVFX activeHead = null;
    internal PooledVFX activeTail = null;

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

        // Use (object) cast to traverse even if Unity-side object is destroyed,
        // ensuring we can clean up the list using C# fields.
        while ((object)current != null)
        {
            PooledVFX next = current.Next;

            if (current == null) // Unity-destroyed
            {
                RemoveFromActiveList(current);
            }
            else if (now >= current.expireAt)
            {
                Return(current);
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
            pooled.isActive = false;

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
            instance.transform.SetParent(transform);
            pooled = instance.AddComponent<PooledVFX>();
            pooled.ps = instance.GetComponent<ParticleSystem>();
            pooled.sourcePool = queue;
        }
        else
        {
            pooled.transform.SetPositionAndRotation(position, rotation);
        }

        pooled.expireAt = Time.time + lifetime;
        pooled.isActive = true;
        pooled.gameObject.SetActive(true);

        if (pooled.ps != null)
        {
            pooled.ps.Clear();
            pooled.ps.Play();
            if (lifetime <= 0f)
                pooled.expireAt = Time.time + pooled.ps.main.duration + pooled.ps.main.startLifetime.constantMax;
        }

        AddToActiveList(pooled);

        return pooled.gameObject;
    }

    private void AddToActiveList(PooledVFX pooled)
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

    internal void RemoveFromActiveList(PooledVFX pooled)
    {
        // Use (object) to bypass Unity's null check for Next/Prev C# fields
        if ((object)pooled.Prev != null) pooled.Prev.Next = pooled.Next;
        if ((object)pooled.Next != null) pooled.Next.Prev = pooled.Prev;

        if ((object)activeHead == (object)pooled) activeHead = pooled.Next;
        if ((object)activeTail == (object)pooled) activeTail = pooled.Prev;

        pooled.Next = null;
        pooled.Prev = null;
    }

    /// <summary>
    /// Manually return a VFX instance to its pool.
    /// </summary>
    public void Return(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        if (instance.TryGetComponent(out PooledVFX pooled))
        {
            Return(pooled);
        }
        else
        {
            // Fallback for objects without PooledVFX (shouldn't happen with current Get/Prewarm)
            instance.SetActive(false);
            Destroy(instance, 0.1f);
        }
    }

    /// <summary>
    /// Bolt: Optimized internal return that uses direct source pool reference and O(1) list removal.
    /// </summary>
    private void Return(PooledVFX pooled)
    {
        if (pooled == null || !pooled.isActive) return;

        RemoveFromActiveList(pooled);
        pooled.isActive = false;

        // Final safety: OnDestroy might have been called, making pooled.gameObject null
        if (pooled != null)
        {
            pooled.gameObject.SetActive(false);

            if (pooled.sourcePool != null)
            {
                pooled.sourcePool.Enqueue(pooled);
            }
            else
            {
                Destroy(pooled.gameObject);
            }
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
/// Bolt: Optimized component cache and intrusive linked list support for pooled VFX objects.
/// </summary>
public class PooledVFX : MonoBehaviour
{
    public ParticleSystem ps;

    [HideInInspector] public float expireAt;
    [HideInInspector] public Queue<PooledVFX> sourcePool;
    [HideInInspector] public bool isActive;

    [HideInInspector] public PooledVFX Next;
    [HideInInspector] public PooledVFX Prev;

    private void OnDestroy()
    {
        // Safety: If the object is destroyed externally, ensure the linked list remains valid.
        if ((object)Next != null) Next.Prev = Prev;
        if ((object)Prev != null) Prev.Next = Next;

        if (VFXPool.Instance != null)
        {
            if ((object)VFXPool.Instance.activeHead == (object)this) VFXPool.Instance.activeHead = Next;
            if ((object)VFXPool.Instance.activeTail == (object)this) VFXPool.Instance.activeTail = Prev;
        }
    }
}
