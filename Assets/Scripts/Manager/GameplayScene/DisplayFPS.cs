using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    [Header("FPS Display Settings")]
    public TextMeshProUGUI fpsText;
    [Tooltip("How often to update the FPS display (in seconds)")]
    public float updateInterval = 0.5f;

    private float timeSinceLastUpdate = 0f;

    void Update()
    {
        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if(timeSinceLastUpdate >= updateInterval)
        {
            UpdateFPSDisplay();
            timeSinceLastUpdate = 0f;
        }
    }

    void UpdateFPSDisplay()
    {
        if(fpsText != null && GameManager.Instance != null)
        {
            // Bolt: Optimized - replaced string concatenation with TextMeshPro's non-allocating
            // SetText overload to eliminate periodic heap allocations in the UI update path.
            fpsText.SetText("FPS: {0:0}", GameManager.Instance.GetCurrentFPS());
        }
    }
}