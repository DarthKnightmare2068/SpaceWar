using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectilePool : MonoBehaviour
{
    public static PlayerProjectilePool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int initialBulletPoolSize = 50;
    [SerializeField] private int initialMissilePoolSize = 20;

    [Header("Prefab References")]
    [Tooltip("Set these in inspector or they will be found from MachineGunControl/MissileLaunch")]
    public GameObject bulletPrefab;
    public GameObject missilePrefab;

    private Queue<PooledProjectile> bulletPool = new Queue<PooledProjectile>();
    private Queue<PooledProjectile> missilePool = new Queue<PooledProjectile>();
    
    private List<PooledProjectile> activeBullets = new List<PooledProjectile>();
    private List<PooledProjectile> activeMissiles = new List<PooledProjectile>();

    private Transform bulletContainer;
    private Transform missileContainer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        bulletContainer = new GameObject("BulletPool").transform;
        bulletContainer.SetParent(transform);
        
        missileContainer = new GameObject("MissilePool").transform;
        missileContainer.SetParent(transform);
    }

    void Update()
    {
        float now = Time.time;
        // Bolt: Optimized - pass current time to avoid multiple Time.time calls per frame
        ReturnExpiredProjectiles(activeBullets, bulletPool, now);
        ReturnExpiredProjectiles(activeMissiles, missilePool, now);
    }

    private void ReturnExpiredProjectiles(List<PooledProjectile> activeList, Queue<PooledProjectile> pool, float now)
    {
        // Bolt: Optimized - chronological order allows early exit for expired checks
        for (int i = 0; i < activeList.Count; i++)
        {
            PooledProjectile projectile = activeList[i];
            if (projectile == null || projectile.gameObject == null)
            {
                activeList.RemoveAt(i);
                i--;
                continue;
            }

            if (projectile.IsExpired(now))
            {
                ReturnToPool(projectile, pool, projectile.transform.parent);
                activeList.RemoveAt(i);
                i--;
            }
            else
            {
                // Since projectiles are added in chronological order, if this one isn't expired,
                // the ones after it won't be either.
                break;
            }
        }
    }

    public void PrewarmBulletPool(GameObject prefab, int count = -1)
    {
        if (prefab == null) return;
        bulletPrefab = prefab;
        
        int poolSize = count > 0 ? count : initialBulletPoolSize;
        for (int i = 0; i < poolSize; i++)
        {
            PooledProjectile bullet = CreatePooledBullet();
            if (bullet != null)
            {
                bullet.gameObject.SetActive(false);
                bulletPool.Enqueue(bullet);
            }
        }
    }

    public void PrewarmMissilePool(GameObject prefab, int count = -1)
    {
        if (prefab == null) return;
        missilePrefab = prefab;
        
        int poolSize = count > 0 ? count : initialMissilePoolSize;
        for (int i = 0; i < poolSize; i++)
        {
            PooledProjectile missile = CreatePooledMissile();
            if (missile != null)
            {
                missile.gameObject.SetActive(false);
                missilePool.Enqueue(missile);
            }
        }
    }

    private PooledProjectile CreatePooledBullet()
    {
        if (bulletPrefab == null) return null;
        
        GameObject bulletObj = Instantiate(bulletPrefab, bulletContainer);
        
        PooledProjectile pooled = bulletObj.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            pooled = bulletObj.AddComponent<PooledProjectile>();
        }
        
        // Bolt: Optimized - cache properties and components at creation time
        bulletObj.layer = LayerMask.NameToLayer("Player");
        bulletObj.tag = "PlayerWeapon";
        pooled.CacheComponents();

        return pooled;
    }

    private PooledProjectile CreatePooledMissile()
    {
        if (missilePrefab == null) return null;
        
        GameObject missileObj = Instantiate(missilePrefab, missileContainer);
        
        PooledProjectile pooled = missileObj.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            pooled = missileObj.AddComponent<PooledProjectile>();
        }
        
        // Bolt: Optimized - cache properties and components at creation time
        missileObj.layer = LayerMask.NameToLayer("Player");
        missileObj.tag = "PlayerWeapon";
        pooled.CacheComponents();

        return pooled;
    }

    public PooledProjectile GetBullet(Vector3 position, Quaternion rotation, float lifetime = 5f)
    {
        PooledProjectile pooled;
        
        if (bulletPool.Count > 0)
        {
            pooled = bulletPool.Dequeue();
        }
        else
        {
            pooled = CreatePooledBullet();
            if (pooled == null)
            {
                return null;
            }
        }

        pooled.CachedTransform.SetPositionAndRotation(position, rotation);
        pooled.gameObject.SetActive(true);

        pooled.Activate(lifetime);
        activeBullets.Add(pooled);

        if (pooled.CachedRigidbody != null)
        {
            pooled.CachedRigidbody.linearVelocity = Vector3.zero;
            pooled.CachedRigidbody.angularVelocity = Vector3.zero;
        }

        return pooled;
    }

    public PooledProjectile GetMissile(Vector3 position, Quaternion rotation, float lifetime = 10f)
    {
        PooledProjectile pooled;
        
        if (missilePool.Count > 0)
        {
            pooled = missilePool.Dequeue();
        }
        else
        {
            pooled = CreatePooledMissile();
            if (pooled == null)
            {
                return null;
            }
        }

        pooled.CachedTransform.SetPositionAndRotation(position, rotation);
        pooled.gameObject.SetActive(true);

        pooled.Activate(lifetime);
        activeMissiles.Add(pooled);

        if (pooled.CachedRigidbody != null)
        {
            pooled.CachedRigidbody.linearVelocity = Vector3.zero;
            pooled.CachedRigidbody.angularVelocity = Vector3.zero;
        }

        return pooled;
    }

    public void ReturnBullet(GameObject bulletObj)
    {
        if (bulletObj == null) return;
        
        PooledProjectile pooled = bulletObj.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            activeBullets.Remove(pooled);
            ReturnToPool(pooled, bulletPool, bulletContainer);
        }
        else
        {
            Destroy(bulletObj);
        }
    }

    public void ReturnMissile(GameObject missileObj)
    {
        if (missileObj == null) return;
        
        PooledProjectile pooled = missileObj.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            activeMissiles.Remove(pooled);
            ReturnToPool(pooled, missilePool, missileContainer);
        }
        else
        {
            Destroy(missileObj);
        }
    }

    private void ReturnToPool(PooledProjectile projectile, Queue<PooledProjectile> pool, Transform container)
    {
        if (projectile == null) return;
        
        projectile.gameObject.SetActive(false);
        projectile.CachedTransform.SetParent(container);
        
        if (projectile.CachedRigidbody != null)
        {
            projectile.CachedRigidbody.linearVelocity = Vector3.zero;
            projectile.CachedRigidbody.angularVelocity = Vector3.zero;
        }
        
        pool.Enqueue(projectile);
    }

    public void ClearAllPools()
    {
        foreach (var projectile in activeBullets)
        {
            if (projectile != null && projectile.gameObject != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        activeBullets.Clear();

        foreach (var projectile in activeMissiles)
        {
            if (projectile != null && projectile.gameObject != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        activeMissiles.Clear();

        while (bulletPool.Count > 0)
        {
            PooledProjectile bullet = bulletPool.Dequeue();
            if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }
        }

        while (missilePool.Count > 0)
        {
            PooledProjectile missile = missilePool.Dequeue();
            if (missile != null)
            {
                Destroy(missile.gameObject);
            }
        }
    }
}

public class PooledProjectile : MonoBehaviour
{
    private float lifetime;
    private float spawnTime;
    private bool isActive = false;

    // Bolt: Optimized - cached components to avoid per-shot GetComponent calls
    public Rigidbody CachedRigidbody { get; private set; }
    public Transform CachedTransform { get; private set; }
    public MissileAutoLock CachedMissileLock { get; private set; }
    public MissileController CachedMissileController { get; private set; }

    public void CacheComponents()
    {
        CachedRigidbody = GetComponent<Rigidbody>();
        CachedTransform = transform;
        CachedMissileLock = GetComponent<MissileAutoLock>();
        CachedMissileController = GetComponent<MissileController>();
    }

    public void Activate(float projectileLifetime)
    {
        lifetime = projectileLifetime;
        spawnTime = Time.time;
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public bool IsExpired(float now)
    {
        if (!isActive) return false;
        return now - spawnTime >= lifetime;
    }

    public void ReturnToPool()
    {
        isActive = false;
        
        if (PlayerProjectilePool.Instance != null)
        {
            // Bolt: Optimized - check component state/type instead of potentially expensive tag string comparisons
            if (CachedMissileController != null)
            {
                PlayerProjectilePool.Instance.ReturnMissile(gameObject);
            }
            else
            {
                PlayerProjectilePool.Instance.ReturnBullet(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
