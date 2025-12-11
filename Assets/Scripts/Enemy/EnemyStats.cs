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

    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float amount)
    {
        if(amount <= 0 || currentHP <= 0)
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
            {
                return;
            }
        }
        currentHP -= amount;
        if(currentHP <= 0)
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

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    void Update()
    {
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
}
