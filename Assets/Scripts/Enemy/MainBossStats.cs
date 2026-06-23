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
    private List<EnemyStats> _sideShipStats = new List<EnemyStats>();

    void Start()
    {
        currentHP = maxHP;
        lastHPThreshold = Mathf.Floor(maxHP / HP_THRESHOLD_STEP) * HP_THRESHOLD_STEP;
        _allSideShipsDestroyed = ComputeAllSideShipsDestroyed();
        UpdateShieldStatus();
        nextSideShipRespawnIndex = 0;
    }

    public void TrackSideShip(EnemyStats sideShip)
    {
        if (sideShip != null)
        {
            sideShip.onDeath.AddListener(OnSideShipDied);
            // Bolt: Maintain local cache to avoid per-frame GetComponent lookups in ComputeAllSideShipsDestroyed
            _sideShipStats.RemoveAll(s => s == null);
            _sideShipStats.Add(sideShip);
            _allSideShipsDestroyed = ComputeAllSideShipsDestroyed();
        }
    }

    private void OnSideShipDied()
    {
        _allSideShipsDestroyed = ComputeAllSideShipsDestroyed();
        UpdateShieldStatus();
        // Bolt: Optimized - check for side ship respawn if the boss is already below threshold when ships die
        CheckSideShipRespawnByHP();
    }

    void Update()
    {
        // Bolt: Optimized - HP-based checks moved to event-driven OnDamageTaken and OnSideShipDied
        if (weaponDmgControl != null)
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
        // Bolt: Optimized - perform HP-based checks only when damage is actually taken
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
        // Bolt: Optimized - iterate over cached stats to avoid GetComponent and GameManager list access
        for (int i = 0; i < _sideShipStats.Count; i++)
        {
            var s = _sideShipStats[i];
            if (s != null && s.CurrentHP > 0)
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
            // Bolt: Clear cached references before respawning to keep list size stable
            _sideShipStats.Clear();
            GameManager.Instance?.RespawnEnemySideShips();
            SetShieldActive(true);
            nextSideShipRespawnIndex++;
        }
    }

    // ITargetable
    public Transform Transform => transform;
    public bool IsAlive => !IsDead;
}
