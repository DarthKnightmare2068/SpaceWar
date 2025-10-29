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

    // Internals
    private float howCloseToPlayer; // This will be set from WeaponDmgControl
    private List<Transform> players = new List<Transform>();
    private Dictionary<TurretControl, Transform> turretTargets = new Dictionary<TurretControl, Transform>();

    // Backup refresh system for turret targeting
    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f; // Refresh every 1 second

    void Awake()
    {
        // Always rebuild the turrets list from all children (active and inactive)
        turrets = new List<TurretControl>(GetComponentsInChildren<TurretControl>(true));

        // Initialize from WeaponDmgControl
        WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
        if (dmgControl != null)
        {
            howCloseToPlayer = dmgControl.GetTurretFireRange();
        }
        else
        {
            howCloseToPlayer = 100f; // Fallback
            Debug.LogWarning("WeaponDmgControl not found. Using default fire range for TurretManager.");
        }

        SetAllTurretsHP();
        maxTurretCount = turrets.Count;
        currentTurretCount = maxTurretCount;

        // Set tracking mode for all turrets at start
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
        UpdatePlayersList();
        AssignTurretsToPlayers();

        // Backup refresh system - force complete targeting refresh every 1 second
        backupRefreshTimer += Time.deltaTime;
        if (backupRefreshTimer >= BACKUP_REFRESH_INTERVAL)
        {
            backupRefreshTimer = 0f;
            ForceRefreshAllTurretTargeting();
        }

        // Sync tracking mode for all turrets every frame (in case toggled at runtime)
        foreach (var turret in turrets)
        {
            if (turret != null)
                turret.SetTrackingMode(trackPlayerInstantly);
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
            // Skip any dead planes
            var stats = playerObj.GetComponent<PlaneStats>();
            if(stats != null && stats.CurrentHP <= 0)
            {
                continue;
            }

            // Only add if within detection range
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if(dist < howCloseToPlayer)
            {
                players.Add(playerObj.transform);
            }
        }
    }

    void AssignTurretsToPlayers()
    {
        // Clear previous assignments
        turretTargets.Clear();
        var assignedTurrets = new HashSet<TurretControl>();

        // For each player, assign the N closest turrets
        foreach (var player in players)
        {
            // Sort turrets by distance to this player
            List<TurretControl> sortedTurrets = new List<TurretControl>(turrets);
            sortedTurrets.Sort((a, b) =>
            {
                float da = a == null ? float.MaxValue : Vector3.Distance(a.transform.position, player.position);
                float db = b == null ? float.MaxValue : Vector3.Distance(b.transform.position, player.position);
                return da.CompareTo(db);
            });

            int assigned = 0;
            foreach (var turret in sortedTurrets)
            {
                if (turret == null || assignedTurrets.Contains(turret)) continue;
                turretTargets[turret] = player;
                assignedTurrets.Add(turret);
                turret.ControlTurret(player, howCloseToPlayer);
                assigned++;
                if (assigned >= maxTurretsPerPlayer) break;
            }
        }

        // Any turret not assigned to a player should not target anyone
        foreach (var turret in turrets)
        {
            if (turret == null) continue;
            if (!turretTargets.ContainsKey(turret))
            {
                turret.ControlTurret(null, howCloseToPlayer); // Or implement idle behavior
            }
        }
    }

    /// <summary>
    /// Backup method to force refresh all turret targeting every 1 second
    /// This ensures turrets properly reset their targeting even if main system fails
    /// </summary>
    private void ForceRefreshAllTurretTargeting()
    {
        // Clear all current assignments
        turretTargets.Clear();
        
        // Force all turrets to stop targeting (cancel aim)
        foreach (var turret in turrets)
        {
            if (turret != null && turret.gameObject.activeInHierarchy)
            {
                turret.ControlTurret(null, howCloseToPlayer);
            }
        }
        
        // Force immediate re-assignment
        UpdatePlayersList();
        AssignTurretsToPlayers();
        
        Debug.Log("[TurretsManager] Backup refresh: All turret targeting reset and reassigned");
    }
} 