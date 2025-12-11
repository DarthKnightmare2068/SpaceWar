using UnityEngine;

public class MissileController : MonoBehaviour
{
    private float speed;
    private float lifetime;
    [Header("Missile Damage")]
    public float baseDamage = 100f;
    private float damage;
    private float startTime;
    private Rigidbody rb;
    private GameObject shooter;
    private bool hasExploded = false;
    private bool isInitialized = false;
    [Header("Missile Mode")]
    public bool useAutoTargetLock = true;

    public void SetShooter(GameObject shooterObj)
    {
        shooter = shooterObj;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }
    }

    private void Start()
    {
        Invoke("SetInitialized", 0.1f);
    }

    private void SetInitialized()
    {
        isInitialized = true;
    }

    public void Initialize(float missileSpeed, float missileLifetime)
    {
        speed = missileSpeed;
        lifetime = missileLifetime;
        PlaneStats playerPlane = null;
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        damage = baseDamage;
        if (playerPlane != null)
            damage += playerPlane.attackPoint;
        startTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (hasExploded) return;

        if (!useAutoTargetLock)
        {
            if (rb != null)
            {
                rb.velocity = transform.forward * speed;
            }
            else
            {
                transform.position += transform.forward * speed * Time.fixedDeltaTime;
            }
        }
        else
        {
            if (rb != null)
            {
                rb.velocity = transform.forward * speed;
            }
            else
            {
                transform.position += transform.forward * speed * Time.fixedDeltaTime;
            }
        }

        float timeAlive = Time.time - startTime;
        if (timeAlive > lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || hasExploded) return;

        Vector3 hitPosition = other.ClosestPoint(transform.position);
        bool isShooter = (other.gameObject == shooter);

        if (isShooter) return;
        if (other.CompareTag("Player")) return;

        hasExploded = true;

        if (other.CompareTag("Turret"))
        {
            var turret = other.GetComponentInParent<TurretControl>();
            var smallCanon = other.GetComponentInParent<SmallCanonControl>();
            var bigCanon = other.GetComponentInParent<BigCanon>();
            
            if (turret != null)
            {
                turret.TakeDamage((int)damage);
                DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
            }
            else if (smallCanon != null)
            {
                smallCanon.TakeDamage((int)damage);
                DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
            }
            else if (bigCanon != null)
            {
                bigCanon.TakeDamage((int)damage);
                DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            var enemyStats = other.gameObject.GetComponentInParent<EnemyStats>();
            var mainBossStats = other.gameObject.GetComponentInParent<MainBossStats>();
            
            if (enemyStats != null)
            {
                if (!useAutoTargetLock)
                {
                    var player = GameManager.Instance.currentPlayer;
                    var weaponManager = player.GetComponent<PlayerWeaponManager>();
                    float distance = Vector3.Distance(player.transform.position, other.transform.position);
                    if (distance <= weaponManager.missileFireRange)
                    {
                        enemyStats.TakeDamage((int)damage);
                        DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
                    }
                }
                else
                {
                    AutoTargetLock autoTargetLock = FindObjectOfType<AutoTargetLock>();
                    if (autoTargetLock != null && autoTargetLock.IsValidTarget(other.transform))
                    {
                        enemyStats.TakeDamage((int)damage);
                        DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
                    }
                }
            }
            else if (mainBossStats != null)
            {
                if (!useAutoTargetLock)
                {
                    var player = GameManager.Instance.currentPlayer;
                    var weaponManager = player.GetComponent<PlayerWeaponManager>();
                    float distance = Vector3.Distance(player.transform.position, other.transform.position);
                    if (distance <= weaponManager.missileFireRange)
                    {
                        mainBossStats.TakeDamage((int)damage);
                        DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
                    }
                }
                else
                {
                    AutoTargetLock autoTargetLock = FindObjectOfType<AutoTargetLock>();
                    if (autoTargetLock != null && autoTargetLock.IsValidTarget(other.transform))
                    {
                        mainBossStats.TakeDamage((int)damage);
                        DmgPopUp.ShowDamage(hitPosition, (int)damage, Color.red);
                    }
                }
            }
        }

        Destroy(gameObject);
    }
}
