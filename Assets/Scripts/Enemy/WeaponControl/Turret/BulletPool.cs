using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 100;

    private Dictionary<string, float> projectileLifetimes = new Dictionary<string, float>();
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private int activeBullets = 0;
    public float bulletLifetime = 5f;

    // Timestamp-based expiry sweep replaces a per-bullet StartCoroutine to cut allocations.
    private struct ActiveBullet
    {
        public GameObject bullet;
        public float expireAt;
    }
    private readonly List<ActiveBullet> activeBulletList = new List<ActiveBullet>();

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
        // Create bullets in batches to avoid frame spikes
        int bulletsPerFrame = 10;
        int created = 0;
        
        while (created < poolSize)
        {
            int toCreate = Mathf.Min(bulletsPerFrame, poolSize - created);
            for (int i = 0; i < toCreate; i++)
            {
                CreateNewBullet();
                created++;
            }
            yield return null; // Wait one frame between batches
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
            if (!bullet.TryGetComponent<BulletDamage>(out _))
                bullet.AddComponent<BulletDamage>();
            bullet.tag = "Bullet";
            bullet.layer = LayerMask.NameToLayer("Bullet");
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }

    public GameObject GetBullet(string type)
    {
        GameObject bullet = null;
        if (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
            if (bullet != null)
            {
                bullet.SetActive(true);
                bullet.tag = "Bullet";
                bullet.layer = LayerMask.NameToLayer("Bullet");
                activeBullets++;
                TrackActiveBullet(bullet, GetLifetimeForType(type));
            }
        }
        else if (activeBullets < poolSize)
        {
            bullet = Instantiate(bulletPrefab);
            if (!bullet.TryGetComponent<BulletDamage>(out _))
                bullet.AddComponent<BulletDamage>();
            bullet.tag = "Bullet";
            bullet.layer = LayerMask.NameToLayer("Bullet");
            bullet.SetActive(true);
            activeBullets++;
            TrackActiveBullet(bullet, GetLifetimeForType(type));
        }
        return bullet;
    }

    private float GetLifetimeForType(string type)
    {
        if (type == "Turret") return bulletLifetime;
        if (projectileLifetimes.TryGetValue(type, out float lifetime)) return lifetime;
        return bulletLifetime;
    }

    private void TrackActiveBullet(GameObject bullet, float lifetime)
    {
        activeBulletList.Add(new ActiveBullet { bullet = bullet, expireAt = Time.time + lifetime });
    }

    void Update()
    {
        float now = Time.time;
        for (int i = activeBulletList.Count - 1; i >= 0; i--)
        {
            ActiveBullet entry = activeBulletList[i];
            if (entry.bullet == null || !entry.bullet.activeInHierarchy)
            {
                activeBulletList.RemoveAt(i);
                continue;
            }
            if (now >= entry.expireAt)
            {
                activeBulletList.RemoveAt(i);
                if (entry.bullet.CompareTag("Bullet"))
                    ReturnBullet(entry.bullet);
            }
        }
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet != null && bullet.CompareTag("Bullet"))
        {
            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            bullet.SetActive(false);
            if (bulletPool.Count < poolSize)
            {
                bulletPool.Enqueue(bullet);
            }
            else
            {
                Destroy(bullet);
            }
            activeBullets--;
        }
    }

}
