// This script is designed to control a cannon with a non-standard rotation setup.
// It requires a specific hierarchy: Body -> Joint -> GunBarrel.
// - Body: Rotates on its local Z-axis for yaw (left/right).
// - Joint: Rotates on its local X-axis for pitch (up/down).

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SmallCanonControl : MonoBehaviour
{
    [Header("Cannon Components")]
    [Tooltip("The object that rotates left/right on its Z-axis.")]
    [SerializeField] private Transform body;
    [Tooltip("The object that rotates up/down on its X-axis. Must be a child of Body.")]
    [SerializeField] private Transform joint;
    [Tooltip("The empty object marking the laser's origin. Must be a child of Joint.")]
    [SerializeField] private Transform gunBarrel;
    
    [Header("VFX")]
    [Tooltip("Direct reference to the laser Visual Effect component.")]
    [SerializeField] private VisualEffect laserVFX;
    [Tooltip("Prefab to instantiate if the direct VFX reference is not set.")]
    [SerializeField] private GameObject laserVFXPrefab;

    [Header("Targeting")]
    [Tooltip("The target for the cannon to aim at. Finds 'Player' tag if empty.")]
    [SerializeField] private Transform enemy;
    [Tooltip("Layers that the cannon's raycast can hit. Should include the Player's layer.")]
    [SerializeField] private LayerMask hittableLayers = -1; // Default to 'Everything'

    [Header("Cannon Stats")]
    [SerializeField] private float maxRotationSpeed = 3f;
    [SerializeField] private float maxBodyRotationAngle = 90f; // Maximum body yaw in degrees
    [SerializeField] private float maxJointRotationAngle = 45f; // Maximum joint pitch in degrees

    [Header("Laser Scaling")]
    public float maxLaserScale = 1000f; // Adjustable in Inspector
    private float currentLaserScale = 0f; // For debug/inspection

    // Internal state variables
    private float damage;
    private float fireRate;
    private float fireRange;
    private float nextFireTime;

    public int maxHP = 100;
    public int currentHP = 100;

    // Reference to health bar
    private WeaponHealthBar healthBar;

    private GameObject activeLaserInstance; // Stores the instantiated laser prefab

    // --- Cached Initial State ---
    // We store the cannon's default rotations from the prefab at the start.
    // This allows us to apply offsets correctly without fighting the prefab's pose.
    private Quaternion initialBodyRotation;
    private Quaternion initialJointLocalRotation;
    private Vector3 initialBodyForward;

    private float laserDamageInterval = 1f;
    private float laserDamageTimer = 0f;

    [SerializeField] private Transform laserEndPoint; // Assign in Inspector or create at runtime

    private bool isTargetLocked = false;
    private float targetLockTimer = 0f;
    private const float TARGET_LOCK_DELAY = 1f;

    private bool trackPlayerInstantly;
    public void SetTrackingMode(bool instant) { trackPlayerInstantly = instant; }

    // New variables for rotation limit detection
    private bool isPlayerInRotationLimit = false;
    private float rotationLimitTimer = 0f;
    private const float ROTATION_LIMIT_DELAY = 2f; // How long to wait before re-targeting after hitting rotation limits

    private float playerSearchCooldown = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 1f;
    private int playerSearchFailCount = 0;
    private const int PLAYER_SEARCH_FAIL_LIMIT = 5;

    #region Unity Lifecycle
    void Start()
    {
        InitializeStats();
        FindPlayerTarget();
        StopLaserVFX(); // Ensure laser is off at the start
        // Disable the parent VFX GameObject at start (use laserVFXPrefab)
        if (laserVFXPrefab != null)
            laserVFXPrefab.SetActive(false);
        // Cache the default rotations of the cannon parts when the game starts.
        initialBodyRotation = body.rotation;
        initialJointLocalRotation = joint.localRotation;
        initialBodyForward = body.forward;
        // Auto-create the laser end point if not assigned
        if (laserEndPoint == null)
        {
            GameObject go = new GameObject("LaserEndPoint_Generated");
            laserEndPoint = go.transform;
        }
        // Parent the endpoint to the VFX object to ensure it works in local space
        if (laserVFX != null)
        {
            laserEndPoint.SetParent(laserVFX.transform);
        }

        // Find the health bar component
        healthBar = GetComponentInChildren<WeaponHealthBar>();
        if (healthBar == null)
        {
            Debug.LogWarning($"[SmallCanonControl] {gameObject.name}: No WeaponHealthBar found in children!");
        }
        else
        {
            Debug.Log($"[SmallCanonControl] {gameObject.name}: Found WeaponHealthBar: {healthBar.name}");
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        // Always reacquire player if missing or inactive
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
                    Debug.Log($"[SmallCanonControl] {gameObject.name}: Reacquired player target: {playerObject.name}");
                }
                else
                {
                    playerSearchFailCount++;
                    if (playerSearchFailCount >= PLAYER_SEARCH_FAIL_LIMIT)
                    {
                        Debug.LogWarning($"[SmallCanonControl] {gameObject.name}: Could not find player for several seconds. Disabling script.");
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
        // Reset state if needed when revived
        isTargetLocked = false;
        StopLaserVFX();
        ResetToDefaultRotation();
        playerSearchFailCount = 0;
        playerSearchCooldown = 0f;
    }
    #endregion

    #region Initialization
    private void InitializeStats()
    {
        // Get stats from the central manager
        WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
        if (dmgControl != null)
        {
            damage = dmgControl.GetSmallCanonDamage();
            fireRate = dmgControl.GetSmallCanonFireRate();
            fireRange = dmgControl.GetSmallCanonFireRange();
        }
        else
        {
            Debug.LogWarning("[SmallCanonControl] WeaponDmgControl not found. Using default values.");
            damage = 10f;
            fireRate = 2f;
            fireRange = 100f;
        }
    }

    private void FindPlayerTarget()
    {
        Debug.Log($"[SmallCanonControl] {gameObject.name}: Starting player search...");
        
        if (enemy == null)
        {
            Debug.Log($"[SmallCanonControl] {gameObject.name}: No enemy assigned, searching for Player tag...");
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                enemy = playerObject.transform;
                Debug.Log($"[SmallCanonControl] {gameObject.name}: ✅ Player found! Target set to: {playerObject.name}");
            }
            else
            {
                Debug.LogError($"[SmallCanonControl] {gameObject.name}: ❌ No target assigned and no GameObject with 'Player' tag found. Disabling script.");
                enabled = false; // Disable script if no target can be found
            }
        }
        else
        {
            Debug.Log($"[SmallCanonControl] {gameObject.name}: Enemy already assigned: {enemy.name}");
        }
    }
    #endregion

    #region Laser and Damage
    void FireLaser()
    {
        if (!gameObject.activeInHierarchy) return;
        RaycastHit hit;
        bool didHit = Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, fireRange, hittableLayers);

        if (didHit && hit.transform.CompareTag("Player"))
        {
            PlaneStats playerStats = hit.transform.GetComponent<PlaneStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage((int)damage);
            }
        }
    }
    #endregion

    #region VFX Control
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
    #endregion

    #region Health and Destruction
    public void TakeDamage(int amount)
    {
        Debug.Log($"[SmallCanonControl] {gameObject.name}: Taking {amount} damage. HP: {currentHP} -> {currentHP - amount}");
        currentHP -= amount;
        
        // Notify health bar of damage
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP, maxHP);
        }
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    private void Die()
    {
        // Notify the manager that this cannon was destroyed
        WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
        if (dmgControl != null)
        {
            SmallCanonManager manager = FindObjectOfType<SmallCanonManager>();
            if (manager != null && manager.canonDestroyedVFX != null)
            {
                var vfx = Instantiate(manager.canonDestroyedVFX, transform.position, Quaternion.identity);
                // Auto-destroy VFX after duration
                float duration = 2f;
                var ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null) duration = ps.main.duration;
                Destroy(vfx, duration);
            }
            dmgControl.OnCanonDestroyed();
        }
        // Disable the cannon
        gameObject.SetActive(false);
    }
    #endregion

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
            // Scale both X, Y by half the length, Z by full length
            Vector3 newScale = new Vector3(currentLaserScale / 2f, currentLaserScale / 2f, currentLaserScale);
            laserVFX.transform.localScale = newScale;
        }
    }

    #region Targeting and Firing Logic
    private void HandleTargeting()
    {
        if (!gameObject.activeInHierarchy) return;
        if (enemy == null)
        {
            isTargetLocked = false;
            isPlayerInRotationLimit = false;
            rotationLimitTimer = 0f;
            return;
        }

        float distanceToEnemy = Vector3.Distance(transform.position, enemy.position);

        // Check if player is in rotation limit position
        bool canAimAtPlayer = CheckIfCanAimAtPlayer();
        
        if (!canAimAtPlayer && !isPlayerInRotationLimit)
        {
            Debug.Log($"[SmallCanonControl] {gameObject.name}: 🚫 Player in rotation limit position - aborting target");
            isPlayerInRotationLimit = true;
            rotationLimitTimer = 0f;
            isTargetLocked = false;
            StopLaserVFX();
            return;
        }

        // If player was in rotation limit, wait before re-targeting
        if (isPlayerInRotationLimit)
        {
            rotationLimitTimer += Time.deltaTime;
            if (rotationLimitTimer < ROTATION_LIMIT_DELAY)
            {
                return; // Still waiting
            }
            else
            {
                Debug.Log($"[SmallCanonControl] {gameObject.name}: 🔄 Rotation limit delay expired - checking for new target");
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

    private bool CheckIfCanAimAtPlayer()
    {
        if (enemy == null) return false;

        // Calculate the required rotations to aim at the player
        Vector3 targetDirection = enemy.position - body.position;
        targetDirection.y = 0; // Ignore height difference for horizontal rotation
        
        if (targetDirection == Vector3.zero) return false;
        
        // Check body rotation limits
        Quaternion targetBodyRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        Quaternion relativeRotation = Quaternion.Inverse(initialBodyRotation) * targetBodyRotation;
        float bodyAngle = relativeRotation.eulerAngles.y;
        if (bodyAngle > 180f) bodyAngle -= 360f;
        
        // Check if body angle exceeds limits
        if (Mathf.Abs(bodyAngle) > maxBodyRotationAngle)
        {
            Debug.Log($"[SmallCanonControl] {gameObject.name}: Body rotation limit exceeded: {bodyAngle:F1}° (max: {maxBodyRotationAngle}°)");
            return false;
        }
        
        // Check joint rotation limits
        Vector3 worldDirToTarget = enemy.position - joint.position;
        if (worldDirToTarget == Vector3.zero) return false;
        
        Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirToTarget, body.up);
        Quaternion targetLocalRotation = Quaternion.Inverse(body.rotation) * targetWorldRotation;
        targetLocalRotation.y = 0;
        targetLocalRotation.z = 0;
        
        float jointPitch = targetLocalRotation.eulerAngles.x;
        if (jointPitch > 180f) jointPitch -= 360f;
        
        // Check if joint pitch exceeds limits
        if (Mathf.Abs(jointPitch) > maxJointRotationAngle)
        {
            Debug.Log($"[SmallCanonControl] {gameObject.name}: Joint rotation limit exceeded: {jointPitch:F1}° (max: {maxJointRotationAngle}°)");
            return false;
        }
        
        return true;
    }

    private void HandleRotationAndFiring()
    {
        if (!gameObject.activeInHierarchy) return;
        if (isTargetLocked && enemy != null)
        {
            RotateToTarget();
            // Draw a red debug ray to visualize the raycast
            Debug.DrawRay(gunBarrel.position, gunBarrel.forward * fireRange, Color.red);
            RaycastHit hit;
            bool didHit = Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, fireRange, hittableLayers);
            if (didHit && hit.transform.CompareTag("Player"))
            {
                // Set endpoint to the hit point in the VFX object's local space
                if (laserEndPoint != null && laserVFX != null)
                    laserEndPoint.localPosition = laserVFX.transform.InverseTransformPoint(hit.point);
                float length = Vector3.Distance(gunBarrel.position, hit.point);
                // Enable the parent VFX GameObject if not already enabled (use laserVFXPrefab)
                if (laserVFXPrefab != null && !laserVFXPrefab.activeSelf)
                    laserVFXPrefab.SetActive(true);
                PlayLaserVFX(length);
                // Apply damage at interval
                laserDamageTimer += Time.deltaTime;
                if (laserDamageTimer >= laserDamageInterval)
                {
                    laserDamageTimer = 0f;
                    PlaneStats playerStats = hit.transform.GetComponent<PlaneStats>();
                    if (playerStats != null)
                        playerStats.TakeDamage((int)damage);
                }
            }
            else
            {
                // Disable the parent VFX GameObject if enabled (use laserVFXPrefab)
                if (laserVFXPrefab != null && laserVFXPrefab.activeSelf)
                    laserVFXPrefab.SetActive(false);
                StopLaserVFX();
                laserDamageTimer = 0f;
            }
        }
        else
        {
            // Disable the parent VFX GameObject if enabled (use laserVFXPrefab)
            if (laserVFXPrefab != null && laserVFXPrefab.activeSelf)
                laserVFXPrefab.SetActive(false);
            StopLaserVFX();
            laserDamageTimer = 0f;
        }
    }
    #endregion

    #region Rotation
    private void RotateToTarget()
    {
        if (enemy == null) return;
        // --- 1. Body Yaw (Horizontal, Y-axis) ---
        Vector3 targetDirection = enemy.position - body.position;
        targetDirection.y = 0; // Ignore height difference for horizontal rotation.
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
        // --- 2. Joint Pitch (Vertical, X-axis) ---
        Vector3 worldDirToTarget = enemy.position - joint.position;
        if (worldDirToTarget != Vector3.zero)
        {
            Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirToTarget, body.up);
            Quaternion targetLocalRotation = Quaternion.Inverse(body.rotation) * targetWorldRotation;
            targetLocalRotation.y = 0;
            targetLocalRotation.z = 0;
            // Clamp pitch (X) to maxJointRotationAngle
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
    #endregion

    #region Debug Methods
    [ContextMenu("Debug Targeting Status")]
    public void DebugTargetingStatus()
    {
        Debug.Log($"[SmallCanonControl] {gameObject.name}: === TARGETING STATUS ===");
        Debug.Log($"  Enemy Target: {(enemy != null ? enemy.name : "None")}");
        Debug.Log($"  Is Target Locked: {isTargetLocked}");
        Debug.Log($"  Is Player In Rotation Limit: {isPlayerInRotationLimit}");
        Debug.Log($"  Rotation Limit Timer: {rotationLimitTimer:F1}s / {ROTATION_LIMIT_DELAY}s");
        Debug.Log($"  Fire Range: {fireRange}");
        
        if (enemy != null)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);
            bool canAim = CheckIfCanAimAtPlayer();
            Debug.Log($"  Distance to Target: {distance:F2}");
            Debug.Log($"  In Range: {distance <= fireRange}");
            Debug.Log($"  Can Aim At Player: {canAim}");
        }
        
        Debug.Log($"  Current HP: {currentHP}/{maxHP}");
        Debug.Log($"  Track Player Instantly: {trackPlayerInstantly}");
        Debug.Log($"  Hittable Layers: {hittableLayers.value}");
        Debug.Log($"  Max Body Rotation: {maxBodyRotationAngle}°");
        Debug.Log($"  Max Joint Rotation: {maxJointRotationAngle}°");
        Debug.Log("=== END STATUS ===");
    }
    #endregion

    private void ResetToDefaultRotation()
    {
        // Smoothly reset body and joint to their initial rotations
        body.rotation = Quaternion.Slerp(body.rotation, initialBodyRotation, maxRotationSpeed * Time.deltaTime);
        joint.localRotation = Quaternion.Slerp(joint.localRotation, initialJointLocalRotation, maxRotationSpeed * Time.deltaTime);
    }
}
