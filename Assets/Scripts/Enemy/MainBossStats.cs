using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MainBossStats : HealthBase, ITargetable
{
    [Header("Weapon Control Reference")]
    [Tooltip("Reference to the WeaponDmgControl managing this boss's weapons.")]
    public WeaponDmgControl weaponDmgControl;

    [Header("Boss Shield GameObject (disable to allow damage)")]
    public GameObject bossShield;

    private float lastHPThreshold;
    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;
    private const float HP_THRESHOLD_STEP = 100000f;
    private readonly float[] sideShipRespawnThresholds = { 250000f, HP_THRESHOLD_STEP };
    private int nextSideShipRespawnIndex;
    private bool lastShieldActive = true;

    // Cached so CheckSideShipRespawnByHP() reads a bool instead of iterating every frame.
    private bool _allSideShipsDestroyed = true;

    void Start()
    {
        currentHP = maxHP;
        lastHPThreshold = Mathf.Floor(maxHP / HP_THRESHOLD_STEP) * HP_THRESHOLD_STEP;
        _allSideShipsDestroyed = ComputeAllSideShipsDestroyed();
        UpdateShieldStatus();
        nextSideShipRespawnIndex = 0;

        CheckWeaponRespawnByHP();
        CheckSideShipRespawnByHP();
    }

    public void TrackSideShip(EnemyStats sideShip)
    {
        if (sideShip != null)
        {
            sideShip.onDeath.AddListener(OnSideShipDied);
            _allSideShipsDestroyed = ComputeAllSideShipsDestroyed();
        }
    }

    private void OnSideShipDied()
    {
        _allSideShipsDestroyed = ComputeAllSideShipsDestroyed();
        UpdateShieldStatus();
        CheckSideShipRespawnByHP();
    }

    void Update()
    {
        EnemyStats.TickForceRespawnTimer(weaponDmgControl, ref forceRespawnTimer, FORCE_RESPAWN_DELAY);
    }

    protected override bool CanTakeDamage()
    {
        if (!AreAllSideShipsDestroyed()) return false;
        if (weaponDmgControl != null && !weaponDmgControl.AllWeaponsInactive) return false;
        return true;
    }

    protected override void OnDamageTaken(float amount)
    {
        forceRespawnTimer = -1f;
        CheckWeaponRespawnByHP();
        CheckSideShipRespawnByHP();
    }

    protected override void OnDeath()
    {
        Destroy(gameObject);
    }

    private bool AreAllSideShipsDestroyed() => _allSideShipsDestroyed;

    private bool ComputeAllSideShipsDestroyed()
    {
        if (GameManager.Instance == null) return true;
        foreach (var ship in GameManager.Instance.GetActiveEnemyShips())
        {
            if (ship != null && ship.GetComponent<EnemyStats>() is EnemyStats s && s.CurrentHP > 0)
                return false;
        }
        return true;
    }

    private void UpdateShieldStatus()
    {
        SetShieldActive(!AreAllSideShipsDestroyed());
    }

    private void SetShieldActive(bool active)
    {
        if (bossShield != null && bossShield.activeSelf != active)
            bossShield.SetActive(active);

        lastShieldActive = active;
    }

    private void CheckWeaponRespawnByHP()
    {
        float threshold = Mathf.Max(Mathf.Floor(currentHP / HP_THRESHOLD_STEP) * HP_THRESHOLD_STEP, 0f);
        if (threshold < lastHPThreshold)
        {
            weaponDmgControl?.ReviveAllTurrets();
            weaponDmgControl?.ReviveAllCanons();
            weaponDmgControl?.ReviveAllBigCanons();
            lastHPThreshold = threshold;
        }
    }

    private void CheckSideShipRespawnByHP()
    {
        if (nextSideShipRespawnIndex >= sideShipRespawnThresholds.Length) return;
        if (currentHP <= sideShipRespawnThresholds[nextSideShipRespawnIndex] && AreAllSideShipsDestroyed())
        {
            GameManager.Instance?.RespawnEnemySideShips();
            SetShieldActive(true);
            nextSideShipRespawnIndex++;
        }
    }

    // ITargetable
    public Transform Transform => transform;
    public bool IsAlive => !IsDead;
}
