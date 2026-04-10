using System.Collections;
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
    private float searchCooldown = 0f;
    private const float SEARCH_INTERVAL = 0.5f;

    void Start()
    {
        // Delay initialization to avoid startup lag
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait a few frames for scene to initialize
        yield return new WaitForSeconds(0.1f);
        FindPlayerComponents();
    }

    void Update()
    {
        // Continuously check for the player and its components in case they respawn.
        // Use cooldown to avoid checking every frame
        if (laserSystem == null || planeControl == null || (laserSystem != null && !laserSystem.gameObject.activeInHierarchy))
        {
            searchCooldown -= Time.deltaTime;
            if (searchCooldown <= 0f)
            {
                searchCooldown = SEARCH_INTERVAL;
                FindPlayerComponents();
            }
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
        GameObject playerObj = null;
        
        // Try cached GameManager reference first (no search)
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerObj = GameManager.Instance.currentPlayer;
            laserSystem = playerObj.GetComponent<LaserActive>();
            planeControl = playerObj.GetComponent<PlaneControl>();
            if (laserSystem != null && planeControl != null)
            {
                return;
            }
        }
        
        // Fallback to GameManager if needed
        if (playerObj == null)
        {
            playerObj = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
        }
        
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