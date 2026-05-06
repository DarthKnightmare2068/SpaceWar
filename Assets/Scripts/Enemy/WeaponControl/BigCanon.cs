using UnityEngine;

public class BigCanon : CanonBase
{
    [Header("Explosion VFX")]
    [SerializeField] private GameObject explosionVFXPrefab;

    protected override void Start()
    {
        laserDamageInterval = 0.5f;
        trackPlayerInstantly = true;
        base.Start();
    }

    protected override void InitializeStats()
    {
        if (cachedDmgControl != null)
        {
            damage = cachedDmgControl.GetBigCanonDamage();
            fireRate = cachedDmgControl.GetBigCanonFireRate();
            fireRange = cachedDmgControl.GetBigCanonFireRange();
        }
        else
        {
            damage = 100f;
            fireRate = 0.1f;
            fireRange = 200f;
        }
    }

    protected override void Die()
    {
        if (explosionVFXPrefab != null)
        {
            if (VFXPool.Instance != null)
                VFXPool.Instance.Get(explosionVFXPrefab, transform.position);
            else
            {
                var vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        }
        cachedDmgControl?.OnBigCanonDestroyed();
        gameObject.SetActive(false);
    }
}
