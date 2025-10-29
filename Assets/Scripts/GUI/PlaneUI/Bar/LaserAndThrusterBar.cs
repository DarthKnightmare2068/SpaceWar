using UnityEngine;
using UnityEngine.UI;

public class LaserAndThrusterBar : MonoBehaviour
{
    [Header("UI Sliders")]
    [Tooltip("Drag the UI Slider for the Laser here.")]
    public Slider laserSlider;
    [Tooltip("Drag the UI Slider for the Thruster here.")]
    public Slider thrusterSlider;

    [Header("Slidebar smooth Settings")]
    [Tooltip("Controls how quickly the bars animate. Smaller is slower.")]
    public float lerpSpeed = 0.05f;

    // References to player components
    private LaserActive laserSystem;
    private PlaneControl planeControl;

    void Update()
    {
        // Continuously check for the player and its components in case they respawn.
        if (laserSystem == null || planeControl == null || !laserSystem.gameObject.activeInHierarchy)
        {
            FindPlayerComponents();
        }

        // --- Update Laser Bar ---
        if (laserSystem != null && laserSlider != null)
        {
            float targetValue = (laserSystem.maxThreshold > 0) ? ((float)laserSystem.currentThreshold / laserSystem.maxThreshold) : 0;
            laserSlider.value = Mathf.Lerp(laserSlider.value, targetValue, lerpSpeed);
        }
        else if (laserSlider != null)
        {
            laserSlider.value = 0;
        }

        // --- Update Thruster Bar ---
        if (planeControl != null && thrusterSlider != null)
        {
            float targetValue = (planeControl.maxThrusterThreshold > 0) ? ((float)planeControl.currentThrusterThreshold / planeControl.maxThrusterThreshold) : 0;
            thrusterSlider.value = Mathf.Lerp(thrusterSlider.value, targetValue, lerpSpeed);
        }
        else if (thrusterSlider != null)
        {
            thrusterSlider.value = 0;
        }
    }

    void FindPlayerComponents()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            laserSystem = playerObj.GetComponent<LaserActive>();
            planeControl = playerObj.GetComponent<PlaneControl>();
        }
        else
        {
            laserSystem = null;
            planeControl = null;
        }
    }
} 