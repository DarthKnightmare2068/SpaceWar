using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelUpSystem : MonoBehaviour
{
    [Header("Level System")]
    [Tooltip("Current level of the player")]
    [SerializeField] private int currentLevel = 1;
    [Tooltip("Maximum level cap")]
    public const int MAX_LEVEL = 30;
    [Tooltip("Experience points needed for next level")]
    [SerializeField] private float expToNextLevel = 1000f;
    [Tooltip("Current experience points")]
    [SerializeField] private float currentExp = 0f;
    [Tooltip("Experience multiplier from damage")]
    [SerializeField] private float damageToExpMultiplier = 1f;
    [Tooltip("Event triggered when leveling up")]
    public UnityEvent<int> onLevelUp;
    [Tooltip("Event triggered when max level is reached")]
    public UnityEvent onMaxLevelReached;
    [Tooltip("Scaling factor for player stats on level up")]
    public float nextLvStatsScale = 3.14f;

    private PlaneStats playerPlane;
    private List<object> trackedTargets = new List<object>();
    private Dictionary<object, float> lastHP = new Dictionary<object, float>();

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;

    void Start()
    {
        FindPlayerAndEnemies();

        // Subscribe PlayerWeaponManager.LevelUp() to onLevelUp event
        var weaponManager = FindObjectOfType<PlayerWeaponManager>();
        if (weaponManager != null)
        {
            onLevelUp.AddListener((level) => weaponManager.LevelUp());
        }
        
        // Subscribe LaserActive.OnPlayerLevelUp() to onLevelUp event
        var laserActive = FindObjectOfType<LaserActive>();
        if (laserActive != null)
        {
            onLevelUp.AddListener((level) => laserActive.OnPlayerLevelUp());
        }
    }

    void Update()
    {
        // Continuously check for new player/enemies in case of respawn
        if (playerPlane == null || playerPlane.IsDead())
            FindPlayerAndEnemies();
        TrackEnemyDamage();
    }

    private void FindPlayerAndEnemies()
    {
        playerPlane = FindObjectOfType<PlaneStats>();
        trackedTargets.Clear();
        lastHP.Clear();

        if (playerPlane != null)
            Debug.Log($"[LevelUpSystem] Detected player: {playerPlane.name}");
        else
            Debug.Log("[LevelUpSystem] No player detected!");

        // Track EnemyStats
        foreach (var enemy in FindObjectsOfType<EnemyStats>())
        {
            trackedTargets.Add(enemy);
            lastHP[enemy] = enemy.CurrentHP;
            Debug.Log($"[LevelUpSystem] Detected enemy: {enemy.name}");
        }
        // Track MainBossStats
        foreach (var boss in FindObjectsOfType<MainBossStats>())
        {
            trackedTargets.Add(boss);
            lastHP[boss] = boss.CurrentHP;
            Debug.Log($"[LevelUpSystem] Detected boss: {boss.name}");
        }
        // Track TurretControl
        foreach (var turret in FindObjectsOfType<TurretControl>())
        {
            trackedTargets.Add(turret);
            lastHP[turret] = turret.currentHP;
            Debug.Log($"[LevelUpSystem] Detected turret: {turret.name}");
        }
        // Track SmallCanonControl
        foreach (var canon in FindObjectsOfType<SmallCanonControl>())
        {
            trackedTargets.Add(canon);
            lastHP[canon] = canon.currentHP;
            Debug.Log($"[LevelUpSystem] Detected small canon: {canon.name}");
        }
        // Track BigCanon
        foreach (var bigCanon in FindObjectsOfType<BigCanon>())
        {
            trackedTargets.Add(bigCanon);
            lastHP[bigCanon] = bigCanon.currentHP;
            Debug.Log($"[LevelUpSystem] Detected big canon: {bigCanon.name}");
        }
        if (trackedTargets.Count == 0)
            Debug.Log("[LevelUpSystem] No enemies, bosses, or turrets detected!");
    }

    private void TrackEnemyDamage()
    {
        foreach (var target in trackedTargets)
        {
            float currentHP = GetCurrentHP(target);
            float last = lastHP.ContainsKey(target) ? lastHP[target] : currentHP;
            if (currentHP < last)
            {
                float damageDealt = last - currentHP;
                AddDamageExperience(damageDealt);
            }
            lastHP[target] = currentHP;
        }
    }

    private float GetCurrentHP(object target)
    {
        if (target is EnemyStats es) return es.CurrentHP;
        if (target is MainBossStats bs) return bs.CurrentHP;
        if (target is TurretControl tc) return tc.currentHP;
        if (target is SmallCanonControl sc) return sc.currentHP;
        if (target is BigCanon bc) return bc.currentHP;
        return 0f;
    }

    public void AddDamageExperience(float damage)
    {
        if (damage <= 0) return;
        if (IsMaxLevel) return;
        AddExperience(damage * damageToExpMultiplier);
    }

    public void AddExperience(float exp)
    {
        if (IsMaxLevel) {
            currentExp = expToNextLevel;
            return;
        }
        currentExp += exp;
        while (currentExp >= expToNextLevel && currentLevel < MAX_LEVEL)
        {
            LevelUp();
        }
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
        {
            expToNextLevel *= 1.5f; // Increase exp needed for next level
        }
        Debug.Log($"[LevelUpSystem] Leveled up to {currentLevel}!");
        onLevelUp?.Invoke(currentLevel);
        if (currentLevel >= MAX_LEVEL)
        {
            Debug.Log($"[LevelUpSystem] Reached maximum level {MAX_LEVEL}!");
            onMaxLevelReached?.Invoke();
        }
        // Update player stats on level up
        if (playerPlane != null)
        {
            playerPlane.maxHP = Mathf.RoundToInt(playerPlane.maxHP * nextLvStatsScale);
            playerPlane.attackPoint = Mathf.RoundToInt(playerPlane.attackPoint * nextLvStatsScale);
            playerPlane.Heal(playerPlane.maxHP); // Fully heal the player
        }
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, MAX_LEVEL);
    }

    public void SetExperience(float exp)
    {
        currentExp = Mathf.Clamp(exp, 0, expToNextLevel);
    }
}
