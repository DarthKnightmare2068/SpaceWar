using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 100;
    [Tooltip("How many bullets to instantiate per frame during pool warm-up. Lower = smoother startup, longer warm-up.")]
    [SerializeField] private int prewarmBatchSize = 4;
    [Tooltip("Per-frame time budget (ms) for pool warm-up. Stops creating once exceeded, even if batch isn't done.")]
    [SerializeField] private float prewarmBudgetMs = 2f;

    private Dictionary<string, float> projectileLifetimes = new Dictionary<string, float>();
    // Bolt: Optimized internal storage to use PooledBullet directly
    private Queue<PooledBullet> bulletPool = new Queue<PooledBullet>();
    private int activeBullets = 0;
    public float bulletLifetime = 5f;

    // Bolt: Intrusive linked list for O(1) active bullet management and early-exit Update
    private PooledBullet activeHead = null;
    private PooledBullet activeTail = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        // Spread pool initialization across frames to prevent startup lag
        StartCoroutine(InitializePoolAsync());
    }

    private IEnumerator InitializePoolAsync()
    {
        // Create bullets across multiple frames, capped by both a per-frame batch size
        // AND a wall-clock budget so we never blow a frame even if a single Instantiate
        // is unexpectedly expensive.
        int created = 0;
        int batch = Mathf.Max(1, prewarmBatchSize);
        float budgetSeconds = Mathf.Max(0.5f, prewarmBudgetMs) / 1000f;

        while (created < poolSize)
        {
            float frameStart = Time.realtimeSinceStartup;
            int toCreate = Mathf.Min(batch, poolSize - created);
            for (int i = 0; i < toCreate; i++)
            {
                CreateNewBullet();
                created++;
                if (Time.realtimeSinceStartup - frameStart > budgetSeconds) break;
            }
            yield return null;
        }
    }

    public void RegisterProjectileType(string type, float lifetime)
    {
        if (!projectileLifetimes.ContainsKey(type))
        {
            projectileLifetimes.Add(type, lifetime);
        }
        else
        {
            projectileLifetimes[type] = lifetime;
        }
    }

    private void CreateNewBullet()
    {
        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab);

            // Bolt: Optimized component caching
            var pooled = bullet.GetComponent<PooledBullet>();
            if (pooled == null) pooled = bullet.AddComponent<PooledBullet>();

            pooled.bulletDamage = bullet.GetComponent<BulletDamage>();
            if (pooled.bulletDamage == null)
                pooled.bulletDamage = bullet.AddComponent<BulletDamage>();

            pooled.rb = bullet.GetComponent<Rigidbody>();
            pooled.trail = bullet.GetComponent<TrailRenderer>();

            // Bolt: Cache GameObject and Transform
            pooled.cachedGameObject = bullet;
            pooled.cachedTransform = bullet.transform;

            bullet.tag = "Bullet";
            bullet.layer = LayerMask.NameToLayer("Bullet");
            bullet.SetActive(false);
            bulletPool.Enqueue(pooled);
        }
    }

    public GameObject GetBullet(string type)
    {
        PooledBullet pooled = null;
        if (bulletPool.Count > 0)
        {
            pooled = bulletPool.Dequeue();
        }
        else if (activeBullets < poolSize)
        {
            GameObject bullet = Instantiate(bulletPrefab);

            // Bolt: Optimized component caching
            pooled = bullet.GetComponent<PooledBullet>();
            if (pooled == null) pooled = bullet.AddComponent<PooledBullet>();

            pooled.bulletDamage = bullet.GetComponent<BulletDamage>();
            if (pooled.bulletDamage == null)
                pooled.bulletDamage = bullet.AddComponent<BulletDamage>();

            pooled.rb = bullet.GetComponent<Rigidbody>();
            pooled.trail = bullet.GetComponent<TrailRenderer>();

            pooled.cachedGameObject = bullet;
            pooled.cachedTransform = bullet.transform;

            bullet.tag = "Bullet";
            bullet.layer = LayerMask.NameToLayer("Bullet");
        }

        if (pooled != null)
        {
            pooled.cachedGameObject.SetActive(true);
            activeBullets++;
            TrackActiveBullet(pooled, GetLifetimeForType(type));
            return pooled.cachedGameObject;
        }

        return null;
    }

    private float GetLifetimeForType(string type)
    {
        if (type == "Turret") return bulletLifetime;
        if (projectileLifetimes.TryGetValue(type, out float lifetime)) return lifetime;
        return bulletLifetime;
    }

    private void TrackActiveBullet(PooledBullet pooled, float lifetime)
    {
        pooled.expireAt = Time.time + lifetime;
        pooled.isDeactivated = false;

        // Bolt: Add to end of chronological linked list
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

    void Update()
    {
        float now = Time.time;
        // Bolt: Optimized with chronological early-exit
        while (activeHead != null && now >= activeHead.expireAt)
        {
            ReturnBullet(activeHead);
        }
    }

    private void RemoveFromActiveList(PooledBullet pooled)
    {
        if (pooled.Prev != null) pooled.Prev.Next = pooled.Next;
        if (pooled.Next != null) pooled.Next.Prev = pooled.Prev;

        if (activeHead == pooled) activeHead = pooled.Next;
        if (activeTail == pooled) activeTail = pooled.Prev;

        pooled.Next = pooled.Prev = null;
        pooled.Prev = null;
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        if (bullet.TryGetComponent(out PooledBullet pooled))
        {
            ReturnBullet(pooled);
        }
        else
        {
            // Fallback for non-pooled bullets (should not happen with this setup)
            bullet.SetActive(false);
            Destroy(bullet);
        }
    }

    // Bolt: Optimized direct return
    public void ReturnBullet(PooledBullet pooled)
    {
        if (pooled == null || pooled.isDeactivated) return;

        pooled.isDeactivated = true;
        RemoveFromActiveList(pooled);

        if (pooled.trail != null) pooled.trail.Clear();
        if (pooled.rb != null)
        {
            pooled.rb.linearVelocity = Vector3.zero;
            pooled.rb.angularVelocity = Vector3.zero;
        }

        pooled.cachedGameObject.SetActive(false);

        if (bulletPool.Count < poolSize)
        {
            bulletPool.Enqueue(pooled);
        }
        else
        {
            Destroy(pooled.cachedGameObject);
        }
        activeBullets--;
    }
}

// Bolt: Optimized component cache and intrusive linked list support
public class PooledBullet : MonoBehaviour
{
    public Rigidbody rb;
    public TrailRenderer trail;
    public BulletDamage bulletDamage;

    [HideInInspector] public GameObject cachedGameObject;
    [HideInInspector] public Transform cachedTransform;

    [HideInInspector] public float expireAt;
    [HideInInspector] public bool isDeactivated;

    [HideInInspector] public PooledBullet Next;
    [HideInInspector] public PooledBullet Prev;
}
