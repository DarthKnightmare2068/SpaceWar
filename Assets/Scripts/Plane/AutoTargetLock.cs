using UnityEngine;
using System.Collections.Generic;

public class AutoTargetLock : MonoBehaviour
{
    [Header("Targeting Settings")]
    public Camera targetingCamera; // The camera used for targeting (usually main camera)
    public string[] targetTags; // Tags for targetable objects (set in inspector)
    public LayerMask enemyLayer = -1; // Layer mask for enemies (optional)
    
    [Header("Lock Circle Settings")]
    [Range(0.01f, 0.5f)]
    public float lockCircleRadius = 0.1f; // Radius in viewport coordinates (0.1 = 10% of screen)
    
    [Header("Lock Behavior")]
    public bool requireLineOfSight = true; // Check if there's line of sight to target
    public LayerMask obstacleLayer = -1; // What layers block line of sight
    
    [Header("References")]
    public PlayerWeaponManager weaponManager; // Reference to weapon manager for ranges
    
    [Header("Current Lock Status")]
    public Transform lockedTarget; // Currently locked target
    public float distanceToTarget; // Distance to locked target
    public bool isTargetInLockCircle; // Is target still in lock circle
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // Events for other systems to subscribe to
    public System.Action<Transform> OnTargetLocked;
    public System.Action<Transform> OnTargetLost;
    
    private List<Transform> enemiesInRange = new List<Transform>();
    
    void Start()
    {
        // Get main camera if not assigned
        if (targetingCamera == null)
            targetingCamera = Camera.main;
            
        if (targetingCamera == null)
            targetingCamera = FindObjectOfType<Camera>();
            
        // Get PlayerWeaponManager if not assigned
        if (weaponManager == null)
            weaponManager = FindObjectOfType<PlayerWeaponManager>();
    }
    
    void Update()
    {
        if (targetingCamera == null || weaponManager == null) return;
        
        // Find all enemies in range
        FindEnemiesInRange();
        
        // Check if current target is still valid
        if (lockedTarget != null)
        {
            if (!IsTargetValid(lockedTarget))
            {
                LoseTarget();
            }
            else
            {
                // Update target info
                distanceToTarget = Vector3.Distance(transform.position, lockedTarget.position);
                isTargetInLockCircle = IsInLockCircle(lockedTarget);
                
                // Lose target if it leaves the lock circle
                if (!isTargetInLockCircle)
                {
                    LoseTarget();
                }
            }
        }
        
        // Try to acquire new target if we don't have one
        if (lockedTarget == null)
        {
            TryLockNewTarget();
        }
    }
    
    void FindEnemiesInRange()
    {
        enemiesInRange.Clear();
        
        // Find all objects with any of the target tags
        foreach (string tag in targetTags)
        {
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in candidates)
            {
                if (obj == null) continue;
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance <= weaponManager.missileFireRange) // Use missile range for target acquisition
                {
                    enemiesInRange.Add(obj.transform);
                }
            }
        }
    }
    
    void TryLockNewTarget()
    {
        Transform bestTarget = null;
        float bestScore = float.MaxValue;
        string bestTargetType = "None";
        
        foreach (Transform enemy in enemiesInRange)
        {
            // Always use the parent with EnemyStats, TurretControl, SmallCanonControl, or BigCanon for locking
            var enemyStats = enemy.GetComponentInParent<EnemyStats>();
            var turret = enemy.GetComponentInParent<TurretControl>();
            var smallCanon = enemy.GetComponentInParent<SmallCanonControl>();
            var bigCanon = enemy.GetComponentInParent<BigCanon>();
            Transform lockTarget = null;
            if (enemyStats != null) lockTarget = enemyStats.transform;
            else if (turret != null) lockTarget = turret.transform;
            else if (smallCanon != null) lockTarget = smallCanon.transform;
            else if (bigCanon != null) lockTarget = bigCanon.transform;
            
            if (lockTarget != null)
            {
                float distance = Vector3.Distance(transform.position, lockTarget.position);
                if (distance <= weaponManager.missileFireRange)
                {
                    LockTarget(lockTarget);
                    return;
                }
            }
        }
        
        if (bestTarget != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[AutoTargetLock] Locking onto {bestTargetType}: {bestTarget.name} (Score: {bestScore:F2})");
            }
            LockTarget(bestTarget);
        }
    }
    
    bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;
        
        // Check distance using weapon manager ranges
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > weaponManager.missileFireRange) return false;
        
        // Check if target is in front of camera
        Vector3 viewportPos = targetingCamera.WorldToViewportPoint(target.position);
        if (viewportPos.z <= 0) return false; // Behind camera
        
        // Check line of sight if required
        if (requireLineOfSight)
        {
            Vector3 directionToTarget = target.position - targetingCamera.transform.position;
            RaycastHit hit;
            
            if (Physics.Raycast(targetingCamera.transform.position, directionToTarget.normalized, out hit, distance, obstacleLayer))
            {
                if (hit.transform != target) return false; // Something is blocking
            }
        }
        
        return true;
    }
    
    bool IsInLockCircle(Transform target)
    {
        Vector3 viewportPos = targetingCamera.WorldToViewportPoint(target.position);
        
        // Check if target is in front of camera
        if (viewportPos.z <= 0) return false;
        
        // Calculate distance from center of screen
        float distanceFromCenter = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
        
        return distanceFromCenter <= lockCircleRadius;
    }
    
    void LockTarget(Transform target)
    {
        if (lockedTarget == target) return; // Already locked
        
        // Debug: Show target type change
        string newTargetType = "Unknown";
        var turret = target.GetComponentInParent<TurretControl>();
        var enemyStats = target.GetComponentInParent<EnemyStats>();
        var smallCanon = target.GetComponentInParent<SmallCanonControl>();
        var bigCanon = target.GetComponentInParent<BigCanon>();
        if (turret != null) newTargetType = "Turret";
        else if (smallCanon != null) newTargetType = "Small Cannon";
        else if (bigCanon != null) newTargetType = "Big Cannon";
        else if (enemyStats != null) newTargetType = "Enemy Ship";
        
        if (showDebugInfo)
        {
            string oldTargetType = "None";
            if (lockedTarget != null)
            {
                var oldTurret = lockedTarget.GetComponentInParent<TurretControl>();
                var oldEnemyStats = lockedTarget.GetComponentInParent<EnemyStats>();
                var oldSmallCanon = lockedTarget.GetComponentInParent<SmallCanonControl>();
                var oldBigCanon = lockedTarget.GetComponentInParent<BigCanon>();
                if (oldTurret != null) oldTargetType = "Turret";
                else if (oldSmallCanon != null) oldTargetType = "Small Cannon";
                else if (oldBigCanon != null) oldTargetType = "Big Cannon";
                else if (oldEnemyStats != null) oldTargetType = "Enemy Ship";
            }
            Debug.Log($"[AutoTargetLock] Target changed: {oldTargetType} -> {newTargetType} ({target.name})");
        }
        
        lockedTarget = target;
        distanceToTarget = Vector3.Distance(transform.position, target.position);
        isTargetInLockCircle = true;
        
        OnTargetLocked?.Invoke(target);
    }
    
    void LoseTarget()
    {
        if (lockedTarget == null) return;
        
        Transform lostTarget = lockedTarget;
        lockedTarget = null;
        distanceToTarget = 0f;
        isTargetInLockCircle = false;
        
        OnTargetLost?.Invoke(lostTarget);
    }
    
    // Public methods for other scripts
    public bool HasTarget()
    {
        return lockedTarget != null;
    }
    
    public Transform GetLockedTarget()
    {
        return lockedTarget;
    }
    
    public Vector3 GetTargetPosition()
    {
        return lockedTarget != null ? lockedTarget.position : Vector3.zero;
    }
    
    public string GetCurrentTargetType()
    {
        if (lockedTarget == null) return "None";
        
        var turret = lockedTarget.GetComponentInParent<TurretControl>();
        var enemyStats = lockedTarget.GetComponentInParent<EnemyStats>();
        var smallCanon = lockedTarget.GetComponentInParent<SmallCanonControl>();
        var bigCanon = lockedTarget.GetComponentInParent<BigCanon>();
        
        if (turret != null) return "Turret";
        if (smallCanon != null) return "Small Cannon";
        if (bigCanon != null) return "Big Cannon";
        if (enemyStats != null) return "Enemy Ship";
        return "Unknown";
    }
    
    public bool IsValidTarget(Transform target)
    {
        if (!HasTarget()) return false;
        if (target == null) return false;
        // Always compare using the parent with EnemyStats, TurretControl, SmallCanonControl, or BigCanon
        var enemy = target.GetComponentInParent<EnemyStats>();
        var turret = target.GetComponentInParent<TurretControl>();
        var smallCanon = target.GetComponentInParent<SmallCanonControl>();
        var bigCanon = target.GetComponentInParent<BigCanon>();
        Transform rootTarget = null;
        if (enemy != null) rootTarget = enemy.transform;
        else if (turret != null) rootTarget = turret.transform;
        else if (smallCanon != null) rootTarget = smallCanon.transform;
        else if (bigCanon != null) rootTarget = bigCanon.transform;
        else rootTarget = target;
        
        return lockedTarget == rootTarget;
    }
    
    public void ForceUnlock()
    {
        LoseTarget();
    }
    
    // Draw the lock circle in scene view for debugging
    void OnDrawGizmos()
    {
        if (targetingCamera == null || !showDebugInfo) return;
        
        // Draw lock circle in scene view (approximate)
        Gizmos.color = lockedTarget != null ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(targetingCamera.transform.position + targetingCamera.transform.forward * 10f, lockCircleRadius * 5f);
        
        // Draw line to locked target
        if (lockedTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(targetingCamera.transform.position, lockedTarget.position);
        }
    }
} 