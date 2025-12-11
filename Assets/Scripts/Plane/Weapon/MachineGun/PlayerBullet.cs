using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    private PooledProjectile pooledProjectile;

    void Awake()
    {
        pooledProjectile = GetComponent<PooledProjectile>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Turret"))
        {
            HandleTurretCollision(collision);
        }
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            HandleEnemyCollision(collision);
        }
        
        ReturnToPoolOrDestroy();
    }

    private void HandleTurretCollision(Collision collision)
    {
        var turret = collision.gameObject.GetComponentInParent<TurretControl>();
        var smallCanon = collision.gameObject.GetComponentInParent<SmallCanonControl>();
        var bigCanon = collision.gameObject.GetComponentInParent<BigCanon>();
        
        if (turret != null)
        {
            turret.TakeDamage(1);
        }
        else if (smallCanon != null)
        {
            smallCanon.TakeDamage(1);
        }
        else if (bigCanon != null)
        {
            bigCanon.TakeDamage(1);
        }
    }

    private void HandleEnemyCollision(Collision collision)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentPlayer == null) return;
        
        var enemyStats = collision.gameObject.GetComponentInParent<EnemyStats>();
        var mainBossStats = collision.gameObject.GetComponentInParent<MainBossStats>();
        var player = GameManager.Instance.currentPlayer;
        var weaponManager = player.GetComponent<PlayerWeaponManager>();
        var gunControl = player.GetComponent<MachineGunControl>();
        var playerStats = player.GetComponent<PlaneStats>();
        
        if (enemyStats != null && weaponManager != null && gunControl != null && playerStats != null)
        {
            ApplyDamageToEnemy(enemyStats, gunControl, playerStats);
        }
        else if (mainBossStats != null && weaponManager != null && gunControl != null && playerStats != null)
        {
            ApplyDamageToMainBoss(mainBossStats, gunControl, playerStats);
        }
    }

    private void ApplyDamageToEnemy(EnemyStats enemyStats, MachineGunControl gunControl, PlaneStats playerStats)
    {
        float finalDamage = gunControl.damage + playerStats.attackPoint;
        enemyStats.TakeDamage((int)finalDamage);
    }

    private void ApplyDamageToMainBoss(MainBossStats mainBossStats, MachineGunControl gunControl, PlaneStats playerStats)
    {
        float finalDamage = gunControl.damage + playerStats.attackPoint;
        mainBossStats.TakeDamage((int)finalDamage);
    }

    private void ReturnToPoolOrDestroy()
    {
        if (pooledProjectile != null)
        {
            pooledProjectile.ReturnToPool();
        }
        else if (PlayerProjectilePool.Instance != null)
        {
            PlayerProjectilePool.Instance.ReturnBullet(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
