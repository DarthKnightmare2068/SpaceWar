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
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        }
        if (playerPlane == null)
        {
            playerPlane = GetComponent<PlaneStats>();
            if (playerPlane == null)
            {
                playerPlane = GetComponentInParent<PlaneStats>();
            }
        }
        if (playerPlane == null)
        {
            var foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                playerPlane = foundPlayer.GetComponent<PlaneStats>();
        }
        
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

        bool inFireRange = weaponManager.IsTargetInRange(weaponManager.machineGunFireRange);

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (inFireRange && !weaponManager.isReloading)
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
        if (machineGunSpawnPoints.Count == 0 || bulletPrefab == null || weaponManager == null)
        {
            return;
        }

        if (!weaponManager.CanFireBullet())
            return;

        Ray ray = weaponManager.GetCurrentTargetRay();
        RaycastHit hit;
        
        Transform currentSpawnPoint = machineGunSpawnPoints[currentSpawnIndex];
        Vector3 bulletDirection;
        LayerMask targetableLayers = weaponManager.GetTargetableLayers();
        
        if (Physics.Raycast(ray, out hit, weaponManager.machineGunFireRange, targetableLayers))
        {
            float hitDistance = hit.distance;
            
            if (hit.collider.CompareTag("Enemy"))
            {
                HandleEnemyHit(hit, hitDistance);
            }
            else if (hit.collider.CompareTag("Turret"))
            {
                HandleTurretHit(hit, hitDistance);
            }
            else if (hit.collider.CompareTag("SmallCanon"))
            {
                HandleSmallCanonHit(hit, hitDistance);
            }
            else if (hit.collider.CompareTag("BigCanon"))
            {
                HandleBigCanonHit(hit, hitDistance);
            }
            
            bulletDirection = (hit.point - currentSpawnPoint.position).normalized;
        }
        else
        {
            bulletDirection = ray.direction;
        }
        
        if (weaponManager.CanFireBullet())
        {
            SpawnBullet(currentSpawnPoint, bulletDirection);
            weaponManager.UseBullet();
        }
        currentSpawnIndex = (currentSpawnIndex + 1) % machineGunSpawnPoints.Count;
        
        PlayFireSound();
    }

    private void HandleEnemyHit(RaycastHit hit, float hitDistance)
    {
        bool inRange = hitDistance <= weaponManager.machineGunFireRange;
        
        if (inRange && weaponManager.CanFireBullet())
        {
            var enemyStats = hit.collider.GetComponentInParent<EnemyStats>();
            var mainBossStats = hit.collider.GetComponentInParent<MainBossStats>();
            
            float finalDamage = damage + (playerPlane != null ? playerPlane.attackPoint : 0);
            
            if (enemyStats != null)
            {
                enemyStats.TakeDamage(finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
            else if (mainBossStats != null)
            {
                mainBossStats.TakeDamage(finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
        }
    }

    private void HandleTurretHit(RaycastHit hit, float hitDistance)
    {
        bool inRange = hitDistance <= weaponManager.machineGunFireRange;
        
        if (inRange && weaponManager.CanFireBullet())
        {
            var turret = hit.collider.GetComponentInParent<TurretControl>();
            var smallCanon = hit.collider.GetComponentInParent<SmallCanonControl>();
            var bigCanon = hit.collider.GetComponentInParent<BigCanon>();
            
            float finalDamage = damage;
            if (playerPlane != null)
                finalDamage += playerPlane.attackPoint;
            
            if (turret != null)
            {
                turret.TakeDamage((int)finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
            else if (smallCanon != null)
            {
                smallCanon.TakeDamage((int)finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
            else if (bigCanon != null)
            {
                bigCanon.TakeDamage((int)finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
        }
    }

    private void HandleSmallCanonHit(RaycastHit hit, float hitDistance)
    {
        bool inRange = hitDistance <= weaponManager.machineGunFireRange;
        
        if (inRange && weaponManager.CanFireBullet())
        {
            var smallCanon = hit.collider.GetComponentInParent<SmallCanonControl>();
            if (smallCanon != null)
            {
                float finalDamage = damage + (playerPlane != null ? playerPlane.attackPoint : 0);
                smallCanon.TakeDamage((int)finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
        }
    }

    private void HandleBigCanonHit(RaycastHit hit, float hitDistance)
    {
        bool inRange = hitDistance <= weaponManager.machineGunFireRange;
        
        if (inRange && weaponManager.CanFireBullet())
        {
            var bigCanon = hit.collider.GetComponentInParent<BigCanon>();
            if (bigCanon != null)
            {
                float finalDamage = damage + (playerPlane != null ? playerPlane.attackPoint : 0);
                bigCanon.TakeDamage((int)finalDamage);
                DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
            }
        }
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

        GameObject bullet = null;
        
        if (PlayerProjectilePool.Instance != null)
        {
            bullet = PlayerProjectilePool.Instance.GetBullet(spawnPoint.position, Quaternion.LookRotation(direction), bulletLifetime);
        }
        
        if (bullet == null)
        {
            bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.LookRotation(direction));
            Destroy(bullet, bulletLifetime);
        }
        
        if (bullet != null)
        {
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * bulletSpeed;
                bullet.layer = LayerMask.NameToLayer("Player");
                bullet.tag = "PlayerWeapon";
            }
        }
    }
}
