using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MachineGunControl : MonoBehaviour
{
    [Header("References")]
    public PlayerWeaponManager weaponManager; // New reference to weapon manager
    public List<Transform> machineGunSpawnPoints = new List<Transform>(); // Multiple bullet spawn points across the plane
    
    [Header("Bullet Settings")]
    public GameObject bulletPrefab; // Bullet prefab for visual feedback
    public float bulletSpeed = 2000f; // Speed of visual bullets
    public float bulletLifetime = 5f; // How long bullets last
    
    [Header("Damage Settings")]
    public float damage = 10f; // Damage per hit
    private PlaneStats playerPlane; // Cache player stats
    
    private float nextFireTime = 0f;
    private int currentSpawnIndex = 0; // Track which spawn point to use next

    // Static variables for clean logging
    private static bool lastInFireRange = false;
    private static string lastRaycastHitName = null;

    private void Start()
    {
        if (bulletPrefab == null)
        {
            return;
        }
        // Get PlayerWeaponManager if not set
        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<PlayerWeaponManager>();
        }
        // Try to get playerPlane from GameManager if available
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        }
        // Fallback: Find player in scene by tag
        if (playerPlane == null)
        {
            var foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                playerPlane = foundPlayer.GetComponent<PlaneStats>();
        }
        // Register with BulletPool
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.RegisterProjectileType("Bullet", bulletLifetime);
        }
        
        // Check if spawn points are set up
        if (machineGunSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No bullet spawn points assigned to MachineGunControl!");
        }
    }

    private void Update()
    {
        if (weaponManager == null) return;

        bool inFireRange = weaponManager.IsTargetInRange(weaponManager.machineGunFireRange);
        if (inFireRange != lastInFireRange)
        {
            Debug.Log($"[MachineGun] In fire range: {inFireRange}");
            lastInFireRange = inFireRange;
        }

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
                    // Out of ammo, trigger reload
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
            return; // No bullets, cannot fire

        Ray ray = weaponManager.GetCurrentTargetRay();
        Debug.DrawRay(ray.origin, ray.direction * weaponManager.machineGunFireRange, Color.green, 1.0f);
        RaycastHit hit;
        
        Transform currentSpawnPoint = machineGunSpawnPoints[currentSpawnIndex];
        Vector3 bulletDirection;
        // Use the same layer mask as PlayerWeaponManager for consistency
        LayerMask targetableLayers = weaponManager.GetTargetableLayers();
        if (Physics.Raycast(ray, out hit, weaponManager.machineGunFireRange, targetableLayers))
        {
            if (hit.collider.name != lastRaycastHitName)
            {
                Debug.Log($"[MachineGun] Raycast hit: {hit.collider.name}");
                lastRaycastHitName = hit.collider.name;
            }
            float hitDistance = hit.distance;
            if (hit.collider.CompareTag("Enemy"))
            {
                bool inRange = hitDistance <= weaponManager.machineGunFireRange;
                Debug.Log($"[MachineGun] Raycast hit enemy: {hit.collider.name}, Distance: {hitDistance:F2}, In fire range: {inRange}");
                // Apply damage if in machine gun fire range only
                if (inRange && weaponManager.CanFireBullet())
                {
                    // Check for both regular enemies and main boss
                    var enemyStats = hit.collider.GetComponentInParent<EnemyStats>();
                    var mainBossStats = hit.collider.GetComponentInParent<MainBossStats>();
                    
                    if (enemyStats != null)
                    {
                        float finalDamage = damage + playerPlane.attackPoint;
                        enemyStats.TakeDamage(finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                        Debug.Log($"[MachineGun] Damaged EnemyStats: {enemyStats.name}");
                    }
                    else if (mainBossStats != null)
                    {
                        float finalDamage = damage + playerPlane.attackPoint;
                        mainBossStats.TakeDamage(finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                        Debug.Log($"[MachineGun] Damaged MainBossStats: {mainBossStats.name}");
                    }
                }
            }
            else if (hit.collider.CompareTag("Turret"))
            {
                bool inRange = hitDistance <= weaponManager.machineGunFireRange;
                Debug.Log($"[MachineGun] Raycast hit turret: {hit.collider.name}, Distance: {hitDistance:F2}, In fire range: {inRange}");
                if (inRange && weaponManager.CanFireBullet())
                {
                    // Check for all weapon types since all weapons use "Turret" tag
                    var turret = hit.collider.GetComponentInParent<TurretControl>();
                    var smallCanon = hit.collider.GetComponentInParent<SmallCanonControl>();
                    var bigCanon = hit.collider.GetComponentInParent<BigCanon>();
                    
                    float finalDamage = damage;
                    if (playerPlane != null)
                        finalDamage += playerPlane.attackPoint;
                    else
                        Debug.LogWarning("[MachineGunControl] playerPlane is null! No attackPoint bonus applied.");
                    
                    if (turret != null)
                    {
                        turret.TakeDamage((int)finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                        Debug.Log($"[MachineGun] Damaged TurretControl: {turret.name}");
                    }
                    else if (smallCanon != null)
                    {
                        smallCanon.TakeDamage((int)finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                        Debug.Log($"[MachineGun] Damaged SmallCanonControl: {smallCanon.name}");
                    }
                    else if (bigCanon != null)
                    {
                        bigCanon.TakeDamage((int)finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                        Debug.Log($"[MachineGun] Damaged BigCanon: {bigCanon.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MachineGun] Hit object with 'Turret' tag but no weapon component found: {hit.collider.name}");
                    }
                }
            }
            else if (hit.collider.CompareTag("SmallCanon"))
            {
                bool inRange = hitDistance <= weaponManager.machineGunFireRange;
                Debug.Log($"[MachineGun] Raycast hit small cannon: {hit.collider.name}, Distance: {hitDistance:F2}, In fire range: {inRange}");
                if (inRange && weaponManager.CanFireBullet())
                {
                    var smallCanon = hit.collider.GetComponentInParent<SmallCanonControl>();
                    if (smallCanon != null)
                    {
                        float finalDamage = damage + playerPlane.attackPoint;
                        smallCanon.TakeDamage((int)finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                    }
                }
            }
            else if (hit.collider.CompareTag("BigCanon"))
            {
                bool inRange = hitDistance <= weaponManager.machineGunFireRange;
                Debug.Log($"[MachineGun] Raycast hit big cannon: {hit.collider.name}, Distance: {hitDistance:F2}, In fire range: {inRange}");
                if (inRange && weaponManager.CanFireBullet())
                {
                    var bigCanon = hit.collider.GetComponentInParent<BigCanon>();
                    if (bigCanon != null)
                    {
                        float finalDamage = damage + playerPlane.attackPoint;
                        bigCanon.TakeDamage((int)finalDamage);
                        DmgPopUp.ShowDamage(hit.point, (int)finalDamage, Color.yellow);
                    }
                }
            }
            bulletDirection = (hit.point - currentSpawnPoint.position).normalized;
        }
        else
        {
            if (lastRaycastHitName != null)
            {
                Debug.Log("[MachineGun] Raycast hit: nothing");
                lastRaycastHitName = null;
            }
            bulletDirection = ray.direction;
        }
        if (weaponManager.CanFireBullet())
        {
            SpawnBullet(currentSpawnPoint, bulletDirection);
            weaponManager.UseBullet();
        }
        currentSpawnIndex = (currentSpawnIndex + 1) % machineGunSpawnPoints.Count;
        
        // Play SFX if assigned
        if (AudioSetting.Instance != null && AudioSetting.Instance.machineGunSound != null)
        {
            AudioSource.PlayClipAtPoint(AudioSetting.Instance.machineGunSound, machineGunSpawnPoints[currentSpawnIndex].position, AudioSetting.Instance.machineGunSFXVolume);
        }
    }

    void SpawnBullet(Transform spawnPoint, Vector3 direction)
    {
        if (bulletPrefab != null)
        {
            if (direction == Vector3.zero)
            {
                direction = spawnPoint.forward;
            }
            GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.LookRotation(direction));
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * bulletSpeed;
                bullet.layer = LayerMask.NameToLayer("Player");
                bullet.tag = "PlayerWeapon";
            }
            Destroy(bullet, bulletLifetime); // Ensure bullet is destroyed after its lifetime
        }
    }
}