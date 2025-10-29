using UnityEngine;
using System.Collections;

public class ShowExplosion : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private float vFXDuration = 2f;
    private bool hasExploded = false;
    private float spawnTime;

    private void Start()
    {
        spawnTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return; // Prevent double VFX

        PlayExplosionVFX();
        hasExploded = true;
        Destroy(gameObject); // Or return to pool if using pooling
    }

    private void PlayExplosionVFX()
    {
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vFXDuration);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator LogVFXDestroyed(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"[ShowExplosion] VFX destroyed at {vfx.transform.position}");
    }
}
