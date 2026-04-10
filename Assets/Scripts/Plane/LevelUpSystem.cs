using System.Collections;
using System.Collections.Generic;
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
    private List<object> trackedTargets = new List<object>();
    private Dictionary<object, float> lastHP = new Dictionary<object, float>();
    
    private PlayerWeaponManager cachedWeaponManager;
    private LaserActive cachedLaserActive;
    private float nextEnemyScanTime = 0f;
    private bool hasSubscribedEvents = false;

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;

    void Start()
    {
        FindPlayerAndEnemies();
        SubscribeToLevelUpEvents();
    }

    void OnDestroy()
    {
        if (hasSubscribedEvents)
        {
            onLevelUp.RemoveAllListeners();
        }
    }

    private void SubscribeToLevelUpEvents()
    {
        if (hasSubscribedEvents) return;
        
        if (cachedWeaponManager == null)
        {
            cachedWeaponManager = FindObjectOfType<PlayerWeaponManager>();
        }
        if (cachedWeaponManager != null)
        {
            onLevelUp.AddListener((level) => {
                if (cachedWeaponManager != null)
                    cachedWeaponManager.LevelUp();
            });
        }
        
        if (cachedLaserActive == null)
        {
            cachedLaserActive = FindObjectOfType<LaserActive>();
        }
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

    private void CleanupTrackedTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            object target = trackedTargets[i];
            bool isDestroyed = false;
            
            if (target is EnemyStats es && es == null) isDestroyed = true;
            else if (target is MainBossStats bs && bs == null) isDestroyed = true;
            else if (target is TurretControl tc && tc == null) isDestroyed = true;
            else if (target is SmallCanonControl sc && sc == null) isDestroyed = true;
            else if (target is BigCanon bc && bc == null) isDestroyed = true;
            
            if (isDestroyed)
            {
                lastHP.Remove(target);
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
            GameObject playerObj = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
            if (playerObj != null)
            {
                playerPlane = playerObj.GetComponent<PlaneStats>();
            }
        }
        
        trackedTargets.Clear();
        lastHP.Clear();

        if (GameManager.Instance != null)
        {
            var activeShips = GameManager.Instance.GetActiveEnemyShips();
            foreach (var ship in activeShips)
            {
                if (ship != null)
                {
                    var enemyStats = ship.GetComponent<EnemyStats>();
                    if (enemyStats != null)
                    {
                        trackedTargets.Add(enemyStats);
                        lastHP[enemyStats] = enemyStats.CurrentHP;
                    }
                }
            }
            
            if (GameManager.Instance.currentBoss != null)
            {
                var bossStats = GameManager.Instance.currentBoss.GetComponent<MainBossStats>();
                if (bossStats != null)
                {
                    trackedTargets.Add(bossStats);
                    lastHP[bossStats] = bossStats.CurrentHP;
                }
            }
        }

        TrackWeaponsByTag("Turret");
    }

    private void TrackWeaponsByTag(string tag)
    {
        // Bolt: Optimized - avoid scene-wide tag searches by traversing active ships and boss
        if (GameManager.Instance != null)
        {
            var ships = GameManager.Instance.GetActiveEnemyShips();
            if (ships != null)
            {
                foreach (var ship in ships)
                {
                    if (ship != null)
                    {
                        AddTrackedWeaponsFromParent(ship);
                    }
                }
            }
            
            if (GameManager.Instance.currentBoss != null)
            {
                AddTrackedWeaponsFromParent(GameManager.Instance.currentBoss);
            }
            
            // Also handle the specific tag requested, in case it's not part of the standard enemy ships/boss
            // But we only do this if it's NOT one of the standard tags already covered
            if (tag != "Turret" && tag != "SmallCanon" && tag != "BigCanon" && tag != "Enemy")
            {
                GameObject[] weapons = GameObject.FindGameObjectsWithTag(tag);
                foreach (var weaponObj in weapons)
                {
                    if (weaponObj == null) continue;
                    AddTrackedWeapon(weaponObj);
                }
            }

            return;
        }

        // Fallback to tag search if GameManager is not available
        GameObject[] weapons = GameObject.FindGameObjectsWithTag(tag);
        foreach (var weaponObj in weapons)
        {
            if (weaponObj == null) continue;
            AddTrackedWeapon(weaponObj);
        }
    }

    private void AddTrackedWeaponsFromParent(GameObject parent)
    {
        var turrets = parent.GetComponentsInChildren<TurretControl>(true);
        foreach (var t in turrets) AddTrackedWeapon(t.gameObject);

        var cannons = parent.GetComponentsInChildren<SmallCanonControl>(true);
        foreach (var c in cannons) AddTrackedWeapon(c.gameObject);

        var bigCannons = parent.GetComponentsInChildren<BigCanon>(true);
        foreach (var bc in bigCannons) AddTrackedWeapon(bc.gameObject);
    }

    private void AddTrackedWeapon(GameObject weaponObj)
    {
        var turret = weaponObj.GetComponent<TurretControl>();
        if (turret != null && !trackedTargets.Contains(turret))
        {
            trackedTargets.Add(turret);
            lastHP[turret] = turret.currentHP;
            return;
        }

        var smallCanon = weaponObj.GetComponent<SmallCanonControl>();
        if (smallCanon != null && !trackedTargets.Contains(smallCanon))
        {
            trackedTargets.Add(smallCanon);
            lastHP[smallCanon] = smallCanon.currentHP;
            return;
        }

        var bigCanon = weaponObj.GetComponent<BigCanon>();
        if (bigCanon != null && !trackedTargets.Contains(bigCanon))
        {
            trackedTargets.Add(bigCanon);
            lastHP[bigCanon] = bigCanon.currentHP;
        }
    }

    private void TrackEnemyDamage()
    {
        foreach (var target in trackedTargets)
        {
            if (target == null) continue;
            
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
        if (target is EnemyStats es && es != null) return es.CurrentHP;
        if (target is MainBossStats bs && bs != null) return bs.CurrentHP;
        if (target is TurretControl tc && tc != null) return tc.currentHP;
        if (target is SmallCanonControl sc && sc != null) return sc.currentHP;
        if (target is BigCanon bc && bc != null) return bc.currentHP;
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
            expToNextLevel *= 1.5f;
        }
        onLevelUp?.Invoke(currentLevel);
        if (currentLevel >= MAX_LEVEL)
        {
            onMaxLevelReached?.Invoke();
        }
        if (playerPlane != null)
        {
            playerPlane.maxHP = Mathf.RoundToInt(playerPlane.maxHP * nextLvStatsScale);
            playerPlane.attackPoint = Mathf.RoundToInt(playerPlane.attackPoint * nextLvStatsScale);
            playerPlane.Heal(playerPlane.maxHP);
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

    public void OnPlayerSpawned(GameObject player)
    {
        if (player != null)
        {
            playerPlane = player.GetComponent<PlaneStats>();
            cachedWeaponManager = player.GetComponent<PlayerWeaponManager>();
            cachedLaserActive = player.GetComponent<LaserActive>();
        }
    }

    public void RefreshTrackedEnemies()
    {
        FindPlayerAndEnemies();
    }
}
