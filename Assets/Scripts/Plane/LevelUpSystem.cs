using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class LevelUpSystem : MonoBehaviour
{
    // Bolt: Optimized - Singleton pattern for O(1) access from high-frequency damage paths.
    public static LevelUpSystem Instance { get; private set; }

    [Header("Level System")]
    [SerializeField] private int currentLevel = 1;
    public const int MAX_LEVEL = 30;
    [SerializeField] private float expToNextLevel = 1000f;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float damageToExpMultiplier = 1f;
    public UnityEvent<int> onLevelUp;
    public UnityEvent onMaxLevelReached;
    public float nextLvStatsScale = 3.14f;

    private PlaneStats playerPlane;

    private PlayerWeaponManager cachedWeaponManager;
    private LaserActive cachedLaserActive;
    private bool hasWeaponLevelUpListener = false;
    private bool hasLaserLevelUpListener = false;

    private static string SavePath =>
        System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

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
        // Bolt: Ensure player references are bound on start if already registered
        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            CachePlayerReferences(player);
        BindLevelUpListenersIfReady();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

    // Bolt: Optimized - Removed heavy polling logic (Update, ProcessTrackedTargets, FindPlayerAndEnemies).
    // Experience gain is now event-driven from DamageHelper.cs, saving significant CPU and memory.

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
        try
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
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        }
        catch (System.Exception) { /* Sentinel: Ignore save failures to prevent interrupting gameplay. */ }
    }

    private void LoadProgress()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            FileInfo fileInfo = new FileInfo(SavePath);
            // Sentinel: Security check - Limit file size to 1MB to prevent OOM attacks.
            if (fileInfo.Length > 1024 * 1024) return;

            var json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;

            currentLevel = Mathf.Clamp(data.level, 1, MAX_LEVEL);
            // Sentinel: Validate loaded data to prevent rapid leveling or infinite loops.
            currentExp = Mathf.Max(0, data.currentExp);
            expToNextLevel = Mathf.Max(100f, data.expToNextLevel);
        }
        catch (System.Exception) { /* Fail silently to prevent crashing on corrupt save data. */ }
    }

    // Reads the save file and applies stored maxHP/attackPoint to a freshly spawned player.
    private void ApplySavedStatsToPlayer(PlaneStats stats)
    {
        if (stats == null || !File.Exists(SavePath)) return;
        try
        {
            FileInfo fileInfo = new FileInfo(SavePath);
            if (fileInfo.Length > 1024 * 1024) return;

            var json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;

            // Sentinel: Sane upper limits to prevent game logic exploitation via file tampering.
            if (data.maxHP > 0)
            {
                stats.maxHP = Mathf.Clamp(data.maxHP, 1, 1000000);
                stats.Heal(stats.maxHP); // Bring currentHP up to new maxHP
            }
            if (data.attackPoint > 0)
            {
                stats.attackPoint = Mathf.Clamp(data.attackPoint, 1, 1000000);
            }
        }
        catch (System.Exception) { }
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
    }
}
