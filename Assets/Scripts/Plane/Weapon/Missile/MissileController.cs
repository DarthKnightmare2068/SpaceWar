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

    // Cached references to avoid FindObjectOfType on collision
    private AutoTargetLock cachedAutoTargetLock;
    private PlayerWeaponManager cachedWeaponManager;
    private Transform cachedPlayerTransform;

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
        // Reset per-life state so pooled missile re-use works.
        hasExploded = false;
        isInitialized = false;
        CancelInvoke();
        Invoke(nameof(SetInitialized), 0.1f);
        PlaneStats playerPlane = null;
        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
        {
            playerPlane = player.GetComponent<PlaneStats>();
            // Cache references once during initialization instead of on collision
            cachedAutoTargetLock = player.GetComponent<AutoTargetLock>();
            cachedWeaponManager = player.GetComponent<PlayerWeaponManager>();
            cachedPlayerTransform = player.transform;
        }
        damage = baseDamage;
        if (playerPlane != null)
            damage += playerPlane.attackPoint;
        startTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (hasExploded) return;

        // Simplified - removed duplicate code from both branches
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            transform.position += transform.forward * speed * Time.fixedDeltaTime;
        }

        float timeAlive = Time.time - startTime;
        if (timeAlive > lifetime)
        {
            ReturnToPoolOrDestroy();
        }
    }

    private void ReturnToPoolOrDestroy()
    {
        hasExploded = true;
        if (PlayerProjectilePool.Instance != null)
            PlayerProjectilePool.Instance.ReturnMissile(gameObject);
        else
            Destroy(gameObject);
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
            DamageHelper.TryDealDamage(other, damage, Color.red, hitPosition);
        }
        else if (other.CompareTag("Enemy"))
        {
            // Range/lock gating still applies before damage — DamageHelper handles the IHittable walk.
            bool allowed;
            if (!useAutoTargetLock)
                allowed = cachedPlayerTransform != null && cachedWeaponManager != null &&
                          Vector3.Distance(cachedPlayerTransform.position, other.transform.position) <= cachedWeaponManager.missileFireRange;
            else
                allowed = cachedAutoTargetLock != null && cachedAutoTargetLock.IsValidTarget(other.transform);

            if (allowed)
                DamageHelper.TryDealDamage(other, damage, Color.red, hitPosition);
        }

        ReturnToPoolOrDestroy();
    }
}
