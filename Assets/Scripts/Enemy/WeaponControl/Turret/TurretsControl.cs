using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretControl : MonoBehaviour
{
    [SerializeField]
    Transform body; // Reference to the turret body
    [SerializeField]
    Transform joint; // Reference to the turret joint
    [SerializeField]
    Transform gunBarrel; // Reference to the turret gun barrel
    private float fireRate = 0.1f; // This will be overwritten by WeaponDmgControl
    private float nextFire = 0f; // Tracks when the turret can fire next

    public List<Transform> turretSpawnPoints = new List<Transform>(); // List of spawn points

    public float maxRotationSpeed = 5.0f; // Adjust the rotation speed as needed
    Vector3 directionToEnemy;

    private float damage = 20f; // Default damage value if not set externally
    private int spawnIndex = 0; // Persistently cycle through spawn points

    // HP logic
    public int maxHP;
    public int currentHP;

    // Reference to health bar
    private WeaponHealthBar healthBar;

    private bool trackPlayerInstantly;
    public void SetTrackingMode(bool instant) { trackPlayerInstantly = instant; }

    private void Awake()
    {
        // currentHP will be set by TurretsManager

        // Initialize from WeaponDmgControl
        WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
        if (dmgControl != null)
        {
            damage = dmgControl.GetBulletDamage();
            fireRate = dmgControl.GetTurretFireRate();
        }
        else
        {
            // Fallback values if manager not found
            Debug.LogWarning("WeaponDmgControl not found. Using default values for TurretControl.");
            damage = 20f;
            fireRate = 0.1f;
        }
    }

    private void Start()
    {
        // Find the health bar component
        healthBar = GetComponentInChildren<WeaponHealthBar>();
        if (healthBar == null)
        {
            Debug.LogWarning($"[TurretControl] {gameObject.name}: No WeaponHealthBar found in children!");
        }
        else
        {
            Debug.Log($"[TurretControl] {gameObject.name}: Found WeaponHealthBar: {healthBar.name}");
        }
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"[TurretControl] {gameObject.name}: Taking {amount} damage. HP: {currentHP} -> {currentHP - amount}");
        currentHP -= amount;
        
        // Notify health bar of damage
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP, maxHP);
        }
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            
            // Play VFX from TurretsManager and call WeaponDmgControl for revive
            WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
            if (dmgControl != null)
            {
                TurretsManager manager = FindObjectOfType<TurretsManager>();
                if (manager != null && manager.turretDestroyedVFX != null)
                {
                    var vfx = Instantiate(manager.turretDestroyedVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                    // Auto-destroy VFX after duration
                    float duration = 2f;
                    var ps = vfx.GetComponent<ParticleSystem>();
                    if (ps != null) duration = ps.main.duration;
                    Destroy(vfx, duration);
                    Debug.Log($"Turret destroyed VFX played at {transform.position + Vector3.up * 1f} for {gameObject.name}");
                }
                dmgControl.OnTurretDestroyed();
            }
            // Add turret death logic here (disable, play animation, etc.)
            gameObject.SetActive(false);
        }
    }

    // Optionally, allow setting damage from outside (e.g., from WeaponDmgControl)
    public void SetBulletDamage(float newDamage)
    {
        damage = newDamage;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return; // Prevent logic if turret is inactive
        // ... existing Update logic ...
    }

    // Ensure no firing/rotation logic runs if inactive
    public void ControlTurret(Transform enemy, float howCloseToEnemy)
    {
        if (!gameObject.activeInHierarchy) return;
        if(enemy != null)
        {
            // Get the direction to the enemy
            directionToEnemy = enemy.position - gunBarrel.position;
            float distanceToEnemy = Vector3.Distance(gunBarrel.position, enemy.position);

            // Check if the enemy is close enough to fire
            if(distanceToEnemy < howCloseToEnemy)
            {
                // Calculate the rotation to look at the enemy
                Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);

                // Lock position along the z-axis
                Vector3 newPosition = new Vector3(joint.position.x, joint.position.y, joint.position.z);
                joint.position = newPosition;

                // Lock rotation along the x and z axes and only rotate around the y-axis
                Quaternion finalRotation = Quaternion.Euler(targetRotation.eulerAngles.x, targetRotation.eulerAngles.y, 90);
                if (trackPlayerInstantly)
                {
                    joint.rotation = finalRotation;
                    body.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                }
                else
                {
                    joint.rotation = Quaternion.Slerp(joint.rotation, finalRotation, maxRotationSpeed * Time.deltaTime);
                    body.rotation = Quaternion.Slerp(body.rotation, Quaternion.Euler(0, targetRotation.eulerAngles.y, 0), maxRotationSpeed * Time.deltaTime);
                }

                // Make the gun barrel face the enemy
                gunBarrel.LookAt(enemy.position);

                // Fire if the current time is greater than or equal to the next allowed fire time
                if(Time.time >= nextFire)
                {
                    nextFire = Time.time + fireRate; // Set the next allowed fire time
                    Shoot(); // Fire the bullet
                }
            }
        }
        else
        {
            // Optionally, reset or idle rotation here if needed
        }
    }

    void Shoot()
    {
        if (!gameObject.activeInHierarchy) return;
        if(turretSpawnPoints.Count == 0)
            return;

        Transform spawnPoint = turretSpawnPoints[spawnIndex];

        // Get a bullet from the bullet pool with the "Turret" type
        GameObject bulletObj = BulletPool.Instance.GetBullet("Turret");

        if(bulletObj != null)
        {
            bulletObj.tag = "Bullet";
            bulletObj.transform.position = spawnPoint.position;
            bulletObj.transform.rotation = Quaternion.LookRotation(gunBarrel.forward);
            
            // Add or get BulletDamage component
            BulletDamage bulletDamageComponent = bulletObj.GetComponent<BulletDamage>();
            if (bulletDamageComponent == null)
            {
                bulletDamageComponent = bulletObj.AddComponent<BulletDamage>();
            }

            // Use the local damage value (float)
            bulletDamageComponent.Initialize(damage, this);

            Rigidbody bulletRb = bulletObj.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                // Get bulletSpeed from TurretsManager
                TurretsManager manager = FindObjectOfType<TurretsManager>();
                float speed = manager != null ? manager.bulletSpeed : 100f;
                bulletRb.velocity = gunBarrel.forward * speed;
            }
        }

        // Cycle through spawn points.
        spawnIndex = (spawnIndex + 1) % turretSpawnPoints.Count;
    }

    void OnDisable()
    {
        // Stop all firing coroutines or timers when disabled
        StopAllCoroutines();
        CancelInvoke();
    }
}

// Component to handle bullet damage
public class BulletDamage : MonoBehaviour
{
    private float damage;
    private TurretControl sourceTurret;

    public void Initialize(float bulletDamage, TurretControl turret)
    {
        damage = bulletDamage;
        sourceTurret = turret;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaneStats playerStats = other.GetComponent<PlaneStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage((int)damage);
            }
            // Return bullet to pool immediately after hitting
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(gameObject);
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            // Return bullet to pool for other collisions
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(gameObject);
            }
        }
        else if (other.CompareTag("Turret"))
        {
            TurretControl turret = other.GetComponent<TurretControl>();
            if (turret != null)
            {
                turret.TakeDamage((int)damage);
            }
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(gameObject);
            }
        }
        else
        {
            // Return bullet to pool for other collisions
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(gameObject);
            }
        }
    }
}