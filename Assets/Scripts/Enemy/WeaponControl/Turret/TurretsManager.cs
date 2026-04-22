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
    private float howCloseToPlayerSqr; // Bolt: Optimized - Pre-calculated squared range
    private List<Transform> players = new List<Transform>();
    private Dictionary<TurretControl, Transform> turretTargets = new Dictionary<TurretControl, Transform>();

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;

    private float targetingUpdateTimer = 0f; // Bolt: Optimized - Throttled targeting logic
    private const float TARGETING_UPDATE_INTERVAL = 0.2f; // 5Hz

    private WeaponDmgControl cachedDmgControl;

    // Bolt: Optimized - Reuse collections to avoid per-update allocations
    private List<TurretControl> internalSortedTurrets = new List<TurretControl>();
    private HashSet<TurretControl> internalAssignedTurrets = new HashSet<TurretControl>();

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

        UpdateTrackingMode();
    }

    private void UpdateTrackingMode()
    {
        foreach (var turret in turrets)
        {
            if (turret != null)
                turret.SetTrackingMode(trackPlayerInstantly);
        }
    }

    void Update()
    {
        CleanTurretList();
        currentTurretCount = turrets.Count;
        
        // Bolt: Optimized - Increment timers first
        targetingUpdateTimer += Time.deltaTime;
        backupRefreshTimer += Time.deltaTime;

        // Bolt: Optimized - Throttled heavy targeting logic (5Hz)
        if (targetingUpdateTimer >= TARGETING_UPDATE_INTERVAL)
        {
            targetingUpdateTimer = 0f;
            UpdatePlayersList();
            UpdateTurretAssignments();
        }

        // Bolt: Optimized - Lightweight per-frame tracking/shooting loop
        foreach (var turret in turrets)
        {
            if (turret == null) continue;

            Transform target = null;
            turretTargets.TryGetValue(turret, out target);
            turret.ControlTurret(target, howCloseToPlayer);
        }

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
        foreach(var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            var stats = playerObj.GetComponent<PlaneStats>();
            if(stats != null && stats.CurrentHP <= 0)
            {
                continue;
            }

            // Bolt: Optimized - Use sqrMagnitude instead of Distance
            float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
            if(sqrDist < howCloseToPlayerSqr)
            {
                players.Add(playerObj.transform);
            }
        }
    }

    void UpdateTurretAssignments()
    {
        turretTargets.Clear();
        internalAssignedTurrets.Clear();

        foreach (var player in players)
        {
            if (player == null) continue;
            
            // Bolt: Optimized - Reuse internal list for sorting
            internalSortedTurrets.Clear();
            internalSortedTurrets.AddRange(turrets);

            internalSortedTurrets.Sort((a, b) =>
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;

                // Bolt: Optimized - Use sqrMagnitude for sorting comparisons
                float da = (a.transform.position - player.position).sqrMagnitude;
                float db = (b.transform.position - player.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int assigned = 0;
            foreach (var turret in internalSortedTurrets)
            {
                if (turret == null || internalAssignedTurrets.Contains(turret)) continue;

                turretTargets[turret] = player;
                internalAssignedTurrets.Add(turret);
                assigned++;
                if (assigned >= maxTurretsPerPlayer) break;
            }
        }
    }

    private void ForceRefreshAllTurretTargeting()
    {
        UpdatePlayersList();
        UpdateTurretAssignments();
        UpdateTrackingMode(); // Also refresh tracking mode here instead of every frame
    }
}
