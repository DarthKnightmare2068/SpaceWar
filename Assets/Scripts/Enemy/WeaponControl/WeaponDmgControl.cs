using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDmgControl : MonoBehaviour
{
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
    private float turretReviveTimer = 0f;
    private bool turretReviveTimerRunning = false;

    [Header("Cannon Revive System")]
    public SmallCanonManager smallCanonManager;
    public float cannonReviveTime = 60f;
    private float cannonReviveTimer = 0f;
    private bool cannonReviveTimerRunning = false;

    [Header("Big Cannon Revive System")]
    public float bigCannonReviveTime = 90f;
    private float bigCannonReviveTimer = 0f;
    private bool bigCannonReviveTimerRunning = false;

    void Update()
    {
        if (turretReviveTimerRunning)
        {
            turretReviveTimer -= Time.deltaTime;
            if (turretReviveTimer <= 0f)
            {
                turretReviveTimerRunning = false;
                if (turretsManager != null && turretsManager.currentTurretCount > 0)
                {
                    ReviveAllTurrets();
                }
            }
        }
        if (cannonReviveTimerRunning)
        {
            cannonReviveTimer -= Time.deltaTime;
            if (cannonReviveTimer <= 0f)
            {
                cannonReviveTimerRunning = false;
                if (smallCanonManager != null && smallCanonManager.currentCanonCount > 0)
                {
                    ReviveAllCanons();
                }
            }
        }
        if (bigCannonReviveTimerRunning)
        {
            bigCannonReviveTimer -= Time.deltaTime;
            if (bigCannonReviveTimer <= 0f)
            {
                bigCannonReviveTimerRunning = false;
                ReviveAllBigCanons();
            }
        }
    }

    public float GetBulletDamage() => bulletDamage;
    public float GetTurretFireRate() => turretFireRate;
    public float GetTurretFireRange() => turretFireRange;
    public float GetSmallCanonDamage() => smallCanonDamage;
    public float GetSmallCanonFireRate() => smallCanonFireRate;
    public float GetSmallCanonFireRange() => smallCanonFireRange;
    public float GetBigCanonDamage() => bigCanonDamage;
    public float GetBigCanonFireRate() => bigCanonFireRate;
    public float GetBigCanonFireRange() => bigCanonFireRange;

    public void SetBulletDamage(float damage)
    {
        bulletDamage = damage;
    }

    public void OnTurretDestroyed()
    {
        if (turretsManager != null)
            turretsManager.currentTurretCount = Mathf.Max(turretsManager.currentTurretCount - 1, 0);
        if (!turretReviveTimerRunning)
        {
            turretReviveTimer = turretReviveTime;
            turretReviveTimerRunning = true;
        }
    }

    public void OnCanonDestroyed()
    {
        if (smallCanonManager != null)
        {
            smallCanonManager.currentCanonCount = Mathf.Max(smallCanonManager.currentCanonCount - 1, 0);
        }
        if (!cannonReviveTimerRunning)
        {
            cannonReviveTimer = cannonReviveTime;
            cannonReviveTimerRunning = true;
        }
    }

    public void OnBigCanonDestroyed()
    {
        if (!bigCannonReviveTimerRunning)
        {
            bigCannonReviveTimer = bigCannonReviveTime;
            bigCannonReviveTimerRunning = true;
        }
    }

    public void ReviveAllTurrets()
    {
        if (turretsManager == null || turretsManager.turrets == null || turretsManager.turrets.Count == 0)
        {
            return;
        }
        foreach (var turret in turretsManager.turrets)
        {
            if (turret != null)
            {
                turret.currentHP = turretsManager.turretHP;
                turret.gameObject.SetActive(true);
            }
        }
        turretsManager.currentTurretCount = turretsManager.maxTurretCount;
    }

    public void ReviveAllCanons()
    {
        if (smallCanonManager == null || smallCanonManager.canons == null || smallCanonManager.canons.Count == 0)
        {
            return;
        }
        foreach (var canon in smallCanonManager.canons)
        {
            if (canon != null)
            {
                canon.currentHP = smallCanonManager.canonHP;
                canon.gameObject.SetActive(true);
            }
        }
        smallCanonManager.currentCanonCount = smallCanonManager.maxCanonCount;
    }

    public void ReviveAllBigCanons()
    {
        BigCanon[] bigCanons = GameObject.FindObjectsOfType<BigCanon>(true);
        foreach (var bigCanon in bigCanons)
        {
            if (bigCanon != null)
            {
                bigCanon.currentHP = bigCanon.maxHP;
                bigCanon.gameObject.SetActive(true);
            }
        }
    }
}
