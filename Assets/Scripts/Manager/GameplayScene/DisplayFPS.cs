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
            // Bolt: Optimized - use SetText with float to avoid string allocation from concatenation and float-to-string conversion
            fpsText.SetText("FPS: {0:0}", GameManager.Instance.GetCurrentFPS());
        }
    }
}