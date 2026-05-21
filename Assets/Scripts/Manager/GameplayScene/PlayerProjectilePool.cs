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

    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private Queue<GameObject> missilePool = new Queue<GameObject>();
    
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
        ReturnExpiredProjectiles(activeBullets, bulletPool);
        ReturnExpiredProjectiles(activeMissiles, missilePool);
    }

    private void ReturnExpiredProjectiles(List<PooledProjectile> activeList, Queue<GameObject> pool)
    {
        // Bolt: Optimized - projectiles are added in chronological order, so we can early exit.
        // We always check the head of the list.
        while (activeList.Count > 0)
        {
            PooledProjectile projectile = activeList[0];
            if (projectile == null)
            {
                activeList.RemoveAt(0);
                continue;
            }

            if (projectile.IsExpired())
            {
                ReturnToPool(projectile.gameObject, pool, projectile.transform.parent);
                activeList.RemoveAt(0);
            }
            else
            {
                // Since they are ordered by spawn time, if this one hasn't expired,
                // none of the subsequent ones have either.
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
            GameObject bullet = CreatePooledBullet();
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }

    public void PrewarmMissilePool(GameObject prefab, int count = -1)
    {
        if (prefab == null) return;
        missilePrefab = prefab;
        
        int poolSize = count > 0 ? count : initialMissilePoolSize;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject missile = CreatePooledMissile();
            missile.SetActive(false);
            missilePool.Enqueue(missile);
        }
    }

    private GameObject CreatePooledBullet()
    {
        if (bulletPrefab == null) return null;
        
        GameObject bullet = Instantiate(bulletPrefab, bulletContainer);
        // Bolt: Pre-set tag and layer to avoid per-frame assignment in firing path
        bullet.tag = "PlayerWeapon";
        bullet.layer = LayerMask.NameToLayer("Player");
        
        PooledProjectile pooled = bullet.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            pooled = bullet.AddComponent<PooledProjectile>();
        }
        pooled.Initialize();
        
        return bullet;
    }

    private GameObject CreatePooledMissile()
    {
        if (missilePrefab == null) return null;
        
        GameObject missile = Instantiate(missilePrefab, missileContainer);
        // Bolt: Pre-set tag and layer
        missile.tag = "PlayerWeapon";
        missile.layer = LayerMask.NameToLayer("Player");
        
        PooledProjectile pooled = missile.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            pooled = missile.AddComponent<PooledProjectile>();
        }
        pooled.Initialize();
        
        return missile;
    }

    public GameObject GetBullet(Vector3 position, Quaternion rotation, float lifetime = 5f)
    {
        GameObject bullet;
        PooledProjectile pooled = null;
        
        if (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
            bullet.TryGetComponent(out pooled);
        }
        else
        {
            bullet = CreatePooledBullet();
            if (bullet == null) return null;
            bullet.TryGetComponent(out pooled);
        }

        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetActive(true);

        if (pooled != null)
        {
            pooled.Activate(lifetime);
            activeBullets.Add(pooled);
            // Bolt: Optimized - use cached Rigidbody
            if (pooled.Rb != null)
            {
                pooled.Rb.linearVelocity = Vector3.zero;
                pooled.Rb.angularVelocity = Vector3.zero;
            }
        }

        return bullet;
    }

    public GameObject GetMissile(Vector3 position, Quaternion rotation, float lifetime = 10f)
    {
        GameObject missile;
        PooledProjectile pooled = null;
        
        if (missilePool.Count > 0)
        {
            missile = missilePool.Dequeue();
            missile.TryGetComponent(out pooled);
        }
        else
        {
            missile = CreatePooledMissile();
            if (missile == null) return null;
            missile.TryGetComponent(out pooled);
        }

        missile.transform.SetPositionAndRotation(position, rotation);
        missile.SetActive(true);

        if (pooled != null)
        {
            pooled.Activate(lifetime);
            activeMissiles.Add(pooled);
            // Bolt: Optimized - use cached Rigidbody
            if (pooled.Rb != null)
            {
                pooled.Rb.linearVelocity = Vector3.zero;
                pooled.Rb.angularVelocity = Vector3.zero;
            }
        }

        return missile;
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;
        
        PooledProjectile pooled = bullet.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            activeBullets.Remove(pooled);
        }
        
        ReturnToPool(bullet, bulletPool, bulletContainer);
    }

    public void ReturnMissile(GameObject missile)
    {
        if (missile == null) return;
        
        PooledProjectile pooled = missile.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            activeMissiles.Remove(pooled);
        }
        
        ReturnToPool(missile, missilePool, missileContainer);
    }

    private void ReturnToPool(GameObject obj, Queue<GameObject> pool, Transform container)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.SetParent(container);
        
        // Bolt: Optimized - use cached component if available
        if (obj.TryGetComponent<PooledProjectile>(out var pooled) && pooled.Rb != null)
        {
            pooled.Rb.linearVelocity = Vector3.zero;
            pooled.Rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        pool.Enqueue(obj);
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
            GameObject bullet = bulletPool.Dequeue();
            if (bullet != null)
            {
                Destroy(bullet);
            }
        }

        while (missilePool.Count > 0)
        {
            GameObject missile = missilePool.Dequeue();
            if (missile != null)
            {
                Destroy(missile);
            }
        }
    }
}

public class PooledProjectile : MonoBehaviour
{
    private float lifetime;
    private float spawnTime;
    private bool isActive = false;

    // Bolt: Optimized component caching
    public Rigidbody Rb { get; private set; }
    public Transform Trans { get; private set; }

    public void Initialize()
    {
        Rb = GetComponent<Rigidbody>();
        Trans = transform;
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

    public bool IsExpired()
    {
        if (!isActive) return false;
        return Time.time - spawnTime >= lifetime;
    }

    public void ReturnToPool()
    {
        isActive = false;
        
        if (PlayerProjectilePool.Instance != null)
        {
            // Bolt: Tag is pre-set during instantiation in the pool
            if (gameObject.CompareTag("PlayerWeapon") || gameObject.CompareTag("Bullet"))
            {
                PlayerProjectilePool.Instance.ReturnBullet(gameObject);
            }
            else if (gameObject.CompareTag("Missile"))
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
