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

    // Bolt: Intrusive linked lists for O(1) active projectile management
    private PooledProjectile bulletHead = null;
    private PooledProjectile bulletTail = null;
    private PooledProjectile missileHead = null;
    private PooledProjectile missileTail = null;

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
        // Bolt: Optimized - chronological order allows early exit for expired checks
        while (bulletHead != null && now >= bulletHead.expireAt)
        {
            ReturnBullet(bulletHead);
        }

        while (missileHead != null && now >= missileHead.expireAt)
        {
            ReturnMissile(missileHead);
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
        AddBulletToActiveList(pooled);

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
        AddMissileToActiveList(pooled);

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
            ReturnBullet(pooled);
        }
        else
        {
            Destroy(bulletObj);
        }
    }

    public void ReturnBullet(PooledProjectile pooled)
    {
        if (pooled == null || !pooled.isActive) return;

        RemoveBulletFromActiveList(pooled);
        ReturnToPool(pooled, bulletPool, bulletContainer);
    }

    public void ReturnMissile(GameObject missileObj)
    {
        if (missileObj == null) return;
        
        PooledProjectile pooled = missileObj.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            ReturnMissile(pooled);
        }
        else
        {
            Destroy(missileObj);
        }
    }

    public void ReturnMissile(PooledProjectile pooled)
    {
        if (pooled == null || !pooled.isActive) return;

        RemoveMissileFromActiveList(pooled);
        ReturnToPool(pooled, missilePool, missileContainer);
    }

    private void AddBulletToActiveList(PooledProjectile pooled)
    {
        if (bulletTail == null)
        {
            bulletHead = bulletTail = pooled;
            pooled.Next = pooled.Prev = null;
        }
        else
        {
            bulletTail.Next = pooled;
            pooled.Prev = bulletTail;
            pooled.Next = null;
            bulletTail = pooled;
        }
    }

    private void RemoveBulletFromActiveList(PooledProjectile pooled)
    {
        if (pooled.Prev != null) pooled.Prev.Next = pooled.Next;
        if (pooled.Next != null) pooled.Next.Prev = pooled.Prev;

        if (bulletHead == pooled) bulletHead = pooled.Next;
        if (bulletTail == pooled) bulletTail = pooled.Prev;

        pooled.Next = pooled.Prev = null;
    }

    private void AddMissileToActiveList(PooledProjectile pooled)
    {
        if (missileTail == null)
        {
            missileHead = missileTail = pooled;
            pooled.Next = pooled.Prev = null;
        }
        else
        {
            missileTail.Next = pooled;
            pooled.Prev = missileTail;
            pooled.Next = null;
            missileTail = pooled;
        }
    }

    private void RemoveMissileFromActiveList(PooledProjectile pooled)
    {
        if (pooled.Prev != null) pooled.Prev.Next = pooled.Next;
        if (pooled.Next != null) pooled.Next.Prev = pooled.Prev;

        if (missileHead == pooled) missileHead = pooled.Next;
        if (missileTail == pooled) missileTail = pooled.Prev;

        pooled.Next = pooled.Prev = null;
    }

    private void ReturnToPool(PooledProjectile projectile, Queue<PooledProjectile> pool, Transform container)
    {
        if (projectile == null) return;
        
        projectile.Deactivate();
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
        PooledProjectile current = bulletHead;
        while (current != null)
        {
            PooledProjectile next = current.Next;
            Destroy(current.gameObject);
            current = next;
        }
        bulletHead = bulletTail = null;

        current = missileHead;
        while (current != null)
        {
            PooledProjectile next = current.Next;
            Destroy(current.gameObject);
            current = next;
        }
        missileHead = missileTail = null;

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
    [HideInInspector] public bool isActive = false;
    [HideInInspector] public float expireAt;

    // Bolt: Intrusive linked list pointers for O(1) active projectile management
    [HideInInspector] public PooledProjectile Next;
    [HideInInspector] public PooledProjectile Prev;

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
        expireAt = Time.time + projectileLifetime;
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public bool IsExpired(float now)
    {
        return isActive && now >= expireAt;
    }

    public void ReturnToPool()
    {
        if (!isActive) return;
        
        if (PlayerProjectilePool.Instance != null)
        {
            // Bolt: Optimized - direct return calls avoid GetComponent and tag comparisons
            if (CachedMissileController != null)
            {
                PlayerProjectilePool.Instance.ReturnMissile(this);
            }
            else
            {
                PlayerProjectilePool.Instance.ReturnBullet(this);
            }
        }
        else
        {
            isActive = false;
            Destroy(gameObject);
        }
    }
}
