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

    private float playerListUpdateTimer = 0f;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 0.5f;

    // Bolt: Optimized targeting by throttling assignments and using squared distances
    private float targetAssignmentTimer = 0f;
    private const float TARGET_ASSIGNMENT_INTERVAL = 0.2f;
    private float howCloseToPlayerSqr;

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
        currentTurretCount = turrets.Count;
        
        // Bolt: Throttle heavy targeting logic (sorting, searching) to reduce CPU load
        targetAssignmentTimer += Time.deltaTime;
        if (targetAssignmentTimer >= TARGET_ASSIGNMENT_INTERVAL)
        {
            targetAssignmentTimer = 0f;
            CleanTurretList();
            UpdatePlayersList();
            AssignTurretsToPlayers();

            foreach (var turret in turrets)
            {
                if (turret != null)
                    turret.SetTrackingMode(trackPlayerInstantly);
            }
        }

        // Bolt: Execute tracking every frame for smooth rotation using cached targets
        foreach (var turret in turrets)
        {
            if (turret != null && turret.gameObject.activeInHierarchy)
            {
                Transform target = null;
                turretTargets.TryGetValue(turret, out target);
                turret.ControlTurret(target, howCloseToPlayer);
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

        // Bolt: Optimized by using GameManager instance instead of scene-wide tag search
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            var playerObj = GameManager.Instance.currentPlayer;
            var stats = playerObj.GetComponent<PlaneStats>();
            if (stats != null && stats.CurrentHP > 0)
            {
                // Bolt: Use sqrMagnitude to avoid expensive square root
                float distSqr = (transform.position - playerObj.transform.position).sqrMagnitude;
                if (distSqr < howCloseToPlayerSqr)
                {
                    players.Add(playerObj.transform);
                }
            }
        }
        else
        {
            // Fallback for multi-player or missing GameManager
            foreach(var playerObj in GameObject.FindGameObjectsWithTag("Player"))
            {
                var stats = playerObj.GetComponent<PlaneStats>();
                if(stats != null && stats.CurrentHP <= 0)
                {
                    continue;
                }

                float distSqr = (transform.position - playerObj.transform.position).sqrMagnitude;
                if(distSqr < howCloseToPlayerSqr)
                {
                    players.Add(playerObj.transform);
                }
            }
        }
    }

    void AssignTurretsToPlayers()
    {
        turretTargets.Clear();
        var assignedTurrets = new HashSet<TurretControl>();

        foreach (var player in players)
        {
            // Skip destroyed players to avoid MissingReferenceException when accessing position
            if (player == null) continue;
            
            List<TurretControl> sortedTurrets = new List<TurretControl>(turrets);
            sortedTurrets.Sort((a, b) =>
            {
                // Bolt: Use sqrMagnitude for sorting to eliminate square root costs
                float da = a == null ? float.MaxValue : (a.transform.position - player.position).sqrMagnitude;
                float db = b == null ? float.MaxValue : (b.transform.position - player.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int assigned = 0;
            foreach (var turret in sortedTurrets)
            {
                if (turret == null || assignedTurrets.Contains(turret)) continue;
                turretTargets[turret] = player;
                assignedTurrets.Add(turret);
                // ControlTurret is now called in Update() every frame for smoothness
                assigned++;
                if (assigned >= maxTurretsPerPlayer) break;
            }
        }

        foreach (var turret in turrets)
        {
            if (turret == null) continue;
            if (!turretTargets.ContainsKey(turret))
            {
                turret.ControlTurret(null, howCloseToPlayer);
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
