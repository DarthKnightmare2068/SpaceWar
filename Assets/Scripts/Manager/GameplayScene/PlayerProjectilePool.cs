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
        for (int i = activeList.Count - 1; i >= 0; i--)
        {
            PooledProjectile projectile = activeList[i];
            if (projectile == null || projectile.gameObject == null)
            {
                activeList.RemoveAt(i);
                continue;
            }

            if (projectile.IsExpired())
            {
                ReturnToPool(projectile.gameObject, pool, projectile.transform.parent);
                activeList.RemoveAt(i);
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
        
        PooledProjectile pooled = bullet.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            pooled = bullet.AddComponent<PooledProjectile>();
        }
        
        return bullet;
    }

    private GameObject CreatePooledMissile()
    {
        if (missilePrefab == null) return null;
        
        GameObject missile = Instantiate(missilePrefab, missileContainer);
        
        PooledProjectile pooled = missile.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            pooled = missile.AddComponent<PooledProjectile>();
        }
        
        return missile;
    }

    public GameObject GetBullet(Vector3 position, Quaternion rotation, float lifetime = 5f)
    {
        GameObject bullet;
        
        if (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
        }
        else
        {
            bullet = CreatePooledBullet();
            if (bullet == null)
            {
                return null;
            }
        }

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        PooledProjectile pooled = bullet.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            pooled.Activate(lifetime);
            activeBullets.Add(pooled);
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        return bullet;
    }

    public GameObject GetMissile(Vector3 position, Quaternion rotation, float lifetime = 10f)
    {
        GameObject missile;
        
        if (missilePool.Count > 0)
        {
            missile = missilePool.Dequeue();
        }
        else
        {
            missile = CreatePooledMissile();
            if (missile == null)
            {
                return null;
            }
        }

        missile.transform.position = position;
        missile.transform.rotation = rotation;
        missile.SetActive(true);

        PooledProjectile pooled = missile.GetComponent<PooledProjectile>();
        if (pooled != null)
        {
            pooled.Activate(lifetime);
            activeMissiles.Add(pooled);
        }

        Rigidbody rb = missile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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
