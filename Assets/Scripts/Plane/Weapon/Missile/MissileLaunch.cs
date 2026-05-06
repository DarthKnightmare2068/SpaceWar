using UnityEngine;
using System.Collections.Generic;

public class MissileLaunch : MonoBehaviour
{
    [Header("Missile Settings")]
    [SerializeField] private float reloadThreshold = 1000f;
    [Tooltip("The missile prefab to spawn")]
    [SerializeField] private GameObject missilePrefab;
    [Tooltip("Speed of the missile in units per second")]
    [SerializeField] private float missileSpeed = 50f;
    [Tooltip("How long the missile will live before being destroyed")]
    [SerializeField] private float missileLifetime = 10f;
    public PlayerWeaponManager weaponManager;
    
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> missileSpawnPoints = new List<Transform>();
    
    public bool useAutoTargetLock = true;
    
    public float damageAccumulated = 0f;
    
    // Cached references to avoid FindObjectOfType calls
    private AutoTargetLock cachedAutoTargetLock;
    private TargetLockUI cachedTargetLockUI;
    
    private void Start()
    {
        if (missilePrefab == null)
        {
            return;
        }
        
        if (weaponManager == null)
        {
            weaponManager = GetComponent<PlayerWeaponManager>();
            if (weaponManager == null)
                weaponManager = GetComponentInParent<PlayerWeaponManager>();
        }
        
        // Cache references once at Start instead of using FindObjectOfType in Update
        cachedAutoTargetLock = Resolver.Find<AutoTargetLock>(this);
        
        cachedTargetLockUI = FindAnyObjectByType<TargetLockUI>();
        
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.RegisterProjectileType("Missile", missileLifetime);
        }
        if (weaponManager != null)
            weaponManager.nextLaunchTime = 0f;
        damageAccumulated = 0f;
    }
    
    private void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
    }

    private void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    private void HandlePlayerChanged(GameObject player)
    {
        // Invalidate caches; lazy-resolve next time they're needed.
        cachedAutoTargetLock = null;
        cachedTargetLockUI = null;
        RefreshCachedReferencesIfNeeded();
    }

    private void Update()
    {
        if (weaponManager == null) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            useAutoTargetLock = !useAutoTargetLock;
            // Use cached reference instead of FindObjectOfType
            if (cachedTargetLockUI != null)
                cachedTargetLockUI.ShowMissileMode();
        }

        
        if (Input.GetMouseButtonDown(1) && Time.time >= weaponManager.nextLaunchTime)
        {
            if (useAutoTargetLock)
            {
                LaunchMissile();
            }
            else
            {
                LaunchDumbMissile();
            }
            weaponManager.nextLaunchTime = Time.time + weaponManager.missileLaunchDelay;
        }
    }
    
    private void RefreshCachedReferencesIfNeeded()
    {
        if (cachedAutoTargetLock == null)
        {
            cachedAutoTargetLock = Resolver.Find<AutoTargetLock>(this);
            if (cachedAutoTargetLock == null)
                cachedAutoTargetLock = FindAnyObjectByType<AutoTargetLock>();
        }

        if (cachedTargetLockUI == null)
            cachedTargetLockUI = FindAnyObjectByType<TargetLockUI>();
    }
    
    public void AddDamagePoints(float damage)
    {
        ReloadMissiles(damage);
    }
    
    public void ReloadMissiles(float damage)
    {
        damageAccumulated += damage;
        while (damageAccumulated >= reloadThreshold && weaponManager.currentMissiles < weaponManager.maxMissiles)
        {
            damageAccumulated -= reloadThreshold;
            weaponManager.currentMissiles++;
        }
    }
    
    private void LaunchMissile()
    {
        if (!weaponManager.CanFireMissile() || Time.time < weaponManager.nextLaunchTime)
        {
            return;
        }

        // Ensure we have a valid AutoTargetLock reference
        RefreshCachedReferencesIfNeeded();
        
        // Use cached reference, with fallback to FindObjectOfType if still null
        AutoTargetLock autoTargetLock = cachedAutoTargetLock;
        if (autoTargetLock == null)
        {
            autoTargetLock = FindAnyObjectByType<AutoTargetLock>();
        }
        
        Transform target = null;
        if (autoTargetLock != null && autoTargetLock.HasTarget())
        {
            target = autoTargetLock.GetLockedTarget();
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget > weaponManager.missileFireRange)
            {
                return;
            }
        }
        else
        {
            return;
        }

        int spawnedCount = 0;
        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            if (spawnPoint != null)
            {
                GameObject missile = PlayerProjectilePool.Instance != null
                    ? PlayerProjectilePool.Instance.GetMissile(spawnPoint.position, spawnPoint.rotation, missileLifetime)
                    : Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
                if (missile == null) continue;
                spawnedCount++;

                MissileAutoLock missileLock = missile.GetComponent<MissileAutoLock>();
                if (missileLock != null)
                {
                    missileLock.SetTarget(target);
                }

                MissileController missileController = missile.GetComponent<MissileController>();
                if (missileController != null)
                {
                    missileController.Initialize(missileSpeed, missileLifetime);
                    missileController.SetShooter(this.gameObject);
                    missileController.useAutoTargetLock = true;
                }

                missile.layer = LayerMask.NameToLayer("Player");
                missile.tag = "PlayerWeapon";
            }
            else
            {
            }
        }

        weaponManager.UseMissile();
        
        if (AudioSetting.Instance != null && AudioSetting.Instance.missileSound != null)
        {
            AudioSource.PlayClipAtPoint(AudioSetting.Instance.missileSound, transform.position, AudioSetting.Instance.missileSFXVolume);
        }
    }
    
    private void LaunchDumbMissile()
    {
        if (!weaponManager.CanFireMissile() || Time.time < weaponManager.nextLaunchTime)
        {
            return;
        }
        
        Ray guideRay = weaponManager.GetCurrentTargetRay();
        
        int spawnedCount = 0;
        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Quaternion missileRot = Quaternion.LookRotation(guideRay.direction);
                GameObject missile = PlayerProjectilePool.Instance != null
                    ? PlayerProjectilePool.Instance.GetMissile(spawnPoint.position, missileRot, missileLifetime)
                    : Instantiate(missilePrefab, spawnPoint.position, missileRot);
                if (missile == null) continue;
                spawnedCount++;

                MissileController missileController = missile.GetComponent<MissileController>();
                if (missileController != null)
                {
                    missileController.Initialize(missileSpeed, missileLifetime);
                    missileController.SetShooter(this.gameObject);
                    missileController.useAutoTargetLock = false;
                }
                missile.layer = LayerMask.NameToLayer("Player");
                missile.tag = "PlayerWeapon";
            }
        }
        
        weaponManager.UseMissile();
        if (AudioSetting.Instance != null && AudioSetting.Instance.missileSound != null)
        {
            AudioSource.PlayClipAtPoint(AudioSetting.Instance.missileSound, transform.position, AudioSetting.Instance.missileSFXVolume);
        }
    }
    
    public void AddSpawnPoint(Transform spawnPoint)
    {
        if (!missileSpawnPoints.Contains(spawnPoint))
        {
            missileSpawnPoints.Add(spawnPoint);
        }
    }
    
    public void RemoveSpawnPoint(Transform spawnPoint)
    {
        missileSpawnPoints.Remove(spawnPoint);
    }
    
    public float GetTimeUntilNextLaunch()
    {
        return Mathf.Max(0f, weaponManager.nextLaunchTime - Time.time);
    }
    
}
