using UnityEngine;
using System.Collections.Generic;

public class AutoTargetLock : MonoBehaviour
{
    [Header("Targeting Settings")]
    public Camera targetingCamera;
    public string[] targetTags;
    public LayerMask enemyLayer = -1;
    
    [Header("Lock Circle Settings")]
    [Range(0.01f, 0.5f)]
    public float lockCircleRadius = 0.1f;
    
    [Header("Lock Behavior")]
    public bool requireLineOfSight = true;
    public LayerMask obstacleLayer = -1;
    
    [Header("References")]
    public PlayerWeaponManager weaponManager;
    
    [Header("Current Lock Status")]
    public Transform lockedTarget;
    public float distanceToTarget;
    public bool isTargetInLockCircle;
    
    [Header("Performance Settings")]
    [SerializeField] private float enemyScanInterval = 0.2f;
    
    public System.Action<Transform> OnTargetLocked;
    public System.Action<Transform> OnTargetLost;
    
    private List<Transform> enemiesInRange = new List<Transform>();
    private float nextEnemyScanTime = 0f;
    private bool isInitialized = false;

    // Bolt: Optimized - Pre-allocated buffer to eliminate per-scan allocations.
    // Limit is 64 colliders per scan for performance/memory balance.
    private Collider[] scanBuffer = new Collider[64];
    private float sqrMissileFireRange;
    
    void Start()
    {
        InitializeReferences();
        InitializeOptimization();
    }

    private void InitializeOptimization()
    {
        if (weaponManager != null)
        {
            sqrMissileFireRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        }
    }

    private void InitializeReferences()
    {
        if (targetingCamera == null)
        {
            targetingCamera = GetComponentInChildren<Camera>();
        }
        if (targetingCamera == null)
        {
            targetingCamera = Camera.main;
        }
            
        if (weaponManager == null)
        {
            weaponManager = GetComponent<PlayerWeaponManager>();
        }
        if (weaponManager == null)
        {
            GameEntityRegistry.TryGetPlayerComponent(out weaponManager);
        }
        
        isInitialized = (targetingCamera != null && weaponManager != null);
    }
    
    void Update()
    {
        if (!isInitialized)
        {
            InitializeReferences();
            if (!isInitialized) return;
        }
        
        if (Time.time >= nextEnemyScanTime)
        {
            FindEnemiesInRange();
            nextEnemyScanTime = Time.time + enemyScanInterval;
        }
        
        if (lockedTarget != null)
        {
            if (!IsTargetValid(lockedTarget))
            {
                LoseTarget();
            }
            else
            {
                // We keep Vector3.Distance for the public field used by UI, but use sqrMagnitude for internal logic
                distanceToTarget = Vector3.Distance(transform.position, lockedTarget.position);
                isTargetInLockCircle = IsInLockCircle(lockedTarget);
                
                if (!isTargetInLockCircle)
                {
                    LoseTarget();
                }
            }
        }
        
        if (lockedTarget == null)
        {
            TryLockNewTarget();
        }
    }
    
    private HashSet<Transform> enemiesInRangeSet = new HashSet<Transform>();

    void FindEnemiesInRange()
    {
        enemiesInRange.Clear();
        enemiesInRangeSet.Clear();

        if (weaponManager == null || targetTags == null) return;
        
        // Bolt: Optimized - Replaced O(N) scene-wide tag search with O(M) local spatial partitioning query
        // Re-calculate squared range in case it changed (e.g. level up)
        sqrMissileFireRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        
        int count = Physics.OverlapSphereNonAlloc(transform.position, weaponManager.missileFireRange, scanBuffer, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider col = scanBuffer[i];
            if (col == null) continue;

            // Parent traversal to find tagged object (maintaining parity with original tag-based search)
            Transform curr = col.transform;
            while (curr != null)
            {
                // Bolt: Optimized - Use CompareTag to avoid string allocation GC pressure
                bool hasTargetTag = false;
                foreach (string tag in targetTags)
                {
                    if (curr.CompareTag(tag))
                    {
                        hasTargetTag = true;
                        break;
                    }
                }

                if (hasTargetTag)
                {
                    if (curr.gameObject.activeInHierarchy && enemiesInRangeSet.Add(curr))
                    {
                        enemiesInRange.Add(curr);
                    }
                    break;
                }
                curr = curr.parent;
            }
        }
    }
    
    void TryLockNewTarget()
    {
        Transform bestTarget = null;
        float bestSqrDistance = float.MaxValue;
        
        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            
            if (!IsInLockCircle(enemy)) continue;
            
            Transform lockTarget = GetLockableTarget(enemy);
            
            if (lockTarget != null)
            {
                // Bolt: Optimized - Use sqrMagnitude for internal distance comparisons to avoid sqrt
                float sqrDistance = (transform.position - lockTarget.position).sqrMagnitude;
                if (sqrDistance <= sqrMissileFireRange && sqrDistance < bestSqrDistance)
                {
                    if (!requireLineOfSight || HasLineOfSight(lockTarget))
                    {
                        bestTarget = lockTarget;
                        bestSqrDistance = sqrDistance;
                    }
                }
            }
        }
        
        if (bestTarget != null)
        {
            LockTarget(bestTarget);
        }
    }

    private Transform GetLockableTarget(Transform enemy)
    {
        var enemyStats = enemy.GetComponentInParent<EnemyStats>();
        if (enemyStats != null) return enemyStats.transform;
        
        var turret = enemy.GetComponentInParent<TurretControl>();
        if (turret != null) return turret.transform;
        
        var smallCanon = enemy.GetComponentInParent<SmallCanonControl>();
        if (smallCanon != null) return smallCanon.transform;
        
        var bigCanon = enemy.GetComponentInParent<BigCanon>();
        if (bigCanon != null) return bigCanon.transform;
        
        return null;
    }

    private bool HasLineOfSight(Transform target)
    {
        if (targetingCamera == null) return false;
        
        Vector3 directionToTarget = target.position - targetingCamera.transform.position;
        float distance = directionToTarget.magnitude;

        // Safety check for very close distances
        if (distance < 0.001f) return true;

        RaycastHit hit;
        
        if (Physics.Raycast(targetingCamera.transform.position, directionToTarget / distance, out hit, distance, obstacleLayer))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        
        return true;
    }

    private string GetTargetTypeString(Transform target)
    {
        if (target == null) return "None";
        
        if (target.GetComponent<TurretControl>() != null) return "Turret";
        if (target.GetComponent<SmallCanonControl>() != null) return "Small Cannon";
        if (target.GetComponent<BigCanon>() != null) return "Big Cannon";
        if (target.GetComponent<EnemyStats>() != null) return "Enemy Ship";
        if (target.GetComponent<MainBossStats>() != null) return "Main Boss";
        
        return "Unknown";
    }
    
    bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;
        if (weaponManager == null) return false;
        
        // Bolt: Optimized - Use sqrMagnitude for internal distance checks
        float sqrDistance = (transform.position - target.position).sqrMagnitude;
        if (sqrDistance > sqrMissileFireRange) return false;
        
        if (targetingCamera == null) return false;
        Vector3 viewportPos = targetingCamera.WorldToViewportPoint(target.position);
        if (viewportPos.z <= 0) return false;
        
        if (requireLineOfSight && !HasLineOfSight(target))
        {
            return false;
        }
        
        return true;
    }
    
    bool IsInLockCircle(Transform target)
    {
        if (targetingCamera == null) return false;
        
        Vector3 viewportPos = targetingCamera.WorldToViewportPoint(target.position);
        
        if (viewportPos.z <= 0) return false;
        
        // Bolt: Optimized - Use sqrMagnitude for viewport distance check
        float dx = viewportPos.x - 0.5f;
        float dy = viewportPos.y - 0.5f;
        float sqrDistanceFromCenter = dx * dx + dy * dy;
        
        return sqrDistanceFromCenter <= lockCircleRadius * lockCircleRadius;
    }
    
    void LockTarget(Transform target)
    {
        if (lockedTarget == target) return;
        
        lockedTarget = target;
        // Keep Vector3.Distance for public field (UI compatibility)
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
        return GetTargetTypeString(lockedTarget);
    }
    
    public bool IsValidTarget(Transform target)
    {
        if (!HasTarget()) return false;
        if (target == null) return false;
        
        Transform rootTarget = GetLockableTarget(target);
        if (rootTarget == null) rootTarget = target;
        
        return lockedTarget == rootTarget;
    }
    
    public void ForceUnlock()
    {
        LoseTarget();
    }

    public void ForceScan()
    {
        nextEnemyScanTime = 0f;
    }
}
