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
            fpsText.text = "FPS: " + GameManager.Instance.GetCurrentFPSString();
        }
    }
}