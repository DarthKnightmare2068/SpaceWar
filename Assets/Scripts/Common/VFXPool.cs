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

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly List<ActiveVFX> activeList = new List<ActiveVFX>();

    private struct ActiveVFX
    {
        public GameObject instance;
        public GameObject prefab;
        public float expireAt;
    }

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
        for (int i = activeList.Count - 1; i >= 0; i--)
        {
            ActiveVFX entry = activeList[i];
            if (entry.instance == null)
            {
                activeList.RemoveAt(i);
                continue;
            }
            if (now >= entry.expireAt)
            {
                activeList.RemoveAt(i);
                Return(entry.prefab, entry.instance);
            }
        }
    }

    /// <summary>
    /// Pre-warm a specific prefab with N instances ready to go.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;
        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
    }

    /// <summary>
    /// Get a VFX instance from the pool, auto-returning after lifetime seconds.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = -1f)
    {
        if (prefab == null) return null;
        if (lifetime < 0f) lifetime = defaultLifetime;

        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        GameObject instance = null;
        while (queue.Count > 0 && instance == null)
            instance = queue.Dequeue();

        if (instance == null)
            instance = Instantiate(prefab, position, rotation);
        else
            instance.transform.SetPositionAndRotation(position, rotation);

        instance.SetActive(true);

        // Reset particle systems if present
        var ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
            if (lifetime <= 0f)
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
        }

        activeList.Add(new ActiveVFX
        {
            instance = instance,
            prefab = prefab,
            expireAt = Time.time + lifetime
        });

        return instance;
    }

    /// <summary>
    /// Manually return a VFX instance to its pool.
    /// </summary>
    public void Return(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;
        instance.SetActive(false);

        if (prefab != null)
        {
            if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pools[prefab] = queue;
            }
            queue.Enqueue(instance);
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
