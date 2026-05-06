using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    private PooledProjectile pooledProjectile;
    
    // Cached references to avoid GetComponent on every collision
    private static MachineGunControl cachedGunControl;
    private static PlaneStats cachedPlayerStats;
    private static GameObject cachedPlayer;

    void Awake()
    {
        pooledProjectile = GetComponent<PooledProjectile>();
    }
    
    void OnEnable()
    {
        // Refresh cache if player changed or cache is stale
        RefreshPlayerCache();
    }
    
    private static void RefreshPlayerCache()
    {
        if (!GameEntityRegistry.TryGetPlayerObject(out GameObject currentPlayer))
            return;

        if (cachedPlayer != currentPlayer)
        {
            cachedPlayer = currentPlayer;
            cachedGunControl = currentPlayer.GetComponent<MachineGunControl>();
            cachedPlayerStats = currentPlayer.GetComponent<PlaneStats>();
        }
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
        DamageHelper.TryDealDamageSilent(collision.collider, 1f);
    }

    private void HandleEnemyCollision(Collision collision)
    {
        if (cachedPlayer == null || cachedGunControl == null || cachedPlayerStats == null)
        {
            RefreshPlayerCache();
            if (cachedGunControl == null || cachedPlayerStats == null) return;
        }

        float finalDamage = cachedGunControl.damage + cachedPlayerStats.attackPoint;
        DamageHelper.TryDealDamageSilent(collision.collider, finalDamage);
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
