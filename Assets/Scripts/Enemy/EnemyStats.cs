using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : MonoBehaviour, IHasHealth, IHittable
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
        if (amount <= 0 || currentHP <= 0) return;
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

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    void Update()
    {
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
}
