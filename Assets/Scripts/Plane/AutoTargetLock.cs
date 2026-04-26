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
    private HashSet<Transform> enemiesInRangeSet = new HashSet<Transform>();
    private float nextEnemyScanTime = 0f;
    private bool isInitialized = false;

    private float sqrMissileFireRange;
    private float sqrLockCircleRadius;

    private Collider[] scanBuffer = new Collider[128];
    private bool scanBufferSaturationWarned = false;

    void Start()
    {
        InitializeReferences();
    }

    private void RefreshCachedRanges()
    {
        if (weaponManager != null)
            sqrMissileFireRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        sqrLockCircleRadius = lockCircleRadius * lockCircleRadius;
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

        if (isInitialized) RefreshCachedRanges();
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
    
    void FindEnemiesInRange()
    {
        enemiesInRange.Clear();
        enemiesInRangeSet.Clear();

        if (weaponManager == null) return;

        RefreshCachedRanges();

        // enemyLayer defaults to -1 (Everything) so the spatial query has parity with the
        // old FindGameObjectsWithTag scan when no dedicated Enemy layer is configured.
        // Tag filtering below is the actual enemy filter — assigning a real Enemy layer
        // in the prefab would tighten this query and improve perf further.
        int mask = enemyLayer.value == 0 ? -1 : enemyLayer.value;

        int count = Physics.OverlapSphereNonAlloc(transform.position, weaponManager.missileFireRange, scanBuffer, mask);

        if (count == scanBuffer.Length)
        {
            if (!scanBufferSaturationWarned)
            {
                Debug.LogWarning($"[AutoTargetLock] OverlapSphere scan buffer saturated at {scanBuffer.Length}; doubling and re-scanning. Consider assigning a dedicated Enemy layer to reduce hit count.");
                scanBufferSaturationWarned = true;
            }
            scanBuffer = new Collider[scanBuffer.Length * 2];
            count = Physics.OverlapSphereNonAlloc(transform.position, weaponManager.missileFireRange, scanBuffer, mask);
        }

        for (int i = 0; i < count; i++)
        {
            Transform candidate = scanBuffer[i].transform;
            if (candidate == null) continue;

            Transform tagged = FindTaggedAncestor(candidate);
            if (tagged != null) enemiesInRangeSet.Add(tagged);
        }

        foreach (Transform t in enemiesInRangeSet) enemiesInRange.Add(t);
    }

    private Transform FindTaggedAncestor(Transform t)
    {
        if (targetTags == null) return null;
        for (int i = 0; i < targetTags.Length; i++)
        {
            string tag = targetTags[i];
            if (string.IsNullOrEmpty(tag)) continue;
            Transform p = t;
            while (p != null)
            {
                if (p.CompareTag(tag)) return p;
                p = p.parent;
            }
        }
        return null;
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
                float sqrDistance = (lockTarget.position - transform.position).sqrMagnitude;
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
        if (enemy.TryGetComponent<EnemyStats>(out var enemyStats) || (enemyStats = enemy.GetComponentInParent<EnemyStats>()) != null)
            return enemyStats.transform;

        if (enemy.TryGetComponent<TurretControl>(out var turret) || (turret = enemy.GetComponentInParent<TurretControl>()) != null)
            return turret.transform;

        if (enemy.TryGetComponent<SmallCanonControl>(out var smallCanon) || (smallCanon = enemy.GetComponentInParent<SmallCanonControl>()) != null)
            return smallCanon.transform;

        if (enemy.TryGetComponent<BigCanon>(out var bigCanon) || (bigCanon = enemy.GetComponentInParent<BigCanon>()) != null)
            return bigCanon.transform;

        return null;
    }

    private bool HasLineOfSight(Transform target)
    {
        if (targetingCamera == null) return false;
        
        Vector3 directionToTarget = target.position - targetingCamera.transform.position;
        float distance = directionToTarget.magnitude;
        RaycastHit hit;
        
        if (Physics.Raycast(targetingCamera.transform.position, directionToTarget.normalized, out hit, distance, obstacleLayer))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        
        return true;
    }

    private string GetTargetTypeString(Transform target)
    {
        if (target == null) return "None";

        if (target.TryGetComponent<TurretControl>(out _)) return "Turret";
        if (target.TryGetComponent<SmallCanonControl>(out _)) return "Small Cannon";
        if (target.TryGetComponent<BigCanon>(out _)) return "Big Cannon";
        if (target.TryGetComponent<EnemyStats>(out _)) return "Enemy Ship";
        if (target.TryGetComponent<MainBossStats>(out _)) return "Main Boss";

        return "Unknown";
    }
    
    bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;
        if (weaponManager == null) return false;

        float sqrDistance = (target.position - transform.position).sqrMagnitude;
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

        float dx = viewportPos.x - 0.5f;
        float dy = viewportPos.y - 0.5f;
        return (dx * dx + dy * dy) <= sqrLockCircleRadius;
    }
    
    void LockTarget(Transform target)
    {
        if (lockedTarget == target) return;
        
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
