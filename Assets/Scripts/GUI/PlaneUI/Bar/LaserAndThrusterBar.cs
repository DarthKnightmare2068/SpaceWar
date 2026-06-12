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

    private LaserActive laserSystem;
    private PlaneControl planeControl;

    // Bolt: Optimized state tracking to avoid redundant per-frame calculations and property writes
    private int lastLaserThreshold = -1;
    private int lastMaxLaserThreshold = -1;
    private float laserTargetValue = 0f;
    private bool laserInitialized = false;

    private int lastThrusterThreshold = -1;
    private int lastMaxThrusterThreshold = -1;
    private float thrusterTargetValue = 0f;
    private bool thrusterInitialized = false;

    private const float EPSILON = 1e-4f;

    void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        TryBindPlayerComponents();
    }

    void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    void Update()
    {
        // Laser Slider Optimization: Only calculate target on change and snap lerp to stop redundant writes.
        if (laserSystem != null && laserSlider != null)
        {
            int curThresh = laserSystem.currentThreshold;
            int maxThresh = laserSystem.maxThreshold;

            if (curThresh != lastLaserThreshold || maxThresh != lastMaxLaserThreshold)
            {
                lastLaserThreshold = curThresh;
                lastMaxLaserThreshold = maxThresh;
                laserTargetValue = maxThresh > 0 ? (float)curThresh / maxThresh : 0f;
            }

            if (Mathf.Abs(laserSlider.value - laserTargetValue) > EPSILON)
            {
                laserSlider.value = Mathf.Lerp(laserSlider.value, laserTargetValue, lerpSpeed);
            }
            else if (laserSlider.value != laserTargetValue)
            {
                laserSlider.value = laserTargetValue;
            }
            laserInitialized = true;
        }
        else if (laserSlider != null && laserInitialized)
        {
            laserSlider.value = 0f;
            laserInitialized = false;
            lastLaserThreshold = -1;
            lastMaxLaserThreshold = -1;
        }

        // Thruster Slider Optimization: Only calculate target on change and snap lerp to stop redundant writes.
        if (planeControl != null && thrusterSlider != null)
        {
            int curThresh = planeControl.currentThrusterThreshold;
            int maxThresh = planeControl.maxThrusterThreshold;

            if (curThresh != lastThrusterThreshold || maxThresh != lastMaxThrusterThreshold)
            {
                lastThrusterThreshold = curThresh;
                lastMaxThrusterThreshold = maxThresh;
                thrusterTargetValue = maxThresh > 0 ? (float)curThresh / maxThresh : 0f;
            }

            if (Mathf.Abs(thrusterSlider.value - thrusterTargetValue) > EPSILON)
            {
                thrusterSlider.value = Mathf.Lerp(thrusterSlider.value, thrusterTargetValue, lerpSpeed);
            }
            else if (thrusterSlider.value != thrusterTargetValue)
            {
                thrusterSlider.value = thrusterTargetValue;
            }
            thrusterInitialized = true;
        }
        else if (thrusterSlider != null && thrusterInitialized)
        {
            thrusterSlider.value = 0f;
            thrusterInitialized = false;
            lastThrusterThreshold = -1;
            lastMaxThrusterThreshold = -1;
        }
    }

    private void TryBindPlayerComponents()
    {
        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            HandlePlayerChanged(player);
        else
            HandlePlayerChanged(null);
    }

    private void HandlePlayerChanged(GameObject player)
    {
        laserSystem = player != null ? player.GetComponent<LaserActive>() : null;
        planeControl = player != null ? player.GetComponent<PlaneControl>() : null;

        // Force a zero-out if player was lost
        if (player == null)
        {
            laserInitialized = true;
            thrusterInitialized = true;
        }
    }
}
