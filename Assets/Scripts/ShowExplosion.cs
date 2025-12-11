using UnityEngine;

public class ShowExplosion : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private float vFXDuration = 2f;
    private bool hasExploded = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        PlayExplosionVFX();
        hasExploded = true;
        Destroy(gameObject);
    }

    private void PlayExplosionVFX()
    {
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vFXDuration);
        }
    }
}
