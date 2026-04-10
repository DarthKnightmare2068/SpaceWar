using System.Collections;
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
        // Delay initialization to avoid startup lag
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait a few frames for scene to initialize
        yield return new WaitForSeconds(0.1f);
        FindPlayer();
    }

    void Update()
    {
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
        // Try cached GameManager reference first (no search)
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            currentPlayer = GameManager.Instance.currentPlayer.GetComponent<PlaneControl>();
            if (currentPlayer != null)
            {
                return;
            }
        }
        
        // Fallback to GameManager (already tried, but for safety in case it was null before)
        GameObject player = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
        if(player != null)
        {
            currentPlayer = player.GetComponent<PlaneControl>();
            if(currentPlayer != null)
            {
                return;
            }
        }

        // Last resort: FindObjectsOfType (expensive, avoid if possible)
        PlaneControl[] planeControls = FindObjectsOfType<PlaneControl>();
        if(planeControls.Length > 0)
        {
            currentPlayer = planeControls[0];
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
