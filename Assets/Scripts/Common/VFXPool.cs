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

    // Bolt: Intrusive linked list for O(1) active VFX management and early-exit Update
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
        // Bolt: Optimized - Traverse the linked list and return expired effects.
        // We do not use chronological early-exit because different VFX have different lifetimes.
        PooledVFX current = activeHead;
        while ((object)current != null)
        {
            // Cache Next before potentially returning/recycling 'current'
            PooledVFX next = current.Next;

            if (current == null || now >= current.expireAt)
            {
                if (current != null)
                {
                    Return(current.sourcePrefab, current);
                }
                else
                {
                    // Destroyed externally, remove from list and continue
                    RemoveFromActiveList(current);
                }
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

            // Bolt: Optimized - cache components and properties during pre-warm
            var pooled = obj.AddComponent<PooledVFX>();
            pooled.ps = obj.GetComponent<ParticleSystem>();
            pooled.cachedGameObject = obj;
            pooled.cachedTransform = obj.transform;
            pooled.sourcePrefab = prefab;

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
        {
            pooled = queue.Dequeue();
        }

        if (pooled == null)
        {
            GameObject instance = Instantiate(prefab, position, rotation);
            pooled = instance.AddComponent<PooledVFX>();
            pooled.ps = instance.GetComponent<ParticleSystem>();
            pooled.cachedGameObject = instance;
            pooled.cachedTransform = instance.transform;
            pooled.sourcePrefab = prefab;
        }
        else
        {
            pooled.cachedTransform.SetPositionAndRotation(position, rotation);
        }

        pooled.isActive = true;
        pooled.cachedGameObject.SetActive(true);

        if (pooled.ps != null)
        {
            pooled.ps.Clear();
            pooled.ps.Play();
            if (lifetime <= 0f)
                lifetime = pooled.ps.main.duration + pooled.ps.main.startLifetime.constantMax;
        }

        pooled.expireAt = Time.time + lifetime;
        AddToActiveList(pooled);

        return pooled.cachedGameObject;
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
    /// Bolt: Optimized internal return that avoids GetComponent and dictionary lookups where possible.
    /// </summary>
    private void Return(GameObject prefab, PooledVFX pooled)
    {
        if ((object)pooled == null || !pooled.isActive) return;

        pooled.isActive = false;
        RemoveFromActiveList(pooled);

        if (pooled != null)
        {
            pooled.cachedGameObject.SetActive(false);

            if (prefab != null)
            {
                if (!pools.TryGetValue(prefab, out Queue<PooledVFX> queue))
                {
                    queue = new Queue<PooledVFX>();
                    pools[prefab] = queue;
                }
                queue.Enqueue(pooled);
            }
        }
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

    private void RemoveFromActiveList(PooledVFX pooled)
    {
        // To safely handle Unity-destroyed objects (where 'pooled == null' is true but
        // '(object)pooled == null' might be false if we have a stale reference),
        // we must be extremely careful.
        // If 'pooled' is a valid managed reference, we mend the chain.

        if ((object)pooled == null) return;

        PooledVFX prev = pooled.Prev;
        PooledVFX next = pooled.Next;

        if ((object)prev != null) prev.Next = next;
        if ((object)next != null) next.Prev = prev;

        if ((object)activeHead == (object)pooled) activeHead = next;
        if ((object)activeTail == (object)pooled) activeTail = prev;

        pooled.Next = null;
        pooled.Prev = null;
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
    [HideInInspector] public GameObject cachedGameObject;
    [HideInInspector] public Transform cachedTransform;
    [HideInInspector] public float expireAt;
    [HideInInspector] public bool isActive;
    [HideInInspector] public GameObject sourcePrefab;

    [HideInInspector] public PooledVFX Next;
    [HideInInspector] public PooledVFX Prev;

    private void OnDestroy()
    {
        // Safety: if the object is destroyed externally, we must ensure it doesn't leave
        // broken references in the active list.
        if (isActive && (object)VFXPool.Instance != null)
        {
            // Mend the chain if possible.
            if ((object)Prev != null) Prev.Next = Next;
            if ((object)Next != null) Next.Prev = Prev;

            // We can't safely update the pool's Head/Tail directly from here
            // without a reference to the pool instance's private fields,
            // but VFXPool.Update() uses '(object)current != null' and 'current == null'
            // checks to handle these "missing" nodes safely.

            Next = null;
            Prev = null;
        }
    }
}
