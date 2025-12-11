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
        foreach(var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            var stats = playerObj.GetComponent<PlaneStats>();
            if(stats != null && stats.CurrentHP <= 0)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if(dist < howCloseToPlayer)
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
