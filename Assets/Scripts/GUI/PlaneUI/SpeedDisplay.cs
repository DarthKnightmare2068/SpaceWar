using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [Header("Speed Display Settings")]
    public TextMeshProUGUI speedText;
    [Tooltip("How often to update the speed display (in seconds)")]
    public float updateInterval = 0.1f;

    private float timeSinceLastUpdate = 0f;
    private PlaneControl currentPlayer;

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
        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            UpdateSpeedDisplay();
            timeSinceLastUpdate = 0f;
        }
    }

    private void TryBindPlayer()
    {
        if (!GameEntityRegistry.TryGetPlayerComponent(out currentPlayer))
            currentPlayer = null;
    }

    private void UpdateSpeedDisplay()
    {
        if (speedText != null && currentPlayer != null)
        {
            // Bolt: Optimized - replaced string interpolation with TextMeshPro's non-allocating
            // SetText overload to eliminate periodic heap allocations in the UI update path.
            speedText.SetText("Speed: {0:0}", currentPlayer.currentSpeed);
        }
        else if (speedText != null)
        {
            speedText.text = "Speed: --";
        }
    }

    private void HandlePlayerChanged(GameObject player)
    {
        currentPlayer = player != null ? player.GetComponent<PlaneControl>() : null;
    }
}
