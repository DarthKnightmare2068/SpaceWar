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
            speedText.text = $"Speed: {currentPlayer.currentSpeed:F0}";
        else if (speedText != null)
            speedText.text = "Speed: --";
    }

    private void HandlePlayerChanged(GameObject player)
    {
        currentPlayer = player != null ? player.GetComponent<PlaneControl>() : null;
    }
}
