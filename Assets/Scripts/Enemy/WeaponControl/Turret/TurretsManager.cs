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
    private float sqrHowCloseToPlayer;
    private List<Transform> players = new List<Transform>();
    private Dictionary<TurretControl, Transform> turretTargets = new Dictionary<TurretControl, Transform>();

    // Bolt: Optimized - Cached collections to avoid per-frame allocations
    private List<TurretControl> reusableTurretList = new List<TurretControl>();
    private HashSet<TurretControl> assignedTurrets = new HashSet<TurretControl>();

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;

    private float playerListUpdateTimer = 0f;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 0.5f;

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
        sqrHowCloseToPlayer = howCloseToPlayer * howCloseToPlayer;

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
        }
        
        AssignTurretsToPlayers();

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

        // Bolt: Optimized - Use GameManager instance for faster player access if available
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            var playerObj = GameManager.Instance.currentPlayer;
            var stats = playerObj.GetComponent<PlaneStats>();
            if (stats != null && stats.CurrentHP > 0)
            {
                // Bolt: Optimized - Use sqrMagnitude to avoid expensive square root
                float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
                if (sqrDist < sqrHowCloseToPlayer)
                {
                    players.Add(playerObj.transform);
                }
            }
            return;
        }

        foreach(var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            var stats = playerObj.GetComponent<PlaneStats>();
            if(stats != null && stats.CurrentHP <= 0)
            {
                continue;
            }

            float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
            if(sqrDist < sqrHowCloseToPlayer)
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
            // Skip destroyed players to avoid MissingReferenceException when accessing position
            if (player == null) continue;
            
            // Bolt: Optimized - Reuse list and use sqrMagnitude for sorting
            reusableTurretList.Clear();
            reusableTurretList.AddRange(turrets);
            reusableTurretList.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                float da = (a.transform.position - player.position).sqrMagnitude;
                float db = (b.transform.position - player.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int assigned = 0;
            foreach (var turret in reusableTurretList)
            {
                if (turret == null || !turret.gameObject.activeInHierarchy || assignedTurrets.Contains(turret)) continue;
                turretTargets[turret] = player;
                assignedTurrets.Add(turret);
                turret.ControlTurret(player, sqrHowCloseToPlayer);
                assigned++;
                if (assigned >= maxTurretsPerPlayer) break;
            }
        }

        foreach (var turret in turrets)
        {
            if (turret == null) continue;
            if (!turretTargets.ContainsKey(turret))
            {
                turret.ControlTurret(null, sqrHowCloseToPlayer);
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
                turret.ControlTurret(null, sqrHowCloseToPlayer);
            }
        }
        
        UpdatePlayersList();
        AssignTurretsToPlayers();
    }
}
