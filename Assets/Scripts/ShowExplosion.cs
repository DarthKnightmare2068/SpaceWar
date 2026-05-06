using UnityEngine;

public class ShowExplosion : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private float vFXDuration = 2f;
    private bool hasExploded = false;

    private void OnEnable()
    {
        hasExploded = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        PlayExplosionVFX();
        hasExploded = true;
        ReturnOrDestroy();
    }

    private void PlayExplosionVFX()
    {
        if (explosionVFX == null) return;
        if (VFXPool.Instance != null)
            VFXPool.Instance.Get(explosionVFX, transform.position);
        else
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vFXDuration);
        }
    }

    private void ReturnOrDestroy()
    {
        var pooled = GetComponent<PooledProjectile>();
        if (pooled != null) { pooled.ReturnToPool(); return; }
        if (PlayerProjectilePool.Instance != null) { PlayerProjectilePool.Instance.ReturnBullet(gameObject); return; }
        Destroy(gameObject);
    }
}
