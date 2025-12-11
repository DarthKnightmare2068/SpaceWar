using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretControl : MonoBehaviour
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
            cachedDmgControl = FindObjectOfType<WeaponDmgControl>();
        }
        
        cachedManager = GetComponentInParent<TurretsManager>();
        if (cachedManager == null)
        {
            cachedManager = FindObjectOfType<TurretsManager>();
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
                    var vfx = Instantiate(cachedManager.turretDestroyedVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                    float duration = 2f;
                    var ps = vfx.GetComponent<ParticleSystem>();
                    if (ps != null) duration = ps.main.duration;
                    Destroy(vfx, duration);
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

                Vector3 newPosition = new Vector3(joint.position.x, joint.position.y, joint.position.z);
                joint.position = newPosition;

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
            
            BulletDamage bulletDamageComponent = bulletObj.GetComponent<BulletDamage>();
            if (bulletDamageComponent == null)
            {
                bulletDamageComponent = bulletObj.AddComponent<BulletDamage>();
            }

            bulletDamageComponent.Initialize(damage, this);

            Rigidbody bulletRb = bulletObj.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                float speed = cachedManager != null ? cachedManager.bulletSpeed : 100f;
                bulletRb.velocity = gunBarrel.forward * speed;
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
                playerStats.TakeDamage((int)damage);
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
