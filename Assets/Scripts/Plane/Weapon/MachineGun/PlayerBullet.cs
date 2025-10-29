using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    // Static variables to keep last log states
    private static string lastHitObjectName = null;
    private static bool lastInFireRange = false;
    private static float lastDamageDealt = -1f;

    void OnCollisionEnter(Collision collision)
    {
        string hitName = collision.gameObject.name;
        if (hitName != lastHitObjectName)
        {
            Debug.Log($"[PlayerBullet] Bullet hit: {hitName}");
            lastHitObjectName = hitName;
        }

        // Prioritize Turret first
        if (collision.gameObject.CompareTag("Turret"))
        {
            // Check for all weapon types since all weapons use "Turret" tag
            var turret = collision.gameObject.GetComponentInParent<TurretControl>();
            var smallCanon = collision.gameObject.GetComponentInParent<SmallCanonControl>();
            var bigCanon = collision.gameObject.GetComponentInParent<BigCanon>();
            
            if (turret != null)
            {
                turret.TakeDamage(1); // Or your bullet damage value
                Debug.Log($"[PlayerBullet] Damaged TurretControl: {turret.name}");
            }
            else if (smallCanon != null)
            {
                smallCanon.TakeDamage(1); // Or your bullet damage value
                Debug.Log($"[PlayerBullet] Damaged SmallCanonControl: {smallCanon.name}");
            }
            else if (bigCanon != null)
            {
                bigCanon.TakeDamage(1); // Or your bullet damage value
                Debug.Log($"[PlayerBullet] Damaged BigCanon: {bigCanon.name}");
            }
            else
            {
                Debug.LogWarning($"[PlayerBullet] Hit object with 'Turret' tag but no weapon component found: {collision.gameObject.name}");
            }
        }
        // Then Enemy
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            var enemyStats = collision.gameObject.GetComponentInParent<EnemyStats>();
            var mainBossStats = collision.gameObject.GetComponentInParent<MainBossStats>();
            var player = GameManager.Instance.currentPlayer;
            var weaponManager = player.GetComponent<PlayerWeaponManager>();
            var gunControl = player.GetComponent<MachineGunControl>();
            var playerStats = player.GetComponent<PlaneStats>();
            
            if (enemyStats != null && weaponManager != null && gunControl != null && playerStats != null)
            {
                // Check fire range
                float distance = Vector3.Distance(player.transform.position, collision.transform.position);
                bool inRange = distance <= weaponManager.machineGunFireRange;
                if (inRange != lastInFireRange)
                {
                    Debug.Log($"[PlayerBullet] Machine gun in fire range: {inRange}");
                    lastInFireRange = inRange;
                }
                Debug.Log($"[PlayerBullet] Bullet hit enemy: {enemyStats.name}, Distance: {distance:F2}, In fire range: {inRange}");
                float finalDamage = gunControl.damage + playerStats.attackPoint;
                if (finalDamage != lastDamageDealt)
                {
                    Debug.Log($"[PlayerBullet] Machine gun caused {finalDamage} damage to {enemyStats.name}");
                    lastDamageDealt = finalDamage;
                }
                enemyStats.TakeDamage((int)finalDamage);
            }
            else if (mainBossStats != null && weaponManager != null && gunControl != null && playerStats != null)
            {
                // Check fire range
                float distance = Vector3.Distance(player.transform.position, collision.transform.position);
                bool inRange = distance <= weaponManager.machineGunFireRange;
                if (inRange != lastInFireRange)
                {
                    Debug.Log($"[PlayerBullet] Machine gun in fire range: {inRange}");
                    lastInFireRange = inRange;
                }
                Debug.Log($"[PlayerBullet] Bullet hit main boss: {mainBossStats.name}, Distance: {distance:F2}, In fire range: {inRange}");
                float finalDamage = gunControl.damage + playerStats.attackPoint;
                if (finalDamage != lastDamageDealt)
                {
                    Debug.Log($"[PlayerBullet] Machine gun caused {finalDamage} damage to {mainBossStats.name}");
                    lastDamageDealt = finalDamage;
                }
                mainBossStats.TakeDamage((int)finalDamage);
            }
        }
        Destroy(gameObject);
    }
} 