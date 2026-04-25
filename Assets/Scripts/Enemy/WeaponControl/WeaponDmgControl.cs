using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDmgControl : MonoBehaviour
{
    [Header("Data Asset (optional — overrides inline values below)")]
    [SerializeField] private WeaponConfigSO weaponConfig;

    [Header("Bullet Turret Settings")]
    [SerializeField] private float bulletDamage = 20f;
    [SerializeField] private float turretFireRate = 0.1f;
    [SerializeField] private float turretFireRange = 100f;

    [Header("Laser Canon Settings")]
    [SerializeField] private float smallCanonDamage = 50f;
    [SerializeField] private float smallCanonFireRate = 0.05f;
    [SerializeField] private float smallCanonFireRange = 100f;

    [Header("Big Canon Settings")]
    [SerializeField] private float bigCanonDamage = 100f;
    [SerializeField] private float bigCanonFireRate = 0.1f;
    [SerializeField] private float bigCanonFireRange = 200f;

    [Header("Turret Revive System")]
    public TurretsManager turretsManager;
    public float turretReviveTime = 60f;
    private float turretReviveTimer;
    private bool turretReviveRunning;

    [Header("Cannon Revive System")]
    public SmallCanonManager smallCanonManager;
    public float cannonReviveTime = 60f;
    private float cannonReviveTimer;
    private bool cannonReviveRunning;

    [Header("Big Cannon Revive System")]
    public float bigCannonReviveTime = 90f;
    private float bigCannonReviveTimer;
    private bool bigCannonReviveRunning;

    private BigCanon[] cachedBigCanons;

    private bool cachedAllWeaponsInactive;
    private float weaponCacheTimer;
    private const float WEAPON_CACHE_INTERVAL = 0.5f;

    public bool AllWeaponsInactive => cachedAllWeaponsInactive;

    void Awake()
    {
        if (weaponConfig != null)
        {
            bulletDamage       = weaponConfig.bulletDamage;
            turretFireRate     = weaponConfig.turretFireRate;
            turretFireRange    = weaponConfig.turretFireRange;
            smallCanonDamage   = weaponConfig.smallCanonDamage;
            smallCanonFireRate = weaponConfig.smallCanonFireRate;
            smallCanonFireRange = weaponConfig.smallCanonFireRange;
            bigCanonDamage     = weaponConfig.bigCanonDamage;
            bigCanonFireRate   = weaponConfig.bigCanonFireRate;
            bigCanonFireRange  = weaponConfig.bigCanonFireRange;
        }

        cachedBigCanons = GetComponentsInChildren<BigCanon>(true);
        cachedAllWeaponsInactive = ComputeAllWeaponsInactive();
    }

    void Update()
    {
        TickRevive(ref turretReviveTimer, ref turretReviveRunning,
            () => { if (turretsManager != null && turretsManager.currentTurretCount > 0) ReviveAllTurrets(); });
        TickRevive(ref cannonReviveTimer, ref cannonReviveRunning,
            () => { if (smallCanonManager != null && smallCanonManager.currentCanonCount > 0) ReviveAllCanons(); });
        TickRevive(ref bigCannonReviveTimer, ref bigCannonReviveRunning, ReviveAllBigCanons);

        weaponCacheTimer -= Time.deltaTime;
        if (weaponCacheTimer <= 0f)
        {
            weaponCacheTimer = WEAPON_CACHE_INTERVAL;
            cachedAllWeaponsInactive = ComputeAllWeaponsInactive();
        }
    }

    private void TickRevive(ref float timer, ref bool running, Action onRevive)
    {
        if (!running) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { running = false; onRevive(); }
    }

    // Called by weapons on death or revive — refreshes the cache immediately and
    // resets the periodic timer so we don't double-compute 0.5s later.
    public void NotifyWeaponStateChanged()
    {
        cachedAllWeaponsInactive = ComputeAllWeaponsInactive();
        weaponCacheTimer = WEAPON_CACHE_INTERVAL;
    }

    private bool ComputeAllWeaponsInactive()
    {
        if (smallCanonManager != null)
            foreach (var c in smallCanonManager.canons)
                if (c != null && c.gameObject.activeInHierarchy) return false;

        if (turretsManager != null)
            foreach (var t in turretsManager.turrets)
                if (t != null && t.gameObject.activeInHierarchy) return false;

        if (cachedBigCanons != null)
            foreach (var b in cachedBigCanons)
                if (b != null && b.gameObject.activeInHierarchy) return false;

        return true;
    }

    public float GetBulletDamage()      => bulletDamage;
    public float GetTurretFireRate()    => turretFireRate;
    public float GetTurretFireRange()   => turretFireRange;
    public float GetSmallCanonDamage()  => smallCanonDamage;
    public float GetSmallCanonFireRate() => smallCanonFireRate;
    public float GetSmallCanonFireRange() => smallCanonFireRange;
    public float GetBigCanonDamage()    => bigCanonDamage;
    public float GetBigCanonFireRate()  => bigCanonFireRate;
    public float GetBigCanonFireRange() => bigCanonFireRange;
    public void  SetBulletDamage(float damage) { bulletDamage = damage; }

    public void OnTurretDestroyed()
    {
        if (turretsManager != null)
            turretsManager.currentTurretCount = Mathf.Max(turretsManager.currentTurretCount - 1, 0);
        if (!turretReviveRunning) { turretReviveTimer = turretReviveTime; turretReviveRunning = true; }
        NotifyWeaponStateChanged();
    }

    public void OnCanonDestroyed()
    {
        if (smallCanonManager != null)
            smallCanonManager.currentCanonCount = Mathf.Max(smallCanonManager.currentCanonCount - 1, 0);
        if (!cannonReviveRunning) { cannonReviveTimer = cannonReviveTime; cannonReviveRunning = true; }
        NotifyWeaponStateChanged();
    }

    public void OnBigCanonDestroyed()
    {
        if (!bigCannonReviveRunning) { bigCannonReviveTimer = bigCannonReviveTime; bigCannonReviveRunning = true; }
        NotifyWeaponStateChanged();
    }

    public void ReviveAllTurrets()
    {
        if (turretsManager?.turrets == null || turretsManager.turrets.Count == 0) return;
        foreach (var turret in turretsManager.turrets)
        {
            if (turret != null) { turret.currentHP = turretsManager.turretHP; turret.gameObject.SetActive(true); }
        }
        turretsManager.currentTurretCount = turretsManager.maxTurretCount;
        NotifyWeaponStateChanged();
    }

    public void ReviveAllCanons()
    {
        if (smallCanonManager?.canons == null || smallCanonManager.canons.Count == 0) return;
        foreach (var canon in smallCanonManager.canons)
        {
            if (canon != null) { canon.currentHP = smallCanonManager.canonHP; canon.gameObject.SetActive(true); }
        }
        smallCanonManager.currentCanonCount = smallCanonManager.maxCanonCount;
        NotifyWeaponStateChanged();
    }

    public void ReviveAllBigCanons()
    {
        if (cachedBigCanons == null) cachedBigCanons = GetComponentsInChildren<BigCanon>(true);
        foreach (var b in cachedBigCanons)
        {
            if (b != null) { b.currentHP = b.maxHP; b.gameObject.SetActive(true); }
        }
        NotifyWeaponStateChanged();
    }
}
