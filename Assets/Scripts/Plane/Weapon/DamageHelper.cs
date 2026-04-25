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
        return true;
    }

    // Apply damage without popup (used by physical bullet collision).
    public static bool TryDealDamageSilent(Collider col, float damage)
    {
        var hittable = col.GetComponentInParent<IHittable>();
        if (hittable == null) return false;
        hittable.TakeDamage(damage);
        return true;
    }
}
