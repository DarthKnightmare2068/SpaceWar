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
    private Collider[] scanResults = new Collider[20]; // Bolt: Pre-allocated array for OverlapSphereNonAlloc
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
        
        if (weaponManager == null) return;
        
        // Bolt: Optimized using Physics.OverlapSphereNonAlloc instead of scene-wide FindGameObjectsWithTag
        int count = Physics.OverlapSphereNonAlloc(transform.position, weaponManager.missileFireRange, scanResults, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Collider col = scanResults[i];
            if (col == null) continue;

            // Replicate tag check if necessary, or just rely on LayerMask
            bool tagMatch = false;
            foreach (string tag in targetTags)
            {
                if (col.CompareTag(tag))
                {
                    tagMatch = true;
                    break;
                }
            }

            if (tagMatch)
            {
                enemiesInRange.Add(col.transform);
            }
            else
            {
                // Bolt: Check parent for tag if collider is on a child object (common for complex ships/turrets)
                Transform parent = col.transform.parent;
                while (parent != null)
                {
                    foreach (string tag in targetTags)
                    {
                        if (parent.CompareTag(tag))
                        {
                            tagMatch = true;
                            break;
                        }
                    }
                    if (tagMatch)
                    {
                        enemiesInRange.Add(parent);
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
    }
    
    void TryLockNewTarget()
    {
        Transform bestTarget = null;
        float bestsqrDistance = float.MaxValue;
        float sqrFireRange = weaponManager.missileFireRange * weaponManager.missileFireRange;
        
        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            
            if (!IsInLockCircle(enemy)) continue;
            
            Transform lockTarget = GetLockableTarget(enemy);
            
            if (lockTarget != null)
            {
                // Bolt: Use sqrMagnitude to avoid square root
                float sqrDistance = (transform.position - lockTarget.position).sqrMagnitude;
                if (sqrDistance <= sqrFireRange && sqrDistance < bestsqrDistance)
                {
                    if (!requireLineOfSight || HasLineOfSight(lockTarget))
                    {
                        bestTarget = lockTarget;
                        bestsqrDistance = sqrDistance;
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
        
        // Bolt: Use sqrMagnitude to avoid square root
        float sqrDistance = (transform.position - target.position).sqrMagnitude;
        if (sqrDistance > weaponManager.missileFireRange * weaponManager.missileFireRange) return false;
        
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
        
        float distanceFromCenter = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
        
        return distanceFromCenter <= lockCircleRadius;
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
