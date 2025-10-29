using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HpScreen : MonoBehaviour
{
    private PlaneStats planeStats; // Auto-found at runtime
    private Volume volume;
    private UnityEngine.Rendering.Universal.Vignette vignette;
    private float lastHpPercent = 1f;
    private PlaneStats lastPlaneStats = null;

    void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
        }
    }

    void Update()
    {
        // Auto-find player plane if missing or destroyed
        if (planeStats == null || !planeStats.gameObject.activeInHierarchy)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                planeStats = playerObj.GetComponent<PlaneStats>();
        }

        // If player respawned (new PlaneStats instance), reset vignette intensity
        if (planeStats != lastPlaneStats && vignette != null)
        {
            vignette.intensity.value = 0f;
            lastHpPercent = 1f;
            lastPlaneStats = planeStats;
        }

        if (planeStats == null || vignette == null) return;

        float hpPercent = (float)planeStats.CurrentHP / Mathf.Max(planeStats.MaxHP, 1);

        // Smoothly interpolate intensity: 0 at 100% HP, 1 at 20% HP or less
        float intensity = 0f;
        if (hpPercent <= 0.2f)
            intensity = 1f;
        else if (hpPercent < 1f)
            intensity = (1f - hpPercent) / 0.8f;
        else
            intensity = 0f;

        vignette.intensity.value = Mathf.Clamp01(intensity);

        lastHpPercent = hpPercent;
    }
}
