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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            currentPlayer = player.GetComponent<PlaneControl>();
            if(currentPlayer != null)
            {
                return;
            }
        }

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
