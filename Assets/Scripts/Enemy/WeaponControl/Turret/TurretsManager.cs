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
    private float sqrHowCloseToPlayer; // Bolt: Cached squared distance for performance
    private List<Transform> players = new List<Transform>();
    private Dictionary<TurretControl, Transform> turretTargets = new Dictionary<TurretControl, Transform>();

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;

    private float playerListUpdateTimer = 0f;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 0.5f;

    private WeaponDmgControl cachedDmgControl;

    // Bolt: Reuse collections to avoid per-frame allocations
    private List<TurretControl> sortedTurrets = new List<TurretControl>();
    private HashSet<TurretControl> assignedTurrets = new HashSet<TurretControl>();

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
        sqrHowCloseToPlayer = howCloseToPlayer * howCloseToPlayer;

        SetAllTurretsHP();
        maxTurretCount = turrets.Count;
        currentTurretCount = maxTurretCount;

        foreach (var turret in turrets)
        {
            if (turret != null)
                turret.SetTrackingMode(trackPlayerInstantly);
        }

        sortedTurrets.Capacity = turrets.Count;
    }

    void Update()
    {
        CleanTurretList();
        currentTurretCount = turrets.Count;
        
        playerListUpdateTimer += Time.deltaTime;
        if (playerListUpdateTimer >= PLAYER_LIST_UPDATE_INTERVAL)
        {
            playerListUpdateTimer = 0f;
            UpdatePlayersList();
            AssignTurretsToPlayers(); // Bolt: Throttled assignment to 2Hz
        }
        
        // Bolt: Execute tracking/firing every frame using cached assignments
        ExecuteTurretLogic();

        backupRefreshTimer += Time.deltaTime;
        if (backupRefreshTimer >= BACKUP_REFRESH_INTERVAL)
        {
            backupRefreshTimer = 0f;
            ForceRefreshAllTurretTargeting();
        }
    }

    /// <summary>
    /// Bolt: Optimized per-frame turret logic. Uses cached targets to avoid re-calculating assignments every frame.
    /// </summary>
    void ExecuteTurretLogic()
    {
        foreach (var turret in turrets)
        {
            if (turret == null) continue;

            Transform target = null;
            turretTargets.TryGetValue(turret, out target);

            // Unity's null check handles destroyed transforms
            turret.ControlTurret(target, howCloseToPlayer);
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

        // Bolt: Still use FindGameObjectsWithTag to support multiplayer if needed,
        // but this method is now throttled to 2Hz to minimize performance cost.
        foreach (var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (playerObj == null) continue;

            var stats = playerObj.GetComponent<PlaneStats>();
            // Bolt: Ensure we don't target dead players
            if (stats != null && stats.CurrentHP <= 0) continue;

            // Bolt: Use sqrMagnitude to avoid square root
            if ((transform.position - playerObj.transform.position).sqrMagnitude < sqrHowCloseToPlayer)
            {
                players.Add(playerObj.transform);
            }
        }
    }

    void AssignTurretsToPlayers()
    {
        turretTargets.Clear();
        assignedTurrets.Clear();

        foreach (var player in players)
        {
            if (player == null) continue;
            
            // Bolt: Reuse list to avoid garbage collection pressure
            sortedTurrets.Clear();
            sortedTurrets.AddRange(turrets);

            // Bolt: Use sqrMagnitude for sorting
            Vector3 playerPos = player.position;
            sortedTurrets.Sort((a, b) =>
            {
                if (a == b) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float da = (a.transform.position - playerPos).sqrMagnitude;
                float db = (b.transform.position - playerPos).sqrMagnitude;
                return da.CompareTo(db);
            });

            int assignedCount = 0;
            foreach (var turret in sortedTurrets)
            {
                if (turret == null || assignedTurrets.Contains(turret)) continue;

                turretTargets[turret] = player;
                assignedTurrets.Add(turret);

                assignedCount++;
                if (assignedCount >= maxTurretsPerPlayer) break;
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
