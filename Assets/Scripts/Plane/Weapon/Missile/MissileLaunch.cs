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
        cachedAutoTargetLock = GetComponent<AutoTargetLock>();
        if (cachedAutoTargetLock == null)
            cachedAutoTargetLock = GetComponentInParent<AutoTargetLock>();
        if (cachedAutoTargetLock == null)
            GameEntityRegistry.TryGetPlayerComponent(out cachedAutoTargetLock);
        
        cachedTargetLockUI = FindObjectOfType<TargetLockUI>();
        
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.RegisterProjectileType("Missile", missileLifetime);
        }
        if (weaponManager != null)
            weaponManager.nextLaunchTime = 0f;
        damageAccumulated = 0f;
    }
    
    private void Update()
    {
        if (weaponManager == null) return;
        
        // Refresh cached references if they're null (lazy initialization)
        RefreshCachedReferencesIfNeeded();
        
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
        // If cachedAutoTargetLock is null, try to find it
        if (cachedAutoTargetLock == null)
        {
            // First try on this GameObject and parent
            cachedAutoTargetLock = GetComponent<AutoTargetLock>();
            if (cachedAutoTargetLock == null)
                cachedAutoTargetLock = GetComponentInParent<AutoTargetLock>();
            
            if (cachedAutoTargetLock == null)
                GameEntityRegistry.TryGetPlayerComponent(out cachedAutoTargetLock);
            
            // Last resort: FindObjectOfType (only when cache is null)
            if (cachedAutoTargetLock == null)
            {
                cachedAutoTargetLock = FindObjectOfType<AutoTargetLock>();
            }
            
        }
        
        // Same for TargetLockUI
        if (cachedTargetLockUI == null)
        {
            cachedTargetLockUI = FindObjectOfType<TargetLockUI>();
        }
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
            autoTargetLock = FindObjectOfType<AutoTargetLock>();
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
                GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
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
                GameObject missile = Instantiate(missilePrefab, spawnPoint.position, Quaternion.LookRotation(guideRay.direction));
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
