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

    private WeaponDmgControl cachedDmgControl;
    private PlaneStats cachedPlayerStats;
    private GameObject lastKnownPlayer;

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
        CleanTurretList();
        currentTurretCount = turrets.Count;
        
        playerListUpdateTimer += Time.deltaTime;
        if (playerListUpdateTimer >= PLAYER_LIST_UPDATE_INTERVAL)
        {
            playerListUpdateTimer = 0f;
            UpdatePlayersList();
            // Bolt: Optimized - Move expensive target assignment inside the timer block
            // to avoid sorting and re-assigning targets every single frame.
            AssignTurretsToPlayers();
        }
        
        // Bolt: Optimized - Control already assigned turrets every frame to ensure smooth tracking
        // using the cached assignments in turretTargets.
        foreach (var turret in turrets)
        {
            if (turret == null) continue;

            Transform target = null;
            turretTargets.TryGetValue(turret, out target);
            turret.ControlTurret(target, howCloseToPlayer);
        }

        backupRefreshTimer += Time.deltaTime;
        if (backupRefreshTimer >= BACKUP_REFRESH_INTERVAL)
        {
            backupRefreshTimer = 0f;
            ForceRefreshAllTurretTargeting();
        }

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

        // Bolt: Optimized - Prefer GameManager's cached player reference to avoid expensive FindGameObjectsWithTag
        GameObject playerObj = (GameManager.Instance != null) ? GameManager.Instance.currentPlayer : null;

        if (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObj != null)
        {
            // Bolt: Optimized - Cache PlaneStats component to avoid repeated GetComponent calls
            if (playerObj != lastKnownPlayer || cachedPlayerStats == null)
            {
                lastKnownPlayer = playerObj;
                cachedPlayerStats = playerObj.GetComponent<PlaneStats>();
            }

            if (cachedPlayerStats != null && cachedPlayerStats.CurrentHP <= 0)
            {
                return;
            }

            // Bolt: Optimized - Use sqrMagnitude for faster distance comparison (skips square root)
            float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
            if (sqrDist < howCloseToPlayer * howCloseToPlayer)
            {
                players.Add(playerObj.transform);
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
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                // Bolt: Optimized - Use sqrMagnitude to avoid expensive square root calculations during sort
                float sqrDa = (a.transform.position - player.position).sqrMagnitude;
                float sqrDb = (b.transform.position - player.position).sqrMagnitude;
                return sqrDa.CompareTo(sqrDb);
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
