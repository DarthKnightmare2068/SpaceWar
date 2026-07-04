using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MachineGunControl : MonoBehaviour
{
    [Header("References")]
    public PlayerWeaponManager weaponManager;
    public List<Transform> machineGunSpawnPoints = new List<Transform>();
    
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 2000f;
    public float bulletLifetime = 5f;
    
    [Header("Damage Settings")]
    public float damage = 10f;
    private PlaneStats playerPlane;
    
    private float nextFireTime = 0f;
    private int currentSpawnIndex = 0;
    private bool poolInitialized = false;

    private void Start()
    {
        if (bulletPrefab == null)
        {
            return;
        }
        
        if (weaponManager == null)
        {
            weaponManager = GetComponent<PlayerWeaponManager>();
        }
        if (weaponManager == null)
        {
            weaponManager = GetComponentInParent<PlayerWeaponManager>();
        }
        
        playerPlane = Resolver.Find<PlaneStats>(this);
        
        InitializeBulletPool();
    }

    private void InitializeBulletPool()
    {
        if (poolInitialized) return;
        
        if (PlayerProjectilePool.Instance != null && bulletPrefab != null)
        {
            PlayerProjectilePool.Instance.PrewarmBulletPool(bulletPrefab, 50);
            poolInitialized = true;
        }
        else if (BulletPool.Instance != null)
        {
            BulletPool.Instance.RegisterProjectileType("Bullet", bulletLifetime);
            poolInitialized = true;
        }
    }

    private void Update()
    {
        if (weaponManager == null) return;

        if (!poolInitialized)
        {
            InitializeBulletPool();
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            // Bolt: Optimized - check range only when attempting to fire
            if (weaponManager.IsTargetInRange(weaponManager.machineGunFireRange) && !weaponManager.isReloading)
            {
                if (weaponManager.CanFireBullet())
                {
                    Fire();
                    nextFireTime = Time.time + weaponManager.machineGunFireRate;
                }
                else if (!weaponManager.isReloading)
                {
                    StartCoroutine(weaponManager.Reload());
                }
            }
        }
    }

    void Fire()
    {
        if (machineGunSpawnPoints.Count == 0 || bulletPrefab == null || weaponManager == null) return;
        if (!weaponManager.CanFireBullet()) return;

        Ray ray = weaponManager.GetCurrentTargetRay();
        RaycastHit hit;
        Transform currentSpawnPoint = machineGunSpawnPoints[currentSpawnIndex];
        Vector3 bulletDirection;

        if (Physics.Raycast(ray, out hit, weaponManager.machineGunFireRange, weaponManager.GetTargetableLayers()))
        {
            float finalDamage = damage + (playerPlane != null ? playerPlane.attackPoint : 0);
            DamageHelper.TryDealDamage(hit, finalDamage, Color.yellow);
            bulletDirection = (hit.point - currentSpawnPoint.position).normalized;
        }
        else
        {
            bulletDirection = ray.direction;
        }

        SpawnBullet(currentSpawnPoint, bulletDirection);
        weaponManager.UseBullet();
        currentSpawnIndex = (currentSpawnIndex + 1) % machineGunSpawnPoints.Count;
        PlayFireSound();
    }

    private void PlayFireSound()
    {
        if (AudioSetting.Instance != null && AudioSetting.Instance.machineGunSound != null)
        {
            AudioSetting.Instance.PlayMachineGunSound();
        }
    }

    void SpawnBullet(Transform spawnPoint, Vector3 direction)
    {
        if (bulletPrefab == null) return;
        
        if (direction == Vector3.zero)
        {
            direction = spawnPoint.forward;
        }

        PooledProjectile bullet = null;
        
        if (PlayerProjectilePool.Instance != null)
        {
            bullet = PlayerProjectilePool.Instance.GetBullet(spawnPoint.position, Quaternion.LookRotation(direction), bulletLifetime);
        }
        
        if (bullet == null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.LookRotation(direction));
            bullet = bulletObj.GetComponent<PooledProjectile>();
            if (bullet == null) bullet = bulletObj.AddComponent<PooledProjectile>();
            bullet.CacheComponents();
            bulletObj.layer = LayerMask.NameToLayer("Player");
            bulletObj.tag = "PlayerWeapon";
            Destroy(bulletObj, bulletLifetime);
        }
        
        if (bullet != null)
        {
            // Bolt: Optimized - use cached Rigidbody and skip redundant tag/layer assignments
            if (bullet.CachedRigidbody != null)
            {
                bullet.CachedRigidbody.linearVelocity = direction * bulletSpeed;
            }
        }
    }
}
