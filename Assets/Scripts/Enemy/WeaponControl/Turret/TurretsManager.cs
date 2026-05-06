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

    // Reused comparer instance — avoids the closure allocation that Sort(lambda) makes per call.
    private static readonly TurretByDistanceComparer comparer = new TurretByDistanceComparer();
    private class TurretByDistanceComparer : IComparer<TurretControl>
    {
        public Vector3 PlayerPos;
        public int Compare(TurretControl a, TurretControl b)
        {
            float da = a == null ? float.MaxValue : (a.transform.position - PlayerPos).sqrMagnitude;
            float db = b == null ? float.MaxValue : (b.transform.position - PlayerPos).sqrMagnitude;
            return da.CompareTo(db);
        }
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
            if (!cachedAssignment.TryGetValue(turret, out Transform target) || target == null) continue;
            turret.ControlTurret(target, howCloseToPlayer);
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

            comparer.PlayerPos = player.position;
            sortedTurretCache.Sort(comparer);

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

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist < howCloseToPlayer)
            players.Add(playerTransform);
    }
}
