using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "SpaceWar/Weapon Config")]
public class WeaponConfigSO : ScriptableObject
{
    [Header("Bullet Turret")]
    public float bulletDamage = 20f;
    public float turretFireRate = 0.1f;
    public float turretFireRange = 100f;

    [Header("Laser Canon")]
    public float smallCanonDamage = 50f;
    public float smallCanonFireRate = 0.05f;
    public float smallCanonFireRange = 100f;

    [Header("Big Canon")]
    public float bigCanonDamage = 100f;
    public float bigCanonFireRate = 0.1f;
    public float bigCanonFireRange = 200f;
}
