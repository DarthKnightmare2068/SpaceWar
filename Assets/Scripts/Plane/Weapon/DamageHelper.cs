using UnityEngine;

// Centralizes the "find the right component and deal damage" pattern used by all player weapons.
// Uses IHittable to do a single GetComponentInParent walk instead of five separate type checks.
public static class DamageHelper
{
    public static bool TryDealDamage(RaycastHit hit, float damage, Color popupColor)
        => TryDealDamage(hit.collider, damage, popupColor, hit.point);

    public static bool TryDealDamage(Collider col, float damage, Color popupColor, Vector3 hitPoint)
    {
        var hittable = col.GetComponentInParent<IHittable>();
        if (hittable == null) return false;
        hittable.TakeDamage(damage);
        DmgPopUp.ShowDamage(hitPoint, (int)damage, popupColor);

        // Bolt: Optimized - centralized experience and score gain. By moving this from LevelUpSystem's
        // per-frame polling to an event-driven call here, we eliminate all damage-tracking Update overhead.
        // We filter for "Enemy" and "Turret" tags to ensure XP/Score is only granted for valid targets.
        if (col.CompareTag("Enemy") || col.CompareTag("Turret"))
        {
            if (LevelUpSystem.Instance != null)
                LevelUpSystem.Instance.AddDamageExperience(damage);

            if (ScoreCounting.Instance != null)
                ScoreCounting.Instance.RecordDamageDealt(damage);
        }

        return true;
    }

    // Apply damage without popup (used by physical bullet collision).
    public static bool TryDealDamageSilent(Collider col, float damage)
    {
        var hittable = col.GetComponentInParent<IHittable>();
        if (hittable == null) return false;
        hittable.TakeDamage(damage);

        // Bolt: Optimized - centralized experience and score gain.
        if (col.CompareTag("Enemy") || col.CompareTag("Turret"))
        {
            if (LevelUpSystem.Instance != null)
                LevelUpSystem.Instance.AddDamageExperience(damage);

            if (ScoreCounting.Instance != null)
                ScoreCounting.Instance.RecordDamageDealt(damage);
        }

        return true;
    }
}
