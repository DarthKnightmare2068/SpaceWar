using UnityEngine;
using UnityEngine.VFX;

public abstract class CanonBase : MonoBehaviour, IHittable, IHasHealth, ITargetable
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
    protected float currentLaserScale;
    protected float laserDamageInterval = 1f;
    protected float laserDamageTimer;
    protected bool isTargetLocked;
    protected float targetLockTimer;
    protected bool trackPlayerInstantly;
    protected bool isPlayerInRotationLimit;
    protected float rotationLimitTimer;
    protected float playerSearchCooldown;
    protected WeaponDmgControl cachedDmgControl;
    protected WeaponHealthBar healthBar;

    [SerializeField] protected Transform laserEndPoint;

    protected Quaternion initialBodyRotation;
    protected Quaternion initialJointLocalRotation;
    protected Vector3 initialBodyForward;

    private bool _startCompleted;
    private GameObject activeLaserInstance;

    // Throttled to ~10 Hz so a 10-cannon boss does ~10 raycasts/frame instead of 20.
    private RaycastHit lastHit;
    private bool lastHitValid;
    private float aimRaycastTimer;
    private const float AIM_RAYCAST_INTERVAL = 0.1f;

    private const float TARGET_LOCK_DELAY = 1f;
    private const float ROTATION_LIMIT_DELAY = 2f;
    private const float PLAYER_SEARCH_INTERVAL = 1f;

    public void SetTrackingMode(bool instant) { trackPlayerInstantly = instant; }

    protected virtual void Start()
    {
        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
            cachedDmgControl = FindAnyObjectByType<WeaponDmgControl>();

        currentHP = maxHP;
        InitializeStats();
        FindPlayerTarget();
        StopLaserVFX();

        if (laserVFXPrefab != null)
            laserVFXPrefab.SetActive(false);

        initialBodyRotation = body.rotation;
        initialJointLocalRotation = joint.localRotation;
        initialBodyForward = body.forward;
        _startCompleted = true;

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
                enemy = playerTransform;
            else
                return;
        }

        // Skip the heavy aiming/raycast work when the target is well outside fire range.
        // Threshold is fireRange * sqrt(2) (sqr 2x) so cannons re-engage smoothly on approach.
        if (enemy != null)
        {
            float sqrDist = (enemy.position - transform.position).sqrMagnitude;
            float gate = fireRange * fireRange * 2f;
            if (gate > 0f && sqrDist > gate)
            {
                if (isTargetLocked)
                {
                    isTargetLocked = false;
                    StopLaserVFX();
                }
                ResetToDefaultRotation();
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
        // Guard: initialBodyRotation is set in Start; calling ResetToDefaultRotation
        // before Start runs would Slerp toward Quaternion(0,0,0,0) (invalid, not identity)
        // and corrupt the rest-rotation that all aim-angle checks are relative to.
        if (_startCompleted) ResetToDefaultRotation();
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

    // ITargetable — auto-target lock uses this single interface call.
    public Transform Transform => transform;
    public bool IsAlive => currentHP > 0 && gameObject.activeInHierarchy;

    protected void FindPlayerTarget()
    {
        if (enemy != null) return;

        if (GameEntityRegistry.TryGetPlayerTransform(out Transform playerTransform))
            enemy = playerTransform;
        // Don't disable — Update polls on cooldown until the player registers.
        // Setting enabled=false here permanently breaks the cannon when canons
        // spawn before the player (which is always the case at scene start).
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
        if (lastHitValid)
            distance = Vector3.Distance(gunBarrel.position, lastHit.point);
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

        float sqrDistToEnemy = (enemy.position - transform.position).sqrMagnitude;
        if (sqrDistToEnemy <= fireRange * fireRange && canAimAtPlayer)
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
            lastHitValid = false;
            aimRaycastTimer = 0f;
            return;
        }

        RotateToTarget();

        aimRaycastTimer -= Time.deltaTime;
        if (aimRaycastTimer <= 0f)
        {
            aimRaycastTimer = AIM_RAYCAST_INTERVAL;
            lastHitValid = Physics.Raycast(gunBarrel.position, gunBarrel.forward, out lastHit, fireRange, hittableLayers);
        }

        if (lastHitValid && lastHit.transform != null && lastHit.transform.CompareTag("Player"))
        {
            if (laserEndPoint != null && laserVFX != null)
                laserEndPoint.localPosition = laserVFX.transform.InverseTransformPoint(lastHit.point);
            if (laserVFXPrefab != null && !laserVFXPrefab.activeSelf)
                laserVFXPrefab.SetActive(true);
            PlayLaserVFX(Vector3.Distance(gunBarrel.position, lastHit.point));
            laserDamageTimer += Time.deltaTime;
            if (laserDamageTimer >= laserDamageInterval)
            {
                laserDamageTimer = 0f;
                lastHit.transform.GetComponent<PlaneStats>()?.TakeDamage(damage);
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
