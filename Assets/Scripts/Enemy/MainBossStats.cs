using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MainBossStats : MonoBehaviour, IHasHealth
{
    [Header("Health Settings")]
    [Tooltip("Maximum hit points of the boss.")]
    public float maxHP = 500000f;
    [SerializeField, Tooltip("Current HP at runtime.")]
    private float currentHP;

    [Header("Events")]
    public UnityEvent onDeath;

    [Header("Death VFX")]
    [Tooltip("Prefab to spawn when the boss is destroyed.")]
    public GameObject deathVFX;

    [Header("Weapon Control Reference")]
    [Tooltip("Reference to the WeaponDmgControl managing this boss's weapons.")]
    public WeaponDmgControl weaponDmgControl;

    [Header("Side Ships (must be destroyed before boss can take damage)")]
    // No need for a local sideShips list; will use GameManager's activeEnemyShips

    [Header("Boss Shield GameObject (disable to allow damage)")]
    public GameObject bossShield;

    private float lastHPThreshold = 0f;

    // --- Force respawn timer ---
    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;

    // --- Side ship respawn logic at specific HP thresholds ---
    private float[] sideShipRespawnThresholds = new float[] { 250000f, 100000f };
    private int nextSideShipRespawnIndex = 0;

    void Start()
    {
        currentHP = maxHP;
        // Set initial threshold to the next threshold below max HP
        lastHPThreshold = Mathf.Floor(maxHP / 100000f) * 100000f;
        Debug.Log($"[MainBossStats] Start - Max HP: {maxHP}, Initial lastHPThreshold: {lastHPThreshold}");
        
        // Debug weapon control setup
        if (weaponDmgControl != null)
        {
            Debug.Log($"[MainBossStats] WeaponDmgControl assigned: {weaponDmgControl.name}");
            Debug.Log($"[MainBossStats] TurretsManager: {weaponDmgControl.turretsManager}");
            Debug.Log($"[MainBossStats] SmallCanonManager: {weaponDmgControl.smallCanonManager}");
        }
        else
        {
            Debug.LogError($"[MainBossStats] WeaponDmgControl is null! Please assign it in the inspector.");
        }
        
        UpdateShieldStatus();
        nextSideShipRespawnIndex = 0;
    }

    void Update()
    {
        UpdateShieldStatus();
        CheckWeaponRespawnByHP();
        CheckSideShipRespawnByHP();

        // DEBUG: Press 'K' to destroy all enemy side ships instantly
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("[MainBossStats] DEBUG: Destroying all enemy side ships!");
            DestroyAllSideShips();
        }

        // Check if all weapons are inactive
        bool allWeaponsInactive = true;
        if (weaponDmgControl != null)
        {
            // Small canons
            if (weaponDmgControl.smallCanonManager != null)
            {
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null && canon.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Turrets
            if (weaponDmgControl.turretsManager != null)
            {
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null && turret.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Big canons
            BigCanon[] bigCanons = GetComponentsInChildren<BigCanon>(true);
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null && bigCanon.gameObject.activeInHierarchy)
                    allWeaponsInactive = false;
            }
        }

        // Start or update the force respawn timer
        if (allWeaponsInactive && forceRespawnTimer < 0f)
        {
            forceRespawnTimer = FORCE_RESPAWN_DELAY;
            Debug.Log($"[MainBossStats] All weapons inactive. Starting force respawn timer: {FORCE_RESPAWN_DELAY}s");
        }
        else if (!allWeaponsInactive)
        {
            forceRespawnTimer = -1f; // Reset timer if any weapon is revived
        }

        // Countdown and trigger force respawn if needed
        if (forceRespawnTimer > 0f)
        {
            forceRespawnTimer -= Time.deltaTime;
            if (forceRespawnTimer <= 0f)
            {
                forceRespawnTimer = -1f;
                Debug.Log("[MainBossStats] Force respawning all weapons!");
                if (weaponDmgControl != null)
                {
                    weaponDmgControl.ReviveAllTurrets();
                    weaponDmgControl.ReviveAllCanons();
                    weaponDmgControl.ReviveAllBigCanons();
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0 || currentHP <= 0)
            return;
            
        Debug.Log($"[MainBossStats] TakeDamage called with {amount} damage. Current HP: {currentHP}");
        
        if (!AreAllSideShipsDestroyed())
        {
            Debug.Log($"{name} cannot take damage until all side ships are destroyed!");
            return;
        }
        if (weaponDmgControl != null)
        {
            bool allWeaponsInactive = true;
            // Check small canons
            if (weaponDmgControl.smallCanonManager != null)
            {
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null && canon.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Check turrets
            if (weaponDmgControl.turretsManager != null)
            {
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null && turret.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Check big canons
            BigCanon[] bigCanons = GetComponentsInChildren<BigCanon>(true);
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null && bigCanon.gameObject.activeInHierarchy)
                    allWeaponsInactive = false;
            }
            if (!allWeaponsInactive)
            {
                Debug.Log($"{name} cannot take damage until all weapons are inactive!");
                return;
            }
        }
        
        float oldHP = currentHP;
        currentHP -= amount;
        Debug.Log($"[MainBossStats] HP changed from {oldHP} to {currentHP} (lost {oldHP - currentHP})");
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            HandleDeath();
        }
        // Reset force respawn timer on damage
        forceRespawnTimer = -1f;
    }

    private void HandleDeath()
    {
        Debug.Log($"{name} is destroyed!");
        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, transform.rotation);
            Destroy(vfx, 5f);
        }
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    // Check if all side ships are destroyed
    private bool AreAllSideShipsDestroyed()
    {
        if (GameManager.Instance == null)
            return true; // If no GameManager, assume all destroyed (fail-safe)
        var enemyShips = GameManager.Instance.GetActiveEnemyShips();
        foreach (var shipGO in enemyShips)
        {
            if (shipGO != null)
            {
                var stats = shipGO.GetComponent<EnemyStats>();
                if (stats != null && stats.CurrentHP > 0)
                    return false;
            }
        }
        return true;
    }

    // Enable/disable boss shield based on side ship status
    private void UpdateShieldStatus()
    {
        bool allDestroyed = AreAllSideShipsDestroyed();
        if (bossShield != null)
            bossShield.SetActive(!allDestroyed);
    }

    // Force weapon respawn every 100,000 HP lost
    private void CheckWeaponRespawnByHP()
    {
        // Calculate the current threshold based on HP
        float hpThreshold = Mathf.Floor(currentHP / 100000f) * 100000f;
        
        // Ensure we don't go below 0
        hpThreshold = Mathf.Max(hpThreshold, 0f);
        
        Debug.Log($"[MainBossStats] Current HP: {currentHP}, HP Threshold: {hpThreshold}, Last HP Threshold: {lastHPThreshold}");
        
        // Check if we've crossed a threshold (HP dropped below a 100k mark)
        if (hpThreshold < lastHPThreshold)
        {
            Debug.Log($"[MainBossStats] HP threshold crossed! Triggering weapon respawn. HP: {currentHP}");
            ForceRespawnAllWeapons();
            lastHPThreshold = hpThreshold;
        }
    }

    // Call respawn methods on WeaponDmgControl
    private void ForceRespawnAllWeapons()
    {
        Debug.Log($"[MainBossStats] ForceRespawnAllWeapons called. weaponDmgControl: {weaponDmgControl}");
        if (weaponDmgControl != null)
        {
            Debug.Log($"[MainBossStats] Calling weapon respawn methods...");
            weaponDmgControl.ReviveAllTurrets();
            weaponDmgControl.ReviveAllCanons();
            weaponDmgControl.ReviveAllBigCanons();
            Debug.Log($"{name} forced all weapons to respawn at {currentHP} HP!");
        }
        else
        {
            Debug.LogError($"[MainBossStats] weaponDmgControl is null! Make sure it's assigned in the inspector.");
        }
    }

    // Destroys all enemy side ships for testing
    private void DestroyAllSideShips()
    {
        if (GameManager.Instance == null) return;
        var enemyShips = GameManager.Instance.GetActiveEnemyShips();
        foreach (var shipGO in enemyShips)
        {
            if (shipGO != null)
            {
                var stats = shipGO.GetComponent<EnemyStats>();
                if (stats != null && stats.CurrentHP > 0)
                {
                    stats.TakeDamage(stats.CurrentHP);
                    Debug.Log($"[MainBossStats] Destroyed side ship: {shipGO.name}");
                }
            }
        }
    }

    // --- Side ship respawn logic at specific HP thresholds ---
    private void CheckSideShipRespawnByHP()
    {
        if (nextSideShipRespawnIndex >= sideShipRespawnThresholds.Length)
            return;
        float threshold = sideShipRespawnThresholds[nextSideShipRespawnIndex];
        if (currentHP <= threshold && AreAllSideShipsDestroyed())
        {
            Debug.Log($"[MainBossStats] HP dropped to {currentHP} (threshold {threshold}): Respawning all enemy side ships and reactivating shield.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnEnemySideShips();
            }
            else
            {
                Debug.LogError("[MainBossStats] GameManager.Instance is null! Cannot respawn side ships.");
            }
            // Reactivate shield (will be handled by UpdateShieldStatus in next frame)
            if (bossShield != null)
                bossShield.SetActive(true);
            nextSideShipRespawnIndex++;
        }
    }

    // Read-only accessors
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    // Debug method to manually trigger weapon respawn
    [ContextMenu("Debug: Force Weapon Respawn")]
    public void DebugForceWeaponRespawn()
    {
        Debug.Log($"[MainBossStats] Manual weapon respawn triggered!");
        ForceRespawnAllWeapons();
    }

    // Debug method to check weapon status
    [ContextMenu("Debug: Check Weapon Status")]
    public void DebugCheckWeaponStatus()
    {
        Debug.Log($"[MainBossStats] === WEAPON STATUS CHECK ===");
        
        if (weaponDmgControl != null)
        {
            // Check turrets
            if (weaponDmgControl.turretsManager != null)
            {
                Debug.Log($"[MainBossStats] TurretsManager: {weaponDmgControl.turretsManager.name}");
                Debug.Log($"[MainBossStats] Current Turret Count: {weaponDmgControl.turretsManager.currentTurretCount}");
                Debug.Log($"[MainBossStats] Max Turret Count: {weaponDmgControl.turretsManager.maxTurretCount}");
                Debug.Log($"[MainBossStats] Active Turrets: {weaponDmgControl.turretsManager.turrets.Count}");
                
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null)
                    {
                        Debug.Log($"[MainBossStats] Turret: {turret.name}, Active: {turret.gameObject.activeInHierarchy}, HP: {turret.currentHP}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[MainBossStats] TurretsManager is null!");
            }
            
            // Check small canons
            if (weaponDmgControl.smallCanonManager != null)
            {
                Debug.Log($"[MainBossStats] SmallCanonManager: {weaponDmgControl.smallCanonManager.name}");
                Debug.Log($"[MainBossStats] Current Canon Count: {weaponDmgControl.smallCanonManager.currentCanonCount}");
                Debug.Log($"[MainBossStats] Max Canon Count: {weaponDmgControl.smallCanonManager.maxCanonCount}");
                Debug.Log($"[MainBossStats] Active Canons: {weaponDmgControl.smallCanonManager.canons.Count}");
                
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null)
                    {
                        Debug.Log($"[MainBossStats] Canon: {canon.name}, Active: {canon.gameObject.activeInHierarchy}, HP: {canon.currentHP}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[MainBossStats] SmallCanonManager is null!");
            }
            
            // Check big canons
            BigCanon[] bigCanons = FindObjectsOfType<BigCanon>();
            Debug.Log($"[MainBossStats] Big Canons Found: {bigCanons.Length}");
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null)
                {
                    Debug.Log($"[MainBossStats] BigCanon: {bigCanon.name}, Active: {bigCanon.gameObject.activeInHierarchy}, HP: {bigCanon.currentHP}");
                }
            }
        }
        else
        {
            Debug.LogError($"[MainBossStats] WeaponDmgControl is null!");
        }
        
        Debug.Log($"[MainBossStats] === END WEAPON STATUS ===");
    }
}
