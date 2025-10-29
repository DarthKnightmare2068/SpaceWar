using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : MonoBehaviour, IHasHealth
{
    [Header("Health Settings")]
    [Tooltip("Maximum hit points of the enemy.")]
    public float maxHP = 1000f;
    [SerializeField, Tooltip("Current HP at runtime.")]
    private float currentHP;

    [Header("Events")]
    public UnityEvent onDeath;

    [Header("Death VFX")]
    [Tooltip("Prefab to spawn when the enemy is destroyed.")]
    public GameObject deathVFX;

    [Header("Weapon Control Reference")]
    [Tooltip("Reference to the WeaponDmgControl managing this enemy's weapons.")]
    public WeaponDmgControl weaponDmgControl;

    private float lastLoggedHP = -1f;
    private bool hasLoggedInitialHP = false;

    // --- Force respawn timer ---
    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;

    // Start is called before the first frame update
    void Start()
    {
        currentHP = maxHP;
    }

    /// <summary>Inflict damage; fires onDeath if HP ≤ 0.</summary>
    public void TakeDamage(float amount)
    {
        if(amount <= 0 || currentHP <= 0)
            return;
        // Only allow damage if all weapons are inactive
        if (weaponDmgControl != null)
        {
            bool allWeaponsInactive = true;
            // Check small canons
            if (weaponDmgControl.smallCanonManager != null)
            {
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null && canon.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Check turrets
            if (weaponDmgControl.turretsManager != null)
            {
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null && turret.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Check big canons
            BigCanon[] bigCanons = GetComponentsInChildren<BigCanon>(true);
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null && bigCanon.gameObject.activeInHierarchy)
                    allWeaponsInactive = false;
            }
            if (!allWeaponsInactive)
            {
                Debug.Log($"{name} cannot take damage until all weapons are inactive!");
                return;
            }
        }
        currentHP -= amount;
        if(currentHP <= 0)
        {
            currentHP = 0;
            HandleDeath();
        }
        // Reset force respawn timer on damage
        forceRespawnTimer = -1f;
    }

    private void HandleDeath()
    {
        Debug.Log($"{name} is destroyed!");
        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, transform.rotation);
            Destroy(vfx, 5f); // Clean up VFX after 5 seconds
        }
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>Read-only accessors for UI or other scripts.</summary>
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    // Update is called once per frame
    void Update()
    {
        // Log HP for debug
        if (!hasLoggedInitialHP || Mathf.Abs(currentHP - lastLoggedHP) > Mathf.Epsilon)
        {
            Debug.Log($"{name} HP: {currentHP}");
            lastLoggedHP = currentHP;
            hasLoggedInitialHP = true;
        }

        // Check if all weapons are inactive
        bool allWeaponsInactive = true;
        if (weaponDmgControl != null)
        {
            // Small canons
            if (weaponDmgControl.smallCanonManager != null)
            {
                foreach (var canon in weaponDmgControl.smallCanonManager.canons)
                {
                    if (canon != null && canon.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Turrets
            if (weaponDmgControl.turretsManager != null)
            {
                foreach (var turret in weaponDmgControl.turretsManager.turrets)
                {
                    if (turret != null && turret.gameObject.activeInHierarchy)
                        allWeaponsInactive = false;
                }
            }
            // Big canons
            BigCanon[] bigCanons = GetComponentsInChildren<BigCanon>(true);
            foreach (var bigCanon in bigCanons)
            {
                if (bigCanon != null && bigCanon.gameObject.activeInHierarchy)
                    allWeaponsInactive = false;
            }
        }

        // Start or update the force respawn timer
        if (allWeaponsInactive && forceRespawnTimer < 0f)
        {
            forceRespawnTimer = FORCE_RESPAWN_DELAY;
            Debug.Log($"[EnemyStats] All weapons inactive. Starting force respawn timer: {FORCE_RESPAWN_DELAY}s");
        }
        else if (!allWeaponsInactive)
        {
            forceRespawnTimer = -1f; // Reset timer if any weapon is revived
        }

        // Countdown and trigger force respawn if needed
        if (forceRespawnTimer > 0f)
        {
            forceRespawnTimer -= Time.deltaTime;
            if (forceRespawnTimer <= 0f)
            {
                forceRespawnTimer = -1f;
                Debug.Log("[EnemyStats] Force respawning all weapons!");
                if (weaponDmgControl != null)
                {
                    weaponDmgControl.ReviveAllTurrets();
                    weaponDmgControl.ReviveAllCanons();
                    weaponDmgControl.ReviveAllBigCanons();
                }
            }
        }
    }
}
