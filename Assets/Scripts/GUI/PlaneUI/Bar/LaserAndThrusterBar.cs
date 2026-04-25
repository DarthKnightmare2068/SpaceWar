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
        if (laserSystem != null && laserSlider != null)
        {
            float targetValue = laserSystem.maxThreshold > 0
                ? (float)laserSystem.currentThreshold / laserSystem.maxThreshold
                : 0f;
            laserSlider.value = Mathf.Lerp(laserSlider.value, targetValue, lerpSpeed);
        }
        else if (laserSlider != null)
        {
            laserSlider.value = 0f;
        }

        if (planeControl != null && thrusterSlider != null)
        {
            float targetValue = planeControl.maxThrusterThreshold > 0
                ? (float)planeControl.currentThrusterThreshold / planeControl.maxThrusterThreshold
                : 0f;
            thrusterSlider.value = Mathf.Lerp(thrusterSlider.value, targetValue, lerpSpeed);
        }
        else if (thrusterSlider != null)
        {
            thrusterSlider.value = 0f;
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
    }
}
