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
    
    void Start()
    {
        InitializeReferences();
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
            if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            {
                weaponManager = GameManager.Instance.currentPlayer.GetComponent<PlayerWeaponManager>();
            }
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
                // Bolt: Optimized - Maintain public distance field, but use sqrMagnitude for internal logic
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
    
    // Bolt: Optimized - Replaced expensive FindGameObjectsWithTag and Distance with cached references and sqrMagnitude
    void FindEnemiesInRange()
    {
        enemiesInRange.Clear();
        
        if (weaponManager == null) return;
        
        float sqrRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        Vector3 myPos = transform.position;

        foreach (string tag in targetTags)
        {
            // Bolt: Optimized - Use GameManager to avoid expensive FindGameObjectsWithTag for "Enemy"
            if (tag == "Enemy" && GameManager.Instance != null)
            {
                GameObject boss = GameManager.Instance.currentBoss;
                if (boss != null && boss.activeInHierarchy)
                {
                    if ((boss.transform.position - myPos).sqrMagnitude <= sqrRange)
                        enemiesInRange.Add(boss.transform);
                }

                var ships = GameManager.Instance.GetActiveEnemyShips();
                if (ships != null)
                {
                    for (int i = 0; i < ships.Count; i++)
                    {
                        GameObject ship = ships[i];
                        if (ship != null && ship.activeInHierarchy)
                        {
                            if ((ship.transform.position - myPos).sqrMagnitude <= sqrRange)
                                enemiesInRange.Add(ship.transform);
                        }
                    }
                }
            }
            else
            {
                GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject obj in candidates)
                {
                    if (obj == null || !obj.activeInHierarchy) continue;
                    if ((obj.transform.position - myPos).sqrMagnitude <= sqrRange)
                    {
                        enemiesInRange.Add(obj.transform);
                    }
                }
            }
        }
    }
    
    // Bolt: Optimized - Use sqrMagnitude for target selection to avoid square root cost
    void TryLockNewTarget()
    {
        Transform bestTarget = null;
        float bestDistanceSqr = float.MaxValue;
        float sqrRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        Vector3 myPos = transform.position;
        
        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            
            if (!IsInLockCircle(enemy)) continue;
            
            Transform lockTarget = GetLockableTarget(enemy);
            
            if (lockTarget != null)
            {
                float distSqr = (lockTarget.position - myPos).sqrMagnitude;
                if (distSqr <= sqrRange && distSqr < bestDistanceSqr)
                {
                    if (!requireLineOfSight || HasLineOfSight(lockTarget))
                    {
                        bestTarget = lockTarget;
                        bestDistanceSqr = distSqr;
                    }
                }
            }
        }
        
        if (bestTarget != null)
        {
            LockTarget(bestTarget);
        }
    }

    // Bolt: Optimized - Use TryGetComponent and direct transform checks to reduce GetComponentInParent overhead
    private Transform GetLockableTarget(Transform enemy)
    {
        if (enemy.TryGetComponent<EnemyStats>(out _)) return enemy;
        if (enemy.TryGetComponent<TurretControl>(out _)) return enemy;
        if (enemy.TryGetComponent<SmallCanonControl>(out _)) return enemy;
        if (enemy.TryGetComponent<BigCanon>(out _)) return enemy;

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
        RaycastHit hit;
        
        if (Physics.Raycast(targetingCamera.transform.position, directionToTarget.normalized, out hit, distance, obstacleLayer))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        
        return true;
    }

    // Bolt: Optimized - Use TryGetComponent
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
    
    // Bolt: Optimized - Use sqrMagnitude
    bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;
        if (weaponManager == null) return false;
        
        float sqrRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        if ((target.position - transform.position).sqrMagnitude > sqrRange) return false;
        
        if (targetingCamera == null) return false;
        Vector3 viewportPos = targetingCamera.WorldToViewportPoint(target.position);
        if (viewportPos.z <= 0) return false;
        
        if (requireLineOfSight && !HasLineOfSight(target))
        {
            return false;
        }
        
        return true;
    }
    
    // Bolt: Optimized - Use squared distance check for lock circle
    bool IsInLockCircle(Transform target)
    {
        if (targetingCamera == null) return false;
        
        Vector3 viewportPos = targetingCamera.WorldToViewportPoint(target.position);
        
        if (viewportPos.z <= 0) return false;
        
        float dx = viewportPos.x - 0.5f;
        float dy = viewportPos.y - 0.5f;
        float distSqr = dx * dx + dy * dy;
        
        return distSqr <= lockCircleRadius * lockCircleRadius;
    }
    
    // Bolt: Optimized - Updated distance for UI
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
