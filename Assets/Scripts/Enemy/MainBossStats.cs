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

    [Header("Boss Shield GameObject (disable to allow damage)")]
    public GameObject bossShield;

    private float lastHPThreshold = 0f;
    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;
    private float[] sideShipRespawnThresholds = new float[] { 250000f, 100000f };
    private int nextSideShipRespawnIndex = 0;

    void Start()
    {
        currentHP = maxHP;
        lastHPThreshold = Mathf.Floor(maxHP / 100000f) * 100000f;
        UpdateShieldStatus();
        nextSideShipRespawnIndex = 0;
    }

    void Update()
    {
        UpdateShieldStatus();
        CheckWeaponRespawnByHP();
        CheckSideShipRespawnByHP();

        bool allWeaponsInactive = true;
        if (weaponDmgControl != null)
        {
            if (weaponDmgControl.smallCanonManager != null)
            {
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null && canon.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            if (weaponDmgControl.turretsManager != null)
            {
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null && turret.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            BigCanon[] bigCanons = GetComponentsInChildren<BigCanon>(true);
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null && bigCanon.gameObject.activeInHierarchy)
                    allWeaponsInactive = false;
            }
        }

        if (allWeaponsInactive && forceRespawnTimer < 0f)
        {
            forceRespawnTimer = FORCE_RESPAWN_DELAY;
        }
        else if (!allWeaponsInactive)
        {
            forceRespawnTimer = -1f;
        }

        if (forceRespawnTimer > 0f)
        {
            forceRespawnTimer -= Time.deltaTime;
            if (forceRespawnTimer <= 0f)
            {
                forceRespawnTimer = -1f;
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
        
        if (!AreAllSideShipsDestroyed())
            return;

        if (weaponDmgControl != null)
        {
            bool allWeaponsInactive = true;
            if (weaponDmgControl.smallCanonManager != null)
            {
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null && canon.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            if (weaponDmgControl.turretsManager != null)
            {
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null && turret.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            BigCanon[] bigCanons = GetComponentsInChildren<BigCanon>(true);
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null && bigCanon.gameObject.activeInHierarchy)
                    allWeaponsInactive = false;
            }
            if (!allWeaponsInactive)
                return;
        }
        
        currentHP -= amount;
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            HandleDeath();
        }
        forceRespawnTimer = -1f;
    }

    private void HandleDeath()
    {
        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, transform.rotation);
            Destroy(vfx, 5f);
        }
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    private bool AreAllSideShipsDestroyed()
    {
        if (GameManager.Instance == null)
            return true;
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

    private void UpdateShieldStatus()
    {
        bool allDestroyed = AreAllSideShipsDestroyed();
        if (bossShield != null)
            bossShield.SetActive(!allDestroyed);
    }

    private void CheckWeaponRespawnByHP()
    {
        float hpThreshold = Mathf.Floor(currentHP / 100000f) * 100000f;
        hpThreshold = Mathf.Max(hpThreshold, 0f);
        
        if (hpThreshold < lastHPThreshold)
        {
            ForceRespawnAllWeapons();
            lastHPThreshold = hpThreshold;
        }
    }

    private void ForceRespawnAllWeapons()
    {
        if (weaponDmgControl != null)
        {
            weaponDmgControl.ReviveAllTurrets();
            weaponDmgControl.ReviveAllCanons();
            weaponDmgControl.ReviveAllBigCanons();
        }
    }

    private void CheckSideShipRespawnByHP()
    {
        if (nextSideShipRespawnIndex >= sideShipRespawnThresholds.Length)
            return;
        float threshold = sideShipRespawnThresholds[nextSideShipRespawnIndex];
        if (currentHP <= threshold && AreAllSideShipsDestroyed())
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnEnemySideShips();
            }
            if (bossShield != null)
                bossShield.SetActive(true);
            nextSideShipRespawnIndex++;
        }
    }

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
}
