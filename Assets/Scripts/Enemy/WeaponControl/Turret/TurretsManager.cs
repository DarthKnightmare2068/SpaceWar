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
    private HashSet<TurretControl> assignedTurrets = new HashSet<TurretControl>();
    private List<TurretControl> sortedTurrets = new List<TurretControl>();

    private float howCloseToPlayerSqr;

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;

    private float assignmentTimer = 0f;
    private const float ASSIGNMENT_INTERVAL = 0.2f;

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
        assignmentTimer += Time.deltaTime;
        if (assignmentTimer >= ASSIGNMENT_INTERVAL)
        {
            assignmentTimer = 0f;
            CleanTurretList();
            currentTurretCount = turrets.Count;
            UpdatePlayersList();
            AssignTurretsToPlayers();
        }

        // Tracking and shooting logic runs every frame for smoothness, using cached targets
        // Bolt: Optimized tracking to run every frame while assignment is throttled
        foreach (var turret in turrets)
        {
            if (turret == null) continue;

            Transform target = null;
            turretTargets.TryGetValue(turret, out target);

            turret.ControlTurret(target, howCloseToPlayer);
            turret.SetTrackingMode(trackPlayerInstantly);
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
        foreach(var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            var stats = playerObj.GetComponent<PlaneStats>();
            if(stats != null && stats.CurrentHP <= 0)
            {
                continue;
            }

            // Bolt: Optimized using sqrMagnitude for proximity check
            float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
            if(sqrDist < howCloseToPlayerSqr)
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
            
            // Bolt: Optimized to reuse sortedTurrets list and avoid new allocations
            sortedTurrets.Clear();
            sortedTurrets.AddRange(turrets);

            // Bolt: Optimized sorting using sqrMagnitude instead of Distance
            sortedTurrets.Sort((a, b) =>
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;
                float da = (a.transform.position - player.position).sqrMagnitude;
                float db = (b.transform.position - player.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int assigned = 0;
            foreach (var turret in sortedTurrets)
            {
                if (turret == null || assignedTurrets.Contains(turret)) continue;
                turretTargets[turret] = player;
                assignedTurrets.Add(turret);
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
