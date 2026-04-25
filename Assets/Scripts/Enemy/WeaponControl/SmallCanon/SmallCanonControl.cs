using UnityEngine;

public class SmallCanonControl : CanonBase
{
    private SmallCanonManager cachedManager;

    protected override void Start()
    {
        base.Start();
        cachedManager = GetComponentInParent<SmallCanonManager>();
        if (cachedManager == null)
            cachedManager = FindObjectOfType<SmallCanonManager>();
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
            var vfx = Instantiate(cachedManager.canonDestroyedVFX, transform.position, Quaternion.identity);
            float duration = 2f;
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null) duration = ps.main.duration;
            Destroy(vfx, duration);
        }
        cachedDmgControl?.OnCanonDestroyed();
        gameObject.SetActive(false);
    }
}
