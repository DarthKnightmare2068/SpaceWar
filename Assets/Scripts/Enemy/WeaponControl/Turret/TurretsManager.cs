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
    private float howCloseToPlayerSqr;
    private List<Transform> players = new List<Transform>();

    private List<TurretControl> sortedTurretCache = new List<TurretControl>();
    private List<TurretControl> assignedTurrets = new List<TurretControl>();
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

    // Reused list for O(N) position pre-fetching before sorting
    private struct TurretDistanceData
    {
        public TurretControl turret;
        public float sqrDist;
    }
    private List<TurretDistanceData> distanceDataCache = new List<TurretDistanceData>();

    // Reused comparer instance for TurretDistanceData
    private static readonly TurretDistanceComparer distanceComparer = new TurretDistanceComparer();
    private class TurretDistanceComparer : IComparer<TurretDistanceData>
    {
        public int Compare(TurretDistanceData a, TurretDistanceData b) => a.sqrDist.CompareTo(b.sqrDist);
    }

    void Awake()
    {
        turrets = new List<TurretControl>(GetComponentsInChildren<TurretControl>(true));

        cachedDmgControl = GetComponentInParent<WeaponDmgControl>();
        if (cachedDmgControl == null)
            cachedDmgControl = FindAnyObjectByType<WeaponDmgControl>();

        if (cachedDmgControl != null)
            howCloseToPlayer = cachedDmgControl.GetTurretFireRange();
        else
            howCloseToPlayer = 100f;

        // Bolt: Optimized squared distance caching
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

        // Bolt: Optimized - Every frame: call ControlTurret only for assigned turrets.
        // This reduces Update overhead from O(Total Turrets) to O(Assigned Turrets).
        for (int i = 0; i < assignedTurrets.Count; i++)
        {
            var turret = assignedTurrets[i];
            if (turret != null)
            {
                // Bolt: Optimized using pre-calculated sqr distance and avoiding dictionary lookups
                turret.ControlTurret(howCloseToPlayerSqr);
            }
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
        // Bolt: Clear old assignments
        for (int i = 0; i < assignedTurrets.Count; i++)
        {
            var t = assignedTurrets[i];
            if (t != null) t.CurrentTarget = null;
        }

        assignedTurrets.Clear();
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

            Vector3 playerPos = player.position;

            // Bolt: Optimized - Pre-fetch sqr distances once (O(N) native calls)
            distanceDataCache.Clear();
            for (int i = 0; i < sortedTurretCache.Count; i++)
            {
                var t = sortedTurretCache[i];
                if (t == null) continue;
                distanceDataCache.Add(new TurretDistanceData {
                    turret = t,
                    sqrDist = (t.transform.position - playerPos).sqrMagnitude
                });
            }

            // Bolt: Optimized - Sort local data (O(N log N) with 0 native calls)
            distanceDataCache.Sort(distanceComparer);

            int assignedCount = 0;
            for (int i = 0; i < distanceDataCache.Count; i++)
            {
                var turret = distanceDataCache[i].turret;
                if (!turret.gameObject.activeInHierarchy || reusableAssignedSet.Contains(turret)) continue;

                // Bolt: Direct assignment to turret
                turret.CurrentTarget = player;
                assignedTurrets.Add(turret);
                reusableAssignedSet.Add(turret);
                assignedCount++;
                if (assignedCount >= maxTurretsPerPlayer) break;
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

        // Bolt: Optimized with sqrMagnitude
        float sqrDist = (transform.position - playerTransform.position).sqrMagnitude;
        if (sqrDist < howCloseToPlayerSqr)
            players.Add(playerTransform);
    }
}
