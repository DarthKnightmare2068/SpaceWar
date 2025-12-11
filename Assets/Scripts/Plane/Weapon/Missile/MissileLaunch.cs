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
    private PlaneStats playerPlane;
    
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> missileSpawnPoints = new List<Transform>();
    
    public bool useAutoTargetLock = true;
    
    public float damageAccumulated = 0f;
    
    private void Start()
    {
        if (missilePrefab == null)
        {
            return;
        }
        
        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<PlayerWeaponManager>();
        }
        playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.RegisterProjectileType("Missile", missileLifetime);
        }
        weaponManager.nextLaunchTime = 0f;
        damageAccumulated = 0f;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            useAutoTargetLock = !useAutoTargetLock;
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

        AutoTargetLock autoTargetLock = FindObjectOfType<AutoTargetLock>();
        Transform target = null;
        if (autoTargetLock != null && autoTargetLock.HasTarget())
        {
            target = autoTargetLock.GetLockedTarget();
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (distanceToTarget <= weaponManager.missileFireRange)
            {
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            if (spawnPoint != null)
            {
                GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
                
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
        }

        weaponManager.UseMissile();
        
        if (AudioSetting.Instance != null && AudioSetting.Instance.missileSound != null)
        {
            AudioSource.PlayClipAtPoint(AudioSetting.Instance.missileSound, transform.position, AudioSetting.Instance.missileSFXVolume);
        }
    }
    
    private void LaunchDumbMissile()
    {
        if (!weaponManager.CanFireMissile() || Time.time < weaponManager.nextLaunchTime) return;
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
