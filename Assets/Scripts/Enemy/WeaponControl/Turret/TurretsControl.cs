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

    public void ControlTurret(Transform enemy, float howCloseToEnemy)
    {
        if (!gameObject.activeInHierarchy) return;
        if(enemy != null)
        {
            directionToEnemy = enemy.position - gunBarrel.position;
            float distanceToEnemy = Vector3.Distance(gunBarrel.position, enemy.position);

            if(distanceToEnemy < howCloseToEnemy)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);

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

                gunBarrel.LookAt(enemy.position);

                if(Time.time >= nextFire)
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
        if(turretSpawnPoints.Count == 0)
            return;

        Transform spawnPoint = turretSpawnPoints[spawnIndex];

        GameObject bulletObj = BulletPool.Instance.GetBullet("Turret");

        if(bulletObj != null)
        {
            bulletObj.tag = "Bullet";
            bulletObj.transform.position = spawnPoint.position;
            bulletObj.transform.rotation = Quaternion.LookRotation(gunBarrel.forward);
            
            // Bolt: Optimized - Use PooledBullet cached components to avoid per-shot GetComponent/TryGetComponent calls
            if (bulletObj.TryGetComponent(out PooledBullet pooled))
            {
                if (pooled.damage != null)
                {
                    pooled.damage.Initialize(damage, this);
                }

                if (pooled.rb != null)
                {
                    float speed = cachedManager != null ? cachedManager.bulletSpeed : 100f;
                    pooled.rb.linearVelocity = gunBarrel.forward * speed;
                }
            }
            else
            {
                // Fallback for safety, though BulletPool should always add PooledBullet now
                if (bulletObj.TryGetComponent(out BulletDamage bulletDamageComponent))
                {
                    bulletDamageComponent.Initialize(damage, this);
                }

                Rigidbody bulletRb = bulletObj.GetComponent<Rigidbody>();
                if (bulletRb != null)
                {
                    float speed = cachedManager != null ? cachedManager.bulletSpeed : 100f;
                    bulletRb.linearVelocity = gunBarrel.forward * speed;
                }
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
                playerStats.TakeDamage(damage);
            }
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(gameObject);
            }
        }
        else if (other.CompareTag("Enemy"))
        {
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
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(gameObject);
            }
        }
    }
}
