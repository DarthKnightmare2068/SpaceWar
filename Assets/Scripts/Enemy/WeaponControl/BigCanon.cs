using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class BigCanon : MonoBehaviour
{
    [Header("Cannon Components")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform joint;
    [SerializeField] private Transform gunBarrel;
    
    [Header("VFX")]
    [SerializeField] private VisualEffect laserVFX;
    [SerializeField] private GameObject laserVFXPrefab;

    [Header("Targeting")]
    [SerializeField] private Transform enemy;
    [SerializeField] private LayerMask hittableLayers = -1;

    [Header("Cannon Stats")]
    [SerializeField] private float maxRotationSpeed = 2f;
    [SerializeField] private float maxBodyRotationAngle = 90f;
    [SerializeField] private float maxJointRotationAngle = 45f;

    [Header("Laser Scaling")]
    public float maxLaserScale = 1000f;
    private float currentLaserScale = 0f;

    private float damage = 100f;
    private float fireRate = 0.1f;
    private float fireRange = 200f;
    private float nextFireTime;

    public int maxHP = 200;
    public int currentHP = 200;

    private WeaponHealthBar healthBar;
    private GameObject activeLaserInstance;
    private Quaternion initialBodyRotation;
    private Quaternion initialJointLocalRotation;
    private Vector3 initialBodyForward;
    private float laserDamageInterval = 0.5f;
    private float laserDamageTimer = 0f;

    [SerializeField] private Transform laserEndPoint;

    private bool isTargetLocked = false;
    private float targetLockTimer = 0f;
    private const float TARGET_LOCK_DELAY = 1f;

    private bool trackPlayerInstantly = false;
    public void SetTrackingMode(bool instant) { trackPlayerInstantly = instant; }

    private bool isPlayerInRotationLimit = false;
    private float rotationLimitTimer = 0f;
    private const float ROTATION_LIMIT_DELAY = 2f;

    [Header("Explosion VFX")]
    [SerializeField] private GameObject explosionVFXPrefab;

    private float playerSearchCooldown = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 1f;
    private int playerSearchFailCount = 0;
    private const int PLAYER_SEARCH_FAIL_LIMIT = 5;

    private WeaponDmgControl cachedDmgControl;

    void Start()
    {
        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
        {
            cachedDmgControl = FindObjectOfType<WeaponDmgControl>();
        }

        currentHP = maxHP;
        trackPlayerInstantly = true;
        InitializeStats();
        FindPlayerTarget();
        StopLaserVFX();
        
        if (laserVFXPrefab != null)
            laserVFXPrefab.SetActive(false);
        
        initialBodyRotation = body.rotation;
        initialJointLocalRotation = joint.localRotation;
        initialBodyForward = body.forward;
        
        if (laserEndPoint == null)
        {
            GameObject go = new GameObject("BigLaserEndPoint_Generated");
            laserEndPoint = go.transform;
        }
        
        if (laserVFX != null)
        {
            laserEndPoint.SetParent(laserVFX.transform);
        }

        healthBar = GetComponentInChildren<WeaponHealthBar>();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (enemy == null || !enemy.gameObject.activeInHierarchy)
        {
            playerSearchCooldown -= Time.deltaTime;
            if (playerSearchCooldown <= 0f)
            {
                playerSearchCooldown = PLAYER_SEARCH_INTERVAL;
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    enemy = playerObject.transform;
                    playerSearchFailCount = 0;
                    enabled = true;
                }
                else
                {
                    playerSearchFailCount++;
                    if (playerSearchFailCount >= PLAYER_SEARCH_FAIL_LIMIT)
                    {
                        enabled = false;
                    }
                    return;
                }
            }
            else
            {
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

    void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke();
        StopLaserVFX();
    }

    void OnEnable()
    {
        isTargetLocked = false;
        StopLaserVFX();
        ResetToDefaultRotation();
        playerSearchFailCount = 0;
        playerSearchCooldown = 0f;
    }

    private void InitializeStats()
    {
        if (cachedDmgControl != null)
        {
            damage = cachedDmgControl.GetBigCanonDamage();
            fireRate = cachedDmgControl.GetBigCanonFireRate();
            fireRange = cachedDmgControl.GetBigCanonFireRange();
        }
        else
        {
            damage = 100f;
            fireRate = 0.1f;
            fireRange = 200f;
        }
    }

    private void FindPlayerTarget()
    {
        if (enemy == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                enemy = playerObject.transform;
            }
            else
            {
                enabled = false;
            }
        }
    }

    void PlayLaserVFX(float length)
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

    void StopLaserVFX()
    {
        if (laserVFX != null && laserVFX.HasAnySystemAwake())
        {
            laserVFX.Stop();
        }
        if (activeLaserInstance != null)
        {
            Destroy(activeLaserInstance);
            activeLaserInstance = null;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP, maxHP);
        }
    }

    private void Die()
    {
        if (explosionVFXPrefab != null)
        {
            var vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            float duration = 2f;
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null) duration = ps.main.duration;
            Destroy(vfx, duration);
        }
        
        if (cachedDmgControl != null)
        {
            cachedDmgControl.OnBigCanonDestroyed();
        }
        gameObject.SetActive(false);
    }

    void UpdateLaserScale()
    {
        if (laserVFX != null && enemy != null)
        {
            float distance = maxLaserScale;
            RaycastHit hit;
            if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, maxLaserScale, hittableLayers))
            {
                distance = Vector3.Distance(gunBarrel.position, hit.point);
            }
            currentLaserScale = distance;
            Vector3 newScale = new Vector3(currentLaserScale / 2f, currentLaserScale / 2f, currentLaserScale);
            laserVFX.transform.localScale = newScale;
        }
    }

    private void HandleTargeting()
    {
        if (!gameObject.activeInHierarchy) return;
        if (enemy == null)
        {
            if (isTargetLocked)
            {
                isTargetLocked = false;
            }
            isPlayerInRotationLimit = false;
            rotationLimitTimer = 0f;
            return;
        }

        float distanceToEnemy = Vector3.Distance(transform.position, enemy.position);
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
            if (rotationLimitTimer < ROTATION_LIMIT_DELAY)
            {
                return;
            }
            else
            {
                isPlayerInRotationLimit = false;
                rotationLimitTimer = 0f;
            }
        }

        if (distanceToEnemy <= fireRange && canAimAtPlayer)
        {
            isTargetLocked = true;
            targetLockTimer = 0f;
        }
        else
        {
            targetLockTimer += Time.deltaTime;
            if (targetLockTimer >= TARGET_LOCK_DELAY)
            {
                isTargetLocked = false;
            }
        }
    }

    private void HandleRotationAndFiring()
    {
        if (!gameObject.activeInHierarchy) return;
        if (isTargetLocked && enemy != null)
        {
            RotateToTarget();
            RaycastHit hit;
            bool didHit = Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, fireRange, hittableLayers);
            if (didHit && hit.transform.CompareTag("Player"))
            {
                if (laserEndPoint != null && laserVFX != null)
                    laserEndPoint.localPosition = laserVFX.transform.InverseTransformPoint(hit.point);
                float length = Vector3.Distance(gunBarrel.position, hit.point);
                if (laserVFXPrefab != null && !laserVFXPrefab.activeSelf)
                    laserVFXPrefab.SetActive(true);
                PlayLaserVFX(length);
                laserDamageTimer += Time.deltaTime;
                if (laserDamageTimer >= laserDamageInterval)
                {
                    laserDamageTimer = 0f;
                    PlaneStats playerStats = hit.transform.GetComponent<PlaneStats>();
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage((int)damage);
                    }
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
        else
        {
            if (laserVFXPrefab != null && laserVFXPrefab.activeSelf)
                laserVFXPrefab.SetActive(false);
            StopLaserVFX();
            laserDamageTimer = 0f;
        }
    }

    void RotateToTarget()
    {
        if (enemy == null) return;
        Vector3 targetDirection = enemy.position - body.position;
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetBodyRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            Quaternion relativeRotation = Quaternion.Inverse(initialBodyRotation) * targetBodyRotation;
            float angle = relativeRotation.eulerAngles.y;
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, -maxBodyRotationAngle, maxBodyRotationAngle);
            Quaternion clampedRotation = initialBodyRotation * Quaternion.Euler(0, angle, 0);
            if (trackPlayerInstantly)
                body.rotation = clampedRotation;
            else
                body.rotation = Quaternion.Slerp(body.rotation, clampedRotation, maxRotationSpeed * Time.deltaTime);
        }
        Vector3 worldDirToTarget = enemy.position - joint.position;
        if (worldDirToTarget != Vector3.zero)
        {
            Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirToTarget, body.up);
            Quaternion targetLocalRotation = Quaternion.Inverse(body.rotation) * targetWorldRotation;
            targetLocalRotation.y = 0;
            targetLocalRotation.z = 0;
            float pitch = targetLocalRotation.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -maxJointRotationAngle, maxJointRotationAngle);
            Quaternion clampedLocalRotation = Quaternion.Euler(pitch, 0, 0);
            if (trackPlayerInstantly)
                joint.localRotation = clampedLocalRotation;
            else
                joint.localRotation = Quaternion.Slerp(joint.localRotation, clampedLocalRotation, maxRotationSpeed * Time.deltaTime);
        }
    }

    private bool CheckIfCanAimAtPlayer()
    {
        if (enemy == null) return false;

        Vector3 targetDirection = enemy.position - body.position;
        targetDirection.y = 0;
        
        if (targetDirection == Vector3.zero) return false;
        
        Quaternion targetBodyRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        Quaternion relativeRotation = Quaternion.Inverse(initialBodyRotation) * targetBodyRotation;
        float bodyAngle = relativeRotation.eulerAngles.y;
        if (bodyAngle > 180f) bodyAngle -= 360f;
        
        if (Mathf.Abs(bodyAngle) > maxBodyRotationAngle)
        {
            return false;
        }
        
        Vector3 worldDirToTarget = enemy.position - joint.position;
        if (worldDirToTarget == Vector3.zero) return false;
        
        Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirToTarget, body.up);
        Quaternion targetLocalRotation = Quaternion.Inverse(body.rotation) * targetWorldRotation;
        targetLocalRotation.y = 0;
        targetLocalRotation.z = 0;
        
        float jointPitch = targetLocalRotation.eulerAngles.x;
        if (jointPitch > 180f) jointPitch -= 360f;
        
        if (Mathf.Abs(jointPitch) > maxJointRotationAngle)
        {
            return false;
        }
        
        return true;
    }

    private void ResetToDefaultRotation()
    {
        body.rotation = Quaternion.Slerp(body.rotation, initialBodyRotation, maxRotationSpeed * Time.deltaTime);
        joint.localRotation = Quaternion.Slerp(joint.localRotation, initialJointLocalRotation, maxRotationSpeed * Time.deltaTime);
    }
}
