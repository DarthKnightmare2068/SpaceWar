using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretControl : MonoBehaviour, IHittable, IHasHealth, ITargetable
{
    [SerializeField]
    Transform body;
    [SerializeField]
    Transform joint;
    [SerializeField]
    Transform gunBarrel;
    private float fireRate = 0.1f;
    private float nextFire = 0f;

    public List<Transform> turretSpawnPoints = new List<Transform>();

    public float maxRotationSpeed = 5.0f;
    Vector3 directionToEnemy;

    private float damage = 20f;
    private int spawnIndex = 0;

    public int maxHP;
    public int currentHP;

    private WeaponHealthBar healthBar;

    private bool trackPlayerInstantly;
    public void SetTrackingMode(bool instant) { trackPlayerInstantly = instant; }

    private WeaponDmgControl cachedDmgControl;
    private TurretsManager cachedManager;

    public Transform CurrentTarget { get; set; }

    private void Awake()
    {
        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
        {
            cachedDmgControl = FindAnyObjectByType<WeaponDmgControl>();
        }

        cachedManager = GetComponentInParent<TurretsManager>();
        if (cachedManager == null)
        {
            cachedManager = FindAnyObjectByType<TurretsManager>();
        }

        if (cachedDmgControl != null)
        {
            damage = cachedDmgControl.GetBulletDamage();
            fireRate = cachedDmgControl.GetTurretFireRate();
        }
        else
        {
            damage = 20f;
            fireRate = 0.1f;
        }
    }

    private void Start()
    {
        healthBar = GetComponentInChildren<WeaponHealthBar>();
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP, maxHP);
        }
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            
            if (cachedDmgControl != null)
            {
                if (cachedManager != null && cachedManager.turretDestroyedVFX != null)
                {
                    if (VFXPool.Instance != null)
                        VFXPool.Instance.Get(cachedManager.turretDestroyedVFX, transform.position + Vector3.up * 1f);
                    else
                    {
                        var vfx = Instantiate(cachedManager.turretDestroyedVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                        Destroy(vfx, 2f);
                    }
                }
                cachedDmgControl.OnTurretDestroyed();
            }
            gameObject.SetActive(false);
        }
    }

    public void SetBulletDamage(float newDamage)
    {
        damage = newDamage;
    }

    // IHittable — single GetComponentInParent<IHittable>() call from DamageHelper.
    void IHittable.TakeDamage(float amount) => TakeDamage((int)amount);

    // IHasHealth — lets LevelUpSystem track HP via unified interface.
    float IHasHealth.CurrentHP => currentHP;
    float IHasHealth.MaxHP => maxHP;

    // ITargetable
    public Transform Transform => transform;
    public bool IsAlive => currentHP > 0 && gameObject.activeInHierarchy;

    public void ControlTurret(float howCloseToEnemySqr)
    {
        if (!gameObject.activeInHierarchy) return;
        if (CurrentTarget != null)
        {
            // Bolt: Optimized - cache target position to minimize native calls
            Vector3 targetPos = CurrentTarget.position;
            directionToEnemy = targetPos - gunBarrel.position;
            // Bolt: Optimized using sqrMagnitude
            float sqrDistanceToEnemy = directionToEnemy.sqrMagnitude;

            if (sqrDistanceToEnemy < howCloseToEnemySqr)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
                Vector3 euler = targetRotation.eulerAngles;

                Quaternion finalRotation = Quaternion.Euler(euler.x, euler.y, 90);
                if (trackPlayerInstantly)
                {
                    joint.rotation = finalRotation;
                    body.rotation = Quaternion.Euler(0, euler.y, 0);
                }
                else
                {
                    float dt = Time.deltaTime;
                    joint.rotation = Quaternion.Slerp(joint.rotation, finalRotation, maxRotationSpeed * dt);
                    body.rotation = Quaternion.Slerp(body.rotation, Quaternion.Euler(0, euler.y, 0), maxRotationSpeed * dt);
                }

                // Bolt: Optimized - use calculated rotation instead of redundant LookAt
                gunBarrel.rotation = targetRotation;

                if (Time.time >= nextFire)
                {
                    nextFire = Time.time + fireRate;
                    Shoot();
                }
            }
        }
    }

    void Shoot()
    {
        if (!gameObject.activeInHierarchy) return;
        if (turretSpawnPoints.Count == 0)
            return;

        Transform spawnPoint = turretSpawnPoints[spawnIndex];

        // Bolt: Optimized - GetBullet now returns PooledBullet directly, avoiding TryGetComponent
        PooledBullet pooled = BulletPool.Instance.GetBullet("Turret");

        if (pooled != null)
        {
            pooled.cachedTransform.SetPositionAndRotation(spawnPoint.position, Quaternion.LookRotation(gunBarrel.forward));

            if (pooled.bulletDamage != null)
            {
                pooled.bulletDamage.Initialize(damage, this, pooled);
            }
            if (pooled.rb != null)
            {
                float speed = cachedManager != null ? cachedManager.bulletSpeed : 100f;
                pooled.rb.linearVelocity = gunBarrel.forward * speed;
            }
        }

        spawnIndex = (spawnIndex + 1) % turretSpawnPoints.Count;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke();
    }
}

public class BulletDamage : MonoBehaviour
{
    private float damage;
    private TurretControl sourceTurret;
    private PooledBullet cachedPooledBullet;

    public void Initialize(float bulletDamage, TurretControl turret, PooledBullet pooled)
    {
        damage = bulletDamage;
        sourceTurret = turret;
        cachedPooledBullet = pooled;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaneStats playerStats = other.GetComponent<PlaneStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }
            ReturnToPool();
        }
        else if (other.CompareTag("Enemy"))
        {
            ReturnToPool();
        }
        else if (other.CompareTag("Turret"))
        {
            TurretControl turret = other.GetComponent<TurretControl>();
            if (turret != null)
            {
                turret.TakeDamage((int)damage);
            }
            ReturnToPool();
        }
        else
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (BulletPool.Instance != null)
        {
            if (cachedPooledBullet != null)
                BulletPool.Instance.ReturnBullet(cachedPooledBullet);
            else
                BulletPool.Instance.ReturnBullet(gameObject);
        }
    }
}
