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
    // Bolt: Optimized - Pre-calculate squared distance threshold to avoid per-frame linear calculations.
    private float howCloseToPlayerSqr;
    private List<Transform> players = new List<Transform>();

    private Dictionary<TurretControl, Transform> cachedAssignment = new Dictionary<TurretControl, Transform>();
    private List<TurretControl> sortedTurretCache = new List<TurretControl>();
    private HashSet<TurretControl> reusableAssignedSet = new HashSet<TurretControl>();

    private bool sortedCacheDirty = true;
    private bool listDirty = false;
    private bool lastTrackPlayerInstantly;

    private float playerListUpdateTimer = 0f;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 0.5f;

    // Assignment (sort + assign) runs every ASSIGN_INTERVAL, not every frame.
    private float assignTimer = 0f;
    private const float ASSIGN_INTERVAL = 0.5f;

    private float backupRefreshTimer = 0f;
    private const float BACKUP_REFRESH_INTERVAL = 1f;

    private WeaponDmgControl cachedDmgControl;

    void Awake()
    {
        turrets = new List<TurretControl>(GetComponentsInChildren<TurretControl>(true));

        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
            cachedDmgControl = FindObjectOfType<WeaponDmgControl>();

        if (cachedDmgControl != null)
            howCloseToPlayer = cachedDmgControl.GetTurretFireRange();
        else
            howCloseToPlayer = 100f;

        howCloseToPlayerSqr = howCloseToPlayer * howCloseToPlayer;

        SetAllTurretsHP();
        maxTurretCount = turrets.Count;
        currentTurretCount = maxTurretCount;
        lastTrackPlayerInstantly = trackPlayerInstantly;

        foreach (var turret in turrets)
        {
            if (turret != null)
                turret.SetTrackingMode(trackPlayerInstantly);
        }
    }

    void Update()
    {
        // Only clean list when a turret was destroyed (dirty flag set by MarkTurretListDirty).
        if (listDirty)
        {
            turrets.RemoveAll(t => t == null);
            listDirty = false;
            sortedCacheDirty = true;
            currentTurretCount = turrets.Count;
        }

        playerListUpdateTimer += Time.deltaTime;
        if (playerListUpdateTimer >= PLAYER_LIST_UPDATE_INTERVAL)
        {
            playerListUpdateTimer = 0f;
            UpdatePlayersList();
            // Player moved — assignment may need refresh.
            sortedCacheDirty = true;
        }

        // Rebuild sort + assignment at interval, not every frame.
        assignTimer += Time.deltaTime;
        if (assignTimer >= ASSIGN_INTERVAL || sortedCacheDirty)
        {
            assignTimer = 0f;
            RefreshAssignment();
        }

        // Every frame: call ControlTurret with cached assignment for smooth rotation/shooting.
        for (int i = 0; i < turrets.Count; i++)
        {
            var turret = turrets[i];
            if (turret == null) continue;
            cachedAssignment.TryGetValue(turret, out Transform target);
            turret.ControlTurret(target, howCloseToPlayerSqr);
        }

        backupRefreshTimer += Time.deltaTime;
        if (backupRefreshTimer >= BACKUP_REFRESH_INTERVAL)
        {
            backupRefreshTimer = 0f;
            sortedCacheDirty = true;
        }

        if (trackPlayerInstantly != lastTrackPlayerInstantly)
        {
            lastTrackPlayerInstantly = trackPlayerInstantly;
            foreach (var turret in turrets)
            {
                if (turret != null)
                    turret.SetTrackingMode(trackPlayerInstantly);
            }
        }
    }

    // Call this when a turret is destroyed so the list is cleaned next frame.
    public void MarkTurretListDirty()
    {
        listDirty = true;
    }

    private void RefreshAssignment()
    {
        cachedAssignment.Clear();
        reusableAssignedSet.Clear();

        if (sortedCacheDirty)
        {
            sortedTurretCache.Clear();
            sortedTurretCache.AddRange(turrets);
            sortedCacheDirty = false;
        }

        foreach (var player in players)
        {
            if (player == null) continue;

            // Bolt: Optimized - Use sqrMagnitude for sorting to eliminate square root overhead.
            sortedTurretCache.Sort((a, b) =>
            {
                float da = a == null ? float.MaxValue : (a.transform.position - player.position).sqrMagnitude;
                float db = b == null ? float.MaxValue : (b.transform.position - player.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int assigned = 0;
            for (int i = 0; i < sortedTurretCache.Count; i++)
            {
                var turret = sortedTurretCache[i];
                if (turret == null || reusableAssignedSet.Contains(turret)) continue;
                cachedAssignment[turret] = player;
                reusableAssignedSet.Add(turret);
                assigned++;
                if (assigned >= maxTurretsPerPlayer) break;
            }
        }
    }

    public void CleanTurretList()
    {
        turrets.RemoveAll(t => t == null);
        sortedCacheDirty = true;
        currentTurretCount = turrets.Count;
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
        Transform playerTransform = GameEntityRegistry.Player;
        if (playerTransform == null) return;

        var stats = playerTransform.GetComponent<PlaneStats>();
        if (stats != null && stats.CurrentHP <= 0) return;

        // Bolt: Optimized - Distance check using sqrMagnitude for performance.
        if ((transform.position - playerTransform.position).sqrMagnitude < howCloseToPlayerSqr)
            players.Add(playerTransform);
    }
}
