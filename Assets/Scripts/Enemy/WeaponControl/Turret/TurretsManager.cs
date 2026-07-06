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

    // Bolt: Optimized - data struct for sorting to avoid per-sort native bridge calls
    private struct TurretDistanceData
    {
        public TurretControl Turret;
        public float SqrDistance;
    }

    private List<TurretDistanceData> turretDistanceBuffer = new List<TurretDistanceData>();

    // Reused comparer instance — avoids the closure allocation that Sort(lambda) makes per call.
    private static readonly TurretDistanceComparer comparer = new TurretDistanceComparer();
    private class TurretDistanceComparer : IComparer<TurretDistanceData>
    {
        public int Compare(TurretDistanceData a, TurretDistanceData b)
        {
            return a.SqrDistance.CompareTo(b.SqrDistance);
        }
    }

    void Awake()
    {
        turrets = new List<TurretControl>(GetComponentsInChildren<TurretControl>(true));
        // Bolt: Pre-allocate buffer to avoid dynamic resizing during assignment
        turretDistanceBuffer = new List<TurretDistanceData>(turrets.Count);

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
            sortedCacheDirty = false;
            RefreshAssignment();
        }

        // Every frame: call ControlTurret with cached assignment for smooth rotation/shooting.
        // Bolt: Optimized - cache target positions to minimize per-turret native calls.
        for (int i = 0; i < turrets.Count; i++)
        {
            var turret = turrets[i];
            if (turret != null && turret.CurrentTarget != null)
            {
                // Bolt: Optimized - pass cached position to avoid per-turret transform.position native calls in ControlTurret
                turret.ControlTurret(turret.CurrentTarget.position, howCloseToPlayerSqr);
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
        foreach (var t in turrets)
        {
            if (t != null) t.CurrentTarget = null;
        }

        reusableAssignedSet.Clear();

        foreach (var player in players)
        {
            if (player == null) continue;

            Vector3 playerPos = player.position;

            // Bolt: Optimized - Pre-calculate distances once to avoid O(N log N) native calls during Sort.
            turretDistanceBuffer.Clear();
            for (int i = 0; i < turrets.Count; i++)
            {
                var t = turrets[i];
                if (t == null || reusableAssignedSet.Contains(t)) continue;

                turretDistanceBuffer.Add(new TurretDistanceData {
                    Turret = t,
                    SqrDistance = (t.transform.position - playerPos).sqrMagnitude
                });
            }

            turretDistanceBuffer.Sort(comparer);

            int assigned = 0;
            for (int i = 0; i < turretDistanceBuffer.Count; i++)
            {
                var data = turretDistanceBuffer[i];
                // Bolt: Direct assignment to turret
                data.Turret.CurrentTarget = player;
                reusableAssignedSet.Add(data.Turret);
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

        // Bolt: Optimized with sqrMagnitude
        float sqrDist = (transform.position - playerTransform.position).sqrMagnitude;
        if (sqrDist < howCloseToPlayerSqr)
            players.Add(playerTransform);
    }
}
