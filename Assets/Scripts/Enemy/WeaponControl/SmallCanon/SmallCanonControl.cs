using UnityEngine;

public class SmallCanonControl : CanonBase
{
    private SmallCanonManager cachedManager;

    protected override void Start()
    {
        base.Start();
        cachedManager = GetComponentInParent<SmallCanonManager>();
        if (cachedManager == null)
            cachedManager = FindAnyObjectByType<SmallCanonManager>();
    }

    protected override void InitializeStats()
    {
        if (cachedDmgControl != null)
        {
            damage = cachedDmgControl.GetSmallCanonDamage();
            fireRate = cachedDmgControl.GetSmallCanonFireRate();
            fireRange = cachedDmgControl.GetSmallCanonFireRange();
        }
        else
        {
            damage = 10f;
            fireRate = 2f;
            fireRange = 100f;
        }
    }

    protected override void Die()
    {
        if (cachedManager != null && cachedManager.canonDestroyedVFX != null)
        {
            if (VFXPool.Instance != null)
                VFXPool.Instance.Get(cachedManager.canonDestroyedVFX, transform.position);
            else
            {
                var vfx = Instantiate(cachedManager.canonDestroyedVFX, transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        }
        cachedDmgControl?.OnCanonDestroyed();
        gameObject.SetActive(false);
    }
}
