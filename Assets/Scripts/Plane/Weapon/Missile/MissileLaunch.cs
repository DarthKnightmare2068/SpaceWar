using UnityEngine;
using System.Collections.Generic;
using System.Collections; // In case it's needed
using System;
// Add this to fix PlayerStats reference
using System.Linq;
// Add this to fix PlayerStats reference
// If PlayerStats is in the default namespace, this is enough
// If not, specify the correct namespace, e.g., using MyGameNamespace;
// using MyGameNamespace; // Uncomment and set if needed

public class MissileLaunch : MonoBehaviour
{
    [Header("Missile Settings")]
    [SerializeField] private float reloadThreshold = 1000f; // Damage needed for 1 missile
    [Tooltip("The missile prefab to spawn")]
    [SerializeField] private GameObject missilePrefab;
    [Tooltip("Speed of the missile in units per second")]
    [SerializeField] private float missileSpeed = 50f;
    [Tooltip("How long the missile will live before being destroyed")]
    [SerializeField] private float missileLifetime = 10f;
    public PlayerWeaponManager weaponManager;
    private PlaneStats playerPlane; // Cache player stats
    
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> missileSpawnPoints = new List<Transform>();
    
    public bool useAutoTargetLock = true;
    
    // Missile reload variables
    public float damageAccumulated = 0f;
    
    private void Start()
    {
        if (missilePrefab == null)
        {
            return;
        }
        
        // Get PlayerWeaponManager if not set
        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<PlayerWeaponManager>();
        }
        // Cache player stats
        playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        // Register with BulletPool
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.RegisterProjectileType("Missile", missileLifetime);
        }
        weaponManager.nextLaunchTime = 0f;
        damageAccumulated = 0f;
    }
    
    private void Update()
    {
        // Toggle missile mode with C key
        if (Input.GetKeyDown(KeyCode.C))
        {
            useAutoTargetLock = !useAutoTargetLock;
            Debug.Log($"Missile mode switched. useAutoTargetLock = {useAutoTargetLock}");
            // Show missile mode UI
            var targetLockUI = FindObjectOfType<TargetLockUI>();
            if (targetLockUI != null)
                targetLockUI.ShowMissileMode();
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
        if (!weaponManager.CanFireMissile() || Time.time < weaponManager.nextLaunchTime) return;

        // Get the current locked target from AutoTargetLock
        AutoTargetLock autoTargetLock = FindObjectOfType<AutoTargetLock>();
        Transform target = null;
        if (autoTargetLock != null && autoTargetLock.HasTarget())
        {
            target = autoTargetLock.GetLockedTarget();
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Check if target is in missile range
            if (distanceToTarget <= weaponManager.missileFireRange)
            {
            }
            else
            {
                return; // Don't launch if target is out of range
            }
        }
        else
        {
            return; // Don't launch if no target
        }

        // Launch missiles from each spawn point
        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            if (spawnPoint != null)
            {
                GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
                
                // Set up missile components
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

                // Set the missile's layer to Player
                missile.layer = LayerMask.NameToLayer("Player");
                // Set the missile's tag to PlayerWeapon
                missile.tag = "PlayerWeapon";
            }
        }

        weaponManager.UseMissile();
        
        // Play missile launch sound
        if (AudioSetting.Instance != null && AudioSetting.Instance.missileSound != null)
        {
            AudioSource.PlayClipAtPoint(AudioSetting.Instance.missileSound, transform.position, AudioSetting.Instance.missileSFXVolume);
        }
    }
    
    private void LaunchDumbMissile()
    {
        if (!weaponManager.CanFireMissile() || Time.time < weaponManager.nextLaunchTime) return;
        // Use the same ray as the machine gun for perfect alignment
        Ray guideRay = weaponManager.GetCurrentTargetRay();
        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            if (spawnPoint != null)
            {
                GameObject missile = Instantiate(missilePrefab, spawnPoint.position, Quaternion.LookRotation(guideRay.direction));
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
    
    // Method to add spawn points from Unity Inspector
    public void AddSpawnPoint(Transform spawnPoint)
    {
        if (!missileSpawnPoints.Contains(spawnPoint))
        {
            missileSpawnPoints.Add(spawnPoint);
        }
    }
    
    // Method to remove spawn points
    public void RemoveSpawnPoint(Transform spawnPoint)
    {
        missileSpawnPoints.Remove(spawnPoint);
    }
    
    // Getter for time until next launch
    public float GetTimeUntilNextLaunch()
    {
        return Mathf.Max(0f, weaponManager.nextLaunchTime - Time.time);
    }
} 