using System.Collections.Generic;
using UnityEngine;

public class TurretsManager : MonoBehaviour
{
    [Tooltip("Max number of turrets that can lock on a single player")]
    public int maxTurretsPerPlayer = 2;
    [Tooltip("All your turret instances")]
    public List<TurretControl> turrets = new List<TurretControl>();
    [Tooltip("Bullet speed for all turrets")]
    public float bulletSpeed = 100f;
    [Tooltip("HP for all turrets")]
    public int turretHP = 5246;
    [Tooltip("VFX prefab to play when a turret is destroyed")]
    public GameObject turretDestroyedVFX;
    [Header("Revive Settings")]
    [Tooltip("Initial number of turrets at start")]
    public int maxTurretCount = 0;
    [Tooltip("Current number of turrets alive")]
    public int currentTurretCount = 0;

    [Header("Tracking Mode")]
    public bool trackPlayerInstantly = false;

    private float howCloseToPlayer;
    private List<Transform> players = new List<Transform>();
    private Dictionary<TurretControl, Transform> turretTargets = new Dictionary<TurretControl, Transform>();

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;

    private float targetingUpdateTimer = 0f;
    private const float TARGETING_UPDATE_INTERVAL = 0.2f;

    private float howCloseToPlayerSqr;
    private HashSet<TurretControl> internalAssignedTurrets = new HashSet<TurretControl>();
    private List<TurretControl> internalSortedTurrets = new List<TurretControl>();

    private WeaponDmgControl cachedDmgControl;

    void Awake()
    {
        turrets = new List<TurretControl>(GetComponentsInChildren<TurretControl>(true));

        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
        {
            cachedDmgControl = FindObjectOfType<WeaponDmgControl>();
        }
        
        if (cachedDmgControl != null)
        {
            howCloseToPlayer = cachedDmgControl.GetTurretFireRange();
        }
        else
        {
            howCloseToPlayer = 100f;
        }
        howCloseToPlayerSqr = howCloseToPlayer * howCloseToPlayer;

        SetAllTurretsHP();
        maxTurretCount = turrets.Count;
        currentTurretCount = maxTurretCount;

        foreach (var turret in turrets)
        {
            if (turret != null)
                turret.SetTrackingMode(trackPlayerInstantly);
        }
    }

    void Update()
    {
        // Bolt: Optimized - Throttle heavy targeting logic to 5Hz to reduce CPU overhead
        targetingUpdateTimer += Time.deltaTime;
        if (targetingUpdateTimer >= TARGETING_UPDATE_INTERVAL)
        {
            targetingUpdateTimer = 0f;
            CleanTurretList();
            currentTurretCount = turrets.Count;
            UpdatePlayersList();
            AssignTurretsToPlayers();
        }

        // Bolt: Optimized - Smooth tracking and rotation must happen every frame using cached targets
        foreach (var turret in turrets)
        {
            if (turret != null)
            {
                if (turretTargets.TryGetValue(turret, out Transform target))
                {
                    turret.ControlTurret(target, howCloseToPlayer);
                }
                else
                {
                    turret.ControlTurret(null, howCloseToPlayer);
                }
                turret.SetTrackingMode(trackPlayerInstantly);
            }
        }

        backupRefreshTimer += Time.deltaTime;
        if (backupRefreshTimer >= BACKUP_REFRESH_INTERVAL)
        {
            backupRefreshTimer = 0f;
            ForceRefreshAllTurretTargeting();
        }
    }

    public void CleanTurretList()
    {
        turrets.RemoveAll(t => t == null);
    }

    public void SetAllTurretsHP()
    {
        foreach (var turret in turrets)
        {
            if (turret != null)
            {
                turret.maxHP = turretHP;
                turret.currentHP = turretHP;
            }
        }
    }

    void UpdatePlayersList()
    {
        players.Clear();
        // Bolt: Optimized - Use GameManager instance for faster player access instead of scene-wide search
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            GameObject playerObj = GameManager.Instance.currentPlayer;
            var stats = playerObj.GetComponent<PlaneStats>();
            if (stats != null && stats.CurrentHP > 0)
            {
                // Bolt: Optimized - Use sqrMagnitude to eliminate expensive square root calculations
                float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
                if (sqrDist < howCloseToPlayerSqr)
                {
                    players.Add(playerObj.transform);
                }
            }
        }
    }

    void AssignTurretsToPlayers()
    {
        turretTargets.Clear();
        // Bolt: Optimized - Reuse internal collections to eliminate per-update allocations and reduce GC pressure
        internalAssignedTurrets.Clear();

        foreach (var player in players)
        {
            // Skip destroyed players to avoid MissingReferenceException when accessing position
            if (player == null) continue;
            
            internalSortedTurrets.Clear();
            internalSortedTurrets.AddRange(turrets);
            // Bolt: Optimized - Replace Vector3.Distance with sqrMagnitude in sort comparer for better performance
            internalSortedTurrets.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float daSqr = (a.transform.position - player.position).sqrMagnitude;
                float dbSqr = (b.transform.position - player.position).sqrMagnitude;
                return daSqr.CompareTo(dbSqr);
            });

            int assigned = 0;
            foreach (var turret in internalSortedTurrets)
            {
                if (turret == null || internalAssignedTurrets.Contains(turret)) continue;
                turretTargets[turret] = player;
                internalAssignedTurrets.Add(turret);
                // ControlTurret is now called every frame in Update() using cached targets
                assigned++;
                if (assigned >= maxTurretsPerPlayer) break;
            }
        }
    }

    private void ForceRefreshAllTurretTargeting()
    {
        turretTargets.Clear();
        
        foreach (var turret in turrets)
        {
            if (turret != null && turret.gameObject.activeInHierarchy)
            {
                turret.ControlTurret(null, howCloseToPlayer);
            }
        }
        
        UpdatePlayersList();
        AssignTurretsToPlayers();
    }
}
