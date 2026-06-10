using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HpScreen : MonoBehaviour
{
    private PlaneStats planeStats;
    private Volume volume;
    private Vignette vignette;
    private PlaneStats lastPlaneStats;
    private float lastIntensity = -1f;

    void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume != null && volume.profile != null)
            volume.profile.TryGet(out vignette);
    }

    void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        TryBindPlayer();
    }

    void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    void Update()
    {
        if (planeStats == null || vignette == null)
            return;

        float hpPercent = (float)planeStats.CurrentHP / Mathf.Max(planeStats.MaxHP, 1);
        float intensity;

        if (hpPercent <= 0.2f)
            intensity = 1f;
        else if (hpPercent < 1f)
            intensity = (1f - hpPercent) / 0.8f;
        else
            intensity = 0f;

        // Bolt: Optimized - implemented change-detection guard to avoid redundant native-to-managed
        // Post-Processing property writes every frame when health is stable.
        intensity = Mathf.Clamp01(intensity);
        if (Mathf.Abs(intensity - lastIntensity) > 1e-4f)
        {
            vignette.intensity.value = intensity;
            lastIntensity = intensity;
        }
    }

    private void TryBindPlayer()
    {
        if (GameEntityRegistry.TryGetPlayerComponent(out PlaneStats newPlaneStats))
            SetPlaneStats(newPlaneStats);
        else
            SetPlaneStats(null);
    }

    private void SetPlaneStats(PlaneStats newPlaneStats)
    {
        planeStats = newPlaneStats;

        if (planeStats != lastPlaneStats && vignette != null)
        {
            vignette.intensity.value = 0f;
            lastIntensity = 0f;
            lastPlaneStats = planeStats;
        }
    }

    private void HandlePlayerChanged(GameObject player)
    {
        SetPlaneStats(player != null ? player.GetComponent<PlaneStats>() : null);
    }
}
