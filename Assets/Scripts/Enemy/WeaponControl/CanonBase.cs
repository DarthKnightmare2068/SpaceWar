using UnityEngine;
using UnityEngine.VFX;

public abstract class CanonBase : MonoBehaviour, IHittable, IHasHealth
{
    [Header("Cannon Components")]
    [SerializeField] protected Transform body;
    [SerializeField] protected Transform joint;
    [SerializeField] protected Transform gunBarrel;

    [Header("VFX")]
    [SerializeField] protected VisualEffect laserVFX;
    [SerializeField] protected GameObject laserVFXPrefab;

    [Header("Targeting")]
    [SerializeField] protected Transform enemy;
    [SerializeField] protected LayerMask hittableLayers = -1;

    [Header("Cannon Stats")]
    [SerializeField] protected float maxRotationSpeed = 2f;
    [SerializeField] protected float maxBodyRotationAngle = 90f;
    [SerializeField] protected float maxJointRotationAngle = 45f;

    [Header("Laser Scaling")]
    public float maxLaserScale = 1000f;

    public int maxHP = 100;
    public int currentHP = 100;

    protected float damage;
    protected float fireRate;
    protected float fireRange;
    // Bolt: Optimized - Cache squared fire range to avoid expensive square root calculations in sqrMagnitude comparisons.
    protected float fireRangeSqr;
    protected float currentLaserScale;
    protected float laserDamageInterval = 1f;
    protected float laserDamageTimer;
    protected bool isTargetLocked;
    protected float targetLockTimer;
    protected bool trackPlayerInstantly;
    protected bool isPlayerInRotationLimit;
    protected float rotationLimitTimer;
    protected int playerSearchFailCount;
    protected float playerSearchCooldown;
    protected WeaponDmgControl cachedDmgControl;
    protected WeaponHealthBar healthBar;

    [SerializeField] protected Transform laserEndPoint;

    protected Quaternion initialBodyRotation;
    protected Quaternion initialJointLocalRotation;
    protected Vector3 initialBodyForward;

    private GameObject activeLaserInstance;

    private const float TARGET_LOCK_DELAY = 1f;
    private const float ROTATION_LIMIT_DELAY = 2f;
    private const float PLAYER_SEARCH_INTERVAL = 1f;
    private const int PLAYER_SEARCH_FAIL_LIMIT = 5;

    public void SetTrackingMode(bool instant) { trackPlayerInstantly = instant; }

    protected virtual void Start()
    {
        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
            cachedDmgControl = FindObjectOfType<WeaponDmgControl>();

        currentHP = maxHP;
        InitializeStats();
        fireRangeSqr = fireRange * fireRange;
        FindPlayerTarget();
        StopLaserVFX();

        if (laserVFXPrefab != null)
            laserVFXPrefab.SetActive(false);

        initialBodyRotation = body.rotation;
        initialJointLocalRotation = joint.localRotation;
        initialBodyForward = body.forward;

        if (laserEndPoint == null)
            laserEndPoint = new GameObject("LaserEndPoint_Generated").transform;

        if (laserVFX != null)
            laserEndPoint.SetParent(laserVFX.transform);

        healthBar = GetComponentInChildren<WeaponHealthBar>();
    }

    protected abstract void InitializeStats();
    protected abstract void Die();

    protected virtual void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (enemy == null || !enemy.gameObject.activeInHierarchy)
        {
            playerSearchCooldown -= Time.deltaTime;
            if (playerSearchCooldown > 0f) return;

            playerSearchCooldown = PLAYER_SEARCH_INTERVAL;
            if (GameEntityRegistry.TryGetPlayerTransform(out Transform playerTransform))
            {
                enemy = playerTransform;
                playerSearchFailCount = 0;
                enabled = true;
            }
            else
            {
                playerSearchFailCount++;
                if (playerSearchFailCount >= PLAYER_SEARCH_FAIL_LIMIT)
                    enabled = false;
                return;
            }
        }

        HandleTargeting();
        HandleRotationAndFiring();
        UpdateLaserScale();

        if (!isTargetLocked)
        {
            StopLaserVFX();
            ResetToDefaultRotation();
        }
    }

    protected void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke();
        StopLaserVFX();
    }

    protected void OnEnable()
    {
        isTargetLocked = false;
        StopLaserVFX();
        ResetToDefaultRotation();
        playerSearchFailCount = 0;
        playerSearchCooldown = 0f;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        healthBar?.SetHealth(currentHP, maxHP);
        if (currentHP <= 0) Die();
    }

    // IHittable — lets DamageHelper use a single GetComponentInParent<IHittable>() call.
    void IHittable.TakeDamage(float amount) => TakeDamage((int)amount);

    // IHasHealth — lets LevelUpSystem track HP through a unified interface.
    float IHasHealth.CurrentHP => currentHP;
    float IHasHealth.MaxHP => maxHP;

    protected void FindPlayerTarget()
    {
        if (enemy != null) return;

        if (GameEntityRegistry.TryGetPlayerTransform(out Transform playerTransform))
            enemy = playerTransform;
        else
            enabled = false;
    }

    protected void PlayLaserVFX(float length)
    {
        if (!gameObject.activeInHierarchy) return;
        if (laserVFX != null)
        {
            if (!laserVFX.HasAnySystemAwake())
                laserVFX.Play();
        }
        else if (laserVFXPrefab != null && activeLaserInstance == null)
        {
            activeLaserInstance = Instantiate(laserVFXPrefab, gunBarrel.position, gunBarrel.rotation, gunBarrel);
        }
    }

    protected void StopLaserVFX()
    {
        if (laserVFX != null && laserVFX.HasAnySystemAwake())
            laserVFX.Stop();
        if (activeLaserInstance != null)
        {
            Destroy(activeLaserInstance);
            activeLaserInstance = null;
        }
    }

    protected void UpdateLaserScale()
    {
        // Only raycast when actually firing — skips the per-frame raycast on every idle canon.
        if (laserVFX == null || enemy == null || !isTargetLocked) return;
        float distance = maxLaserScale;
        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, maxLaserScale, hittableLayers))
            // Bolt: Optimized - Use hit.distance instead of Vector3.Distance(gunBarrel.position, hit.point)
            // because the distance is already calculated by the Raycast.
            distance = hit.distance;
        currentLaserScale = distance;
        laserVFX.transform.localScale = new Vector3(currentLaserScale / 2f, currentLaserScale / 2f, currentLaserScale);
    }

    protected void HandleTargeting()
    {
        if (!gameObject.activeInHierarchy) return;
        if (enemy == null)
        {
            isTargetLocked = false;
            isPlayerInRotationLimit = false;
            rotationLimitTimer = 0f;
            return;
        }

        bool canAimAtPlayer = CheckIfCanAimAtPlayer();

        if (!canAimAtPlayer && !isPlayerInRotationLimit)
        {
            isPlayerInRotationLimit = true;
            rotationLimitTimer = 0f;
            isTargetLocked = false;
            StopLaserVFX();
            return;
        }

        if (isPlayerInRotationLimit)
        {
            rotationLimitTimer += Time.deltaTime;
            if (rotationLimitTimer < ROTATION_LIMIT_DELAY) return;
            isPlayerInRotationLimit = false;
            rotationLimitTimer = 0f;
        }

        // Bolt: Optimized - Use sqrMagnitude instead of Vector3.Distance to save CPU cycles (no square root).
        if ((transform.position - enemy.position).sqrMagnitude <= fireRangeSqr && canAimAtPlayer)
        {
            isTargetLocked = true;
            targetLockTimer = 0f;
        }
        else
        {
            targetLockTimer += Time.deltaTime;
            if (targetLockTimer >= TARGET_LOCK_DELAY)
                isTargetLocked = false;
        }
    }

    protected bool CheckIfCanAimAtPlayer()
    {
        if (enemy == null) return false;

        Vector3 targetDirection = enemy.position - body.position;
        targetDirection.y = 0;
        if (targetDirection == Vector3.zero) return false;

        Quaternion targetBodyRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        float bodyAngle = (Quaternion.Inverse(initialBodyRotation) * targetBodyRotation).eulerAngles.y;
        if (bodyAngle > 180f) bodyAngle -= 360f;
        if (Mathf.Abs(bodyAngle) > maxBodyRotationAngle) return false;

        Vector3 worldDirToTarget = enemy.position - joint.position;
        if (worldDirToTarget == Vector3.zero) return false;

        Quaternion targetLocalRotation = Quaternion.Inverse(body.rotation) * Quaternion.LookRotation(worldDirToTarget, body.up);
        targetLocalRotation.y = 0;
        targetLocalRotation.z = 0;
        float jointPitch = targetLocalRotation.eulerAngles.x;
        if (jointPitch > 180f) jointPitch -= 360f;
        if (Mathf.Abs(jointPitch) > maxJointRotationAngle) return false;

        return true;
    }

    protected void HandleRotationAndFiring()
    {
        if (!gameObject.activeInHierarchy) return;

        if (!isTargetLocked || enemy == null)
        {
            if (laserVFXPrefab != null && laserVFXPrefab.activeSelf)
                laserVFXPrefab.SetActive(false);
            StopLaserVFX();
            laserDamageTimer = 0f;
            return;
        }

        RotateToTarget();
        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, fireRange, hittableLayers) && hit.transform.CompareTag("Player"))
        {
            if (laserEndPoint != null && laserVFX != null)
                laserEndPoint.localPosition = laserVFX.transform.InverseTransformPoint(hit.point);
            if (laserVFXPrefab != null && !laserVFXPrefab.activeSelf)
                laserVFXPrefab.SetActive(true);
            PlayLaserVFX(hit.distance);
            laserDamageTimer += Time.deltaTime;
            if (laserDamageTimer >= laserDamageInterval)
            {
                laserDamageTimer = 0f;
                hit.transform.GetComponent<PlaneStats>()?.TakeDamage((int)damage);
            }
        }
        else
        {
            if (laserVFXPrefab != null && laserVFXPrefab.activeSelf)
                laserVFXPrefab.SetActive(false);
            StopLaserVFX();
            laserDamageTimer = 0f;
        }
    }

    protected void RotateToTarget()
    {
        if (enemy == null) return;

        Vector3 targetDir = enemy.position - body.position;
        targetDir.y = 0;
        if (targetDir != Vector3.zero)
        {
            float angle = (Quaternion.Inverse(initialBodyRotation) * Quaternion.LookRotation(targetDir, Vector3.up)).eulerAngles.y;
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, -maxBodyRotationAngle, maxBodyRotationAngle);
            Quaternion target = initialBodyRotation * Quaternion.Euler(0, angle, 0);
            body.rotation = trackPlayerInstantly ? target : Quaternion.Slerp(body.rotation, target, maxRotationSpeed * Time.deltaTime);
        }

        Vector3 worldDir = enemy.position - joint.position;
        if (worldDir != Vector3.zero)
        {
            Quaternion localRot = Quaternion.Inverse(body.rotation) * Quaternion.LookRotation(worldDir, body.up);
            localRot.y = 0;
            localRot.z = 0;
            float pitch = localRot.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -maxJointRotationAngle, maxJointRotationAngle);
            Quaternion target = Quaternion.Euler(pitch, 0, 0);
            joint.localRotation = trackPlayerInstantly ? target : Quaternion.Slerp(joint.localRotation, target, maxRotationSpeed * Time.deltaTime);
        }
    }

    protected void ResetToDefaultRotation()
    {
        body.rotation = Quaternion.Slerp(body.rotation, initialBodyRotation, maxRotationSpeed * Time.deltaTime);
        joint.localRotation = Quaternion.Slerp(joint.localRotation, initialJointLocalRotation, maxRotationSpeed * Time.deltaTime);
    }
}
