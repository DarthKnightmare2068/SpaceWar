using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MainBossStats : MonoBehaviour, IHasHealth, IHittable
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

    private float lastHPThreshold;
    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;
    private const float HP_THRESHOLD_STEP = 100000f;
    private readonly float[] sideShipRespawnThresholds = { 250000f, HP_THRESHOLD_STEP };
    private int nextSideShipRespawnIndex;
    private bool lastShieldActive = true;

    void Start()
    {
        currentHP = maxHP;
        lastHPThreshold = Mathf.Floor(maxHP / HP_THRESHOLD_STEP) * HP_THRESHOLD_STEP;
        UpdateShieldStatus();
        nextSideShipRespawnIndex = 0;
    }

    public void TrackSideShip(EnemyStats sideShip)
    {
        if (sideShip != null) sideShip.onDeath.AddListener(OnSideShipDied);
    }

    private void OnSideShipDied() => UpdateShieldStatus();

    void Update()
    {
        CheckWeaponRespawnByHP();
        CheckSideShipRespawnByHP();

        bool allInactive = weaponDmgControl == null || weaponDmgControl.AllWeaponsInactive;

        if (allInactive && forceRespawnTimer < 0f)
            forceRespawnTimer = FORCE_RESPAWN_DELAY;
        else if (!allInactive)
            forceRespawnTimer = -1f;

        if (forceRespawnTimer > 0f)
        {
            forceRespawnTimer -= Time.deltaTime;
            if (forceRespawnTimer <= 0f)
            {
                forceRespawnTimer = -1f;
                weaponDmgControl?.ReviveAllTurrets();
                weaponDmgControl?.ReviveAllCanons();
                weaponDmgControl?.ReviveAllBigCanons();
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0 || currentHP <= 0) return;
        if (!AreAllSideShipsDestroyed()) return;
        if (weaponDmgControl != null && !weaponDmgControl.AllWeaponsInactive) return;

        currentHP -= amount;
        if (currentHP <= 0) { currentHP = 0; HandleDeath(); }
        forceRespawnTimer = -1f;
    }

    private void HandleDeath()
    {
        if (deathVFX != null) { var vfx = Instantiate(deathVFX, transform.position, transform.rotation); Destroy(vfx, 5f); }
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    private bool AreAllSideShipsDestroyed()
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
        bool active = !AreAllSideShipsDestroyed();
        if (bossShield != null && active != lastShieldActive)
        {
            bossShield.SetActive(active);
            lastShieldActive = active;
        }
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
            if (bossShield != null) bossShield.SetActive(true);
            nextSideShipRespawnIndex++;
        }
    }

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
}
