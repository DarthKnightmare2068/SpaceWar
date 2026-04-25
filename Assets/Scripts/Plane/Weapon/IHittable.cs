// Single interface for all hittable targets — lets DamageHelper do one GetComponentInParent call
// instead of five separate type-specific calls.
public interface IHittable
{
    void TakeDamage(float amount);
}
