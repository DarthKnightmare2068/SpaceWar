using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider normalHealthBarSlider; // The main health bar (should be on the bottom in hierarchy)
    public Slider easeHealthBarSlider;   // The "ease" bar (should be on top in hierarchy)

    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    [Header("Animation Settings")]
    private float lerpSpeed = 0.05f;

    // This will hold the reference to the player's stats
    private PlaneStats playerStats;

    void Start()
    {
        // Attempt to find the player at the start
        FindPlayer();
    }

    void Update()
    {
        // Continuously check for the player in case they don't exist at Start, or they respawn.
        if (playerStats == null || !playerStats.gameObject.activeInHierarchy)
        {
            FindPlayer();
        }

        // If we have a valid player reference, update the UI.
        if (playerStats != null)
        {
            // 1. Instantly update the Normal Health Bar's slider value by calculating the percentage directly.
            normalHealthBarSlider.value = (playerStats.MaxHP > 0) ? (playerStats.CurrentHP / (float)playerStats.MaxHP) : 0;

            // 2. Animate the Ease Health Bar to catch up to the normal slider's new value.
            if (easeHealthBarSlider.value != normalHealthBarSlider.value)
            {
                easeHealthBarSlider.value = Mathf.Lerp(easeHealthBarSlider.value, normalHealthBarSlider.value, lerpSpeed);
            }

            // 3. Update the health text.
            UpdateHealthText(playerStats.CurrentHP, playerStats.MaxHP);
        }
        else // If no player is found, set the health bar to empty.
        {
            normalHealthBarSlider.value = 0;
            easeHealthBarSlider.value = 0;
            UpdateHealthText(0, 0);
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlaneStats>();
            if (playerStats != null)
            {
                Debug.Log("[PlayerHealthBar] Successfully connected to player's PlaneStats.");
            }
        }
    }

    void UpdateHealthText(float current, float max)
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
} 