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
    private float targetingUpdateTimer = 0f;
    private const float TARGETING_UPDATE_INTERVAL = 0.2f;
    private List<Transform> players = new List<Transform>();
    private Dictionary<TurretControl, Transform> turretTargets = new Dictionary<TurretControl, Transform>();

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;


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
        
        // Bolt: Optimized targeting by throttling expensive sorting/searching to 5Hz
        targetingUpdateTimer += Time.deltaTime;
        if (targetingUpdateTimer >= TARGETING_UPDATE_INTERVAL)
        {
            targetingUpdateTimer = 0f;
            UpdatePlayersList();
            AssignTurretsToPlayers();
        }
        
        // Bolt: Track targets every frame for smooth rotation using cached results
        foreach (var turret in turrets)
        {
            if (turret == null || !turret.gameObject.activeInHierarchy) continue;

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

        // Bolt: Optimized player access by using GameManager.Instance.currentPlayer
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            GameObject playerObj = GameManager.Instance.currentPlayer;
            var stats = playerObj.GetComponent<PlaneStats>();
            if (stats != null && stats.CurrentHP > 0)
            {
                // Bolt: Using sqrMagnitude instead of Distance to avoid expensive square root
                float sqrDist = (transform.position - playerObj.transform.position).sqrMagnitude;
                if (sqrDist < sqrHowCloseToPlayer)
                {
                    players.Add(playerObj.transform);
                }
            }
        }
        else
        {
            // Fallback to tag search if GameManager is not available
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
                // Bolt: Using sqrMagnitude for sorting to avoid square root cost
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
                // Bolt: Removed ControlTurret call from here to move it to every frame in Update
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
