using System.Collections;
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
    private bool hasSubscribedEvents = false;

    private const string SAVE_FILE = "/progress.json";

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;

    void Start()
    {
        LoadProgress();
        FindPlayerAndEnemies();
        SubscribeToLevelUpEvents();
    }

    void OnDestroy()
    {
        if (hasSubscribedEvents)
            onLevelUp.RemoveAllListeners();
    }

    private void SubscribeToLevelUpEvents()
    {
        if (hasSubscribedEvents) return;

        if (cachedWeaponManager == null)
            cachedWeaponManager = FindObjectOfType<PlayerWeaponManager>();
        if (cachedWeaponManager != null)
        {
            onLevelUp.AddListener((level) => {
                if (cachedWeaponManager != null)
                    cachedWeaponManager.LevelUp();
            });
        }

        if (cachedLaserActive == null)
            cachedLaserActive = FindObjectOfType<LaserActive>();
        if (cachedLaserActive != null)
        {
            onLevelUp.AddListener((level) => {
                if (cachedLaserActive != null)
                    cachedLaserActive.OnPlayerLevelUp();
            });
        }

        hasSubscribedEvents = true;
    }

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

    private void FindPlayerAndEnemies()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerPlane = playerObj.GetComponent<PlaneStats>();
        }

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

    private void SaveProgress()
    {
        var data = new SaveData { level = currentLevel, currentExp = currentExp, expToNextLevel = expToNextLevel };
        File.WriteAllText(Application.persistentDataPath + SAVE_FILE, JsonUtility.ToJson(data));
    }

    private void LoadProgress()
    {
        string path = Application.persistentDataPath + SAVE_FILE;
        if (!File.Exists(path)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            currentLevel = Mathf.Clamp(data.level, 1, MAX_LEVEL);
            currentExp = data.currentExp;
            expToNextLevel = data.expToNextLevel;
        }
        catch { }
    }

    public void DeleteSave()
    {
        string path = Application.persistentDataPath + SAVE_FILE;
        if (File.Exists(path)) File.Delete(path);
        currentLevel = 1;
        currentExp = 0f;
        expToNextLevel = 1000f;
    }

    public void SetLevel(int level) => currentLevel = Mathf.Clamp(level, 1, MAX_LEVEL);
    public void SetExperience(float exp) => currentExp = Mathf.Clamp(exp, 0, expToNextLevel);

    public void OnPlayerSpawned(GameObject player)
    {
        if (player != null)
        {
            playerPlane = player.GetComponent<PlaneStats>();
            cachedWeaponManager = player.GetComponent<PlayerWeaponManager>();
            cachedLaserActive = player.GetComponent<LaserActive>();
        }
    }

    public void RefreshTrackedEnemies() => FindPlayerAndEnemies();
}
