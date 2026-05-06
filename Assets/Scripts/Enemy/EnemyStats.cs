using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : HealthBase, ITargetable
{
    [Header("Weapon Control Reference")]
    [Tooltip("Reference to the WeaponDmgControl managing this enemy's weapons.")]
    public WeaponDmgControl weaponDmgControl;

    private float forceRespawnTimer = -1f;
    private const float FORCE_RESPAWN_DELAY = 10f;

    void Start()
    {
        currentHP = maxHP;
    }

    protected override bool CanTakeDamage()
    {
        // Damage gated until all this enemy's weapons are inactive — prevents nuking
        // a ship while its turrets/canons can still fight back.
        return weaponDmgControl == null || weaponDmgControl.AllWeaponsInactive;
    }

    protected override void OnDamageTaken(float amount)
    {
        forceRespawnTimer = -1f;
    }

    protected override void OnDeath()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        TickForceRespawnTimer(weaponDmgControl, ref forceRespawnTimer, FORCE_RESPAWN_DELAY);
    }

    // Shared between EnemyStats and MainBossStats — kept as a static helper rather than baked into
    // HealthBase because PlaneStats (player) has no equivalent revive concept.
    internal static void TickForceRespawnTimer(WeaponDmgControl dmg, ref float timer, float delay)
    {
        bool allInactive = dmg == null || dmg.AllWeaponsInactive;

        if (allInactive && timer < 0f)
            timer = delay;
        else if (!allInactive)
            timer = -1f;

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = -1f;
                dmg?.ReviveAllTurrets();
                dmg?.ReviveAllCanons();
                dmg?.ReviveAllBigCanons();
            }
        }
    }

    // ITargetable
    public Transform Transform => transform;
    public bool IsAlive => !IsDead;
}
