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

    void Start()
    {
        // Try to find the player immediately
        FindPlayer();
    }

    void Update()
    {
        // Try to find player if not found yet
        if(currentPlayer == null)
        {
            FindPlayer();
        }

        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if(timeSinceLastUpdate >= updateInterval)
        {
            UpdateSpeedDisplay();
            timeSinceLastUpdate = 0f;
        }
    }

    void FindPlayer()
    {
        // Try to find player by tag first
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            currentPlayer = player.GetComponent<PlaneControl>();
            if(currentPlayer != null)
            {
                Debug.Log("SpeedDisplay: Found player with PlaneControl component");
                return;
            }
        }

        // If not found by tag, try to find by component
        PlaneControl [] planeControls = FindObjectsOfType<PlaneControl>();
        if(planeControls.Length > 0)
        {
            currentPlayer = planeControls [0];
            Debug.Log("SpeedDisplay: Found player by PlaneControl component");
        }
    }

    void UpdateSpeedDisplay()
    {
        if(speedText != null && currentPlayer != null)
        {
            speedText.text = $"Speed: {currentPlayer.currentSpeed:F0}";
        }
        else if(speedText != null)
        {
            speedText.text = "Speed: --";
        }
    }
}