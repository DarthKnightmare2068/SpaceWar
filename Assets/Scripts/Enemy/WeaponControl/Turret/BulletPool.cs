using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 100;
    
    // Dictionary to store different lifetime values for different projectile types
    private Dictionary<string, float> projectileLifetimes = new Dictionary<string, float>();
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private int activeBullets = 0;
    public float bulletLifetime = 5f; // Set in Inspector for all turret bullets

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Populate the pool with inactive bullets
        for(int i = 0; i < poolSize; i++)
        {
            CreateNewBullet();
        }
    }

    // Method to register a projectile type with its lifetime
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
            bullet.tag = "Bullet"; // Only enemy bullets are pooled
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
                if (type == "Turret")
                {
                    StartCoroutine(DestroyBulletAfterLifetime(bullet, bulletLifetime));
                }
                else if (projectileLifetimes.ContainsKey(type))
                {
                    StartCoroutine(DestroyBulletAfterLifetime(bullet, projectileLifetimes[type]));
                }
                else
                {
                    // Always ensure a lifetime for all bullets
                    StartCoroutine(DestroyBulletAfterLifetime(bullet, bulletLifetime));
                }
            }
        }
        else if (activeBullets < poolSize)
        {
            bullet = Instantiate(bulletPrefab);
            bullet.tag = "Bullet";
            bullet.layer = LayerMask.NameToLayer("Bullet");
            bullet.SetActive(true);
            activeBullets++;
            if (projectileLifetimes.ContainsKey(type))
            {
                StartCoroutine(DestroyBulletAfterLifetime(bullet, projectileLifetimes[type]));
            }
            else
            {
                StartCoroutine(DestroyBulletAfterLifetime(bullet, bulletLifetime));
            }
        }
        else if (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
            if (bullet != null)
            {
                bullet.SetActive(true);
                bullet.tag = "Bullet";
                bullet.layer = LayerMask.NameToLayer("Bullet");
                if (projectileLifetimes.ContainsKey(type))
                {
                    StartCoroutine(DestroyBulletAfterLifetime(bullet, projectileLifetimes[type]));
                }
                else
                {
                    StartCoroutine(DestroyBulletAfterLifetime(bullet, bulletLifetime));
                }
            }
        }
        return bullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet != null && bullet.CompareTag("Bullet"))
        {
            // Disable trail if present
            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
            // Reset velocity
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
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

    private IEnumerator DestroyBulletAfterLifetime(GameObject bullet, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        if (bullet != null && bullet.activeInHierarchy && bullet.CompareTag("Bullet"))
        {
            Debug.Log($"Returning bullet {bullet.name} after {lifetime} seconds");
            ReturnBullet(bullet);
        }
    }
}
