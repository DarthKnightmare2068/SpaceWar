using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class LevelUpSystem : MonoBehaviour
{
    [Header("Level System")]
    [SerializeField] private int currentLevel = 1;
    public const int MAX_LEVEL = 30;
    [SerializeField] private float expToNextLevel = 1000f;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float damageToExpMultiplier = 1f;
    public UnityEvent<int> onLevelUp;
    public UnityEvent onMaxLevelReached;
    public float nextLvStatsScale = 3.14f;

    [Header("Refresh Settings")]
    [SerializeField] private float enemyScanInterval = 2f;

    private PlaneStats playerPlane;

    // List for ordered iteration; HashSet for O(1) duplicate-check in TrackWeaponsOnObject.
    private List<IHasHealth> trackedTargets = new List<IHasHealth>();
    private HashSet<IHasHealth> trackedSet = new HashSet<IHasHealth>();
    private Dictionary<IHasHealth, float> lastHP = new Dictionary<IHasHealth, float>();

    private PlayerWeaponManager cachedWeaponManager;
    private LaserActive cachedLaserActive;
    private float nextEnemyScanTime = 0f;
    private bool hasWeaponLevelUpListener = false;
    private bool hasLaserLevelUpListener = false;
    private int _damageTrackFrame = 0;

    private static string SavePath =>
        System.IO.Path.Combine(Application.dataPath, "Scripts", "Data", "JSON", "save.json");

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;

    void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;

        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            HandlePlayerChanged(player);
    }

    void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    void Start()
    {
        LoadProgress();
        FindPlayerAndEnemies();
        BindLevelUpListenersIfReady();
    }

    void OnDestroy()
    {
        if (hasWeaponLevelUpListener)
            onLevelUp.RemoveListener(HandleWeaponLevelUp);
        if (hasLaserLevelUpListener)
            onLevelUp.RemoveListener(HandleLaserLevelUp);
    }

    private void BindLevelUpListenersIfReady()
    {
        if (!hasWeaponLevelUpListener && cachedWeaponManager != null)
        {
            onLevelUp.AddListener(HandleWeaponLevelUp);
            hasWeaponLevelUpListener = true;
        }

        if (!hasLaserLevelUpListener && cachedLaserActive != null)
        {
            onLevelUp.AddListener(HandleLaserLevelUp);
            hasLaserLevelUpListener = true;
        }
    }

    private void HandleWeaponLevelUp(int level) => cachedWeaponManager?.LevelUp();

    private void HandleLaserLevelUp(int level) => cachedLaserActive?.OnPlayerLevelUp();

    void Update()
    {
        if (playerPlane == null || playerPlane.IsDead())
        {
            if (Time.time >= nextEnemyScanTime)
            {
                FindPlayerAndEnemies();
                nextEnemyScanTime = Time.time + enemyScanInterval;
            }
        }

        CleanupTrackedTargets();
        _damageTrackFrame++;
        if (_damageTrackFrame % 5 == 0)
            TrackEnemyDamage();
    }

    private void Track(IHasHealth target, float hp)
    {
        if (!trackedSet.Add(target)) return; // already tracked
        trackedTargets.Add(target);
        lastHP[target] = hp;
    }

    private void CleanupTrackedTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            if ((trackedTargets[i] as MonoBehaviour) == null)
            {
                trackedSet.Remove(trackedTargets[i]);
                lastHP.Remove(trackedTargets[i]);
                trackedTargets.RemoveAt(i);
            }
        }
    }

    private void CachePlayerReferences(GameObject player)
    {
        if (player == null)
        {
            playerPlane = null;
            cachedWeaponManager = null;
            cachedLaserActive = null;
            return;
        }

        playerPlane = player.GetComponent<PlaneStats>();
        cachedWeaponManager = player.GetComponent<PlayerWeaponManager>();
        cachedLaserActive = player.GetComponent<LaserActive>();
    }

    private void FindPlayerAndEnemies()
    {
        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            CachePlayerReferences(player);
        else
            CachePlayerReferences(null);

        trackedTargets.Clear();
        trackedSet.Clear();
        lastHP.Clear();

        if (GameManager.Instance != null)
        {
            var activeShips = GameManager.Instance.GetActiveEnemyShips();
            foreach (var ship in activeShips)
            {
                if (ship == null) continue;
                var enemyStats = ship.GetComponent<EnemyStats>();
                if (enemyStats != null) Track(enemyStats, enemyStats.CurrentHP);
            }

            if (GameManager.Instance.currentBoss != null)
            {
                var bossStats = GameManager.Instance.currentBoss.GetComponent<MainBossStats>();
                if (bossStats != null) Track(bossStats, bossStats.CurrentHP);
            }
        }

        TrackWeaponsFromManagers();
    }

    // Finds all weapon components through WeaponDmgControl on active ships and boss
    // instead of the expensive FindGameObjectsWithTag scene search.
    private void TrackWeaponsFromManagers()
    {
        if (GameManager.Instance == null) return;

        var allShips = GameManager.Instance.GetActiveEnemyShips();
        TrackWeaponsOnObject(allShips);

        if (GameManager.Instance.currentBoss != null)
        {
            var bossList = new List<GameObject> { GameManager.Instance.currentBoss };
            TrackWeaponsOnObject(bossList);
        }
    }

    private void TrackWeaponsOnObject(List<GameObject> ships)
    {
        foreach (var ship in ships)
        {
            if (ship == null) continue;
            var dmgControl = ship.GetComponentInChildren<WeaponDmgControl>();
            if (dmgControl == null) continue;

            if (dmgControl.turretsManager != null)
                foreach (var turret in dmgControl.turretsManager.turrets)
                    if (turret != null) Track(turret, ((IHasHealth)turret).CurrentHP);

            if (dmgControl.smallCanonManager != null)
                foreach (var canon in dmgControl.smallCanonManager.canons)
                    if (canon != null) Track(canon, ((IHasHealth)canon).CurrentHP);

            foreach (var bigCanon in ship.GetComponentsInChildren<BigCanon>(true))
                if (bigCanon != null) Track(bigCanon, ((IHasHealth)bigCanon).CurrentHP);
        }
    }

    private void TrackEnemyDamage()
    {
        foreach (var target in trackedTargets)
        {
            if ((target as MonoBehaviour) == null) continue;
            float current = target.CurrentHP;
            if (lastHP.TryGetValue(target, out float last) && current < last)
                AddDamageExperience(last - current);
            lastHP[target] = current;
        }
    }

    public void AddDamageExperience(float damage)
    {
        if (damage <= 0 || IsMaxLevel) return;
        AddExperience(damage * damageToExpMultiplier);
    }

    public void AddExperience(float exp)
    {
        if (IsMaxLevel)
        {
            currentExp = expToNextLevel;
            return;
        }
        currentExp += exp;
        while (currentExp >= expToNextLevel && currentLevel < MAX_LEVEL)
            LevelUp();
    }

    private void LevelUp()
    {
        if (currentLevel >= MAX_LEVEL)
        {
            currentExp = expToNextLevel;
            return;
        }
        currentLevel++;
        currentExp -= expToNextLevel;
        if (currentLevel < MAX_LEVEL)
            expToNextLevel *= 1.5f;
        onLevelUp?.Invoke(currentLevel);
        if (currentLevel >= MAX_LEVEL)
            onMaxLevelReached?.Invoke();
        if (playerPlane != null)
        {
            playerPlane.maxHP = Mathf.RoundToInt(playerPlane.maxHP * nextLvStatsScale);
            playerPlane.attackPoint = Mathf.RoundToInt(playerPlane.attackPoint * nextLvStatsScale);
            playerPlane.Heal(playerPlane.maxHP);
        }
        SaveProgress();
    }

    public void SaveProgress()
    {
        var data = new SaveData
        {
            level = currentLevel,
            currentExp = currentExp,
            expToNextLevel = expToNextLevel,
            maxHP = playerPlane != null ? playerPlane.maxHP : 0,
            attackPoint = playerPlane != null ? playerPlane.attackPoint : 0
        };
        string dir = System.IO.Path.GetDirectoryName(SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
    }

    private void LoadProgress()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            currentLevel = Mathf.Clamp(data.level, 1, MAX_LEVEL);
            currentExp = data.currentExp;
            expToNextLevel = data.expToNextLevel;
        }
        catch { }
    }

    // Reads the save file and applies stored maxHP/attackPoint to a freshly spawned player.
    private void ApplySavedStatsToPlayer(PlaneStats stats)
    {
        if (stats == null || !File.Exists(SavePath)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data.maxHP > 0)
            {
                stats.maxHP = data.maxHP;
                stats.Heal(data.maxHP); // Bring currentHP up to new maxHP
            }
            if (data.attackPoint > 0)
                stats.attackPoint = data.attackPoint;
        }
        catch { }
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public void DeleteSave()
    {
        DeleteSaveFile();
        currentLevel = 1;
        currentExp = 0f;
        expToNextLevel = 1000f;
    }

    public void SetLevel(int level) => currentLevel = Mathf.Clamp(level, 1, MAX_LEVEL);
    public void SetExperience(float exp) => currentExp = Mathf.Clamp(exp, 0, expToNextLevel);

    private void HandlePlayerChanged(GameObject player)
    {
        CachePlayerReferences(player);
        if (playerPlane != null)
            ApplySavedStatsToPlayer(playerPlane);
        BindLevelUpListenersIfReady();
        FindPlayerAndEnemies();
    }
}
