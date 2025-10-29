using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider expSlider;
    public TextMeshProUGUI levelText;

    // This will hold the reference to the player's level system
    private LevelUpSystem levelUpSystem;

    void Start()
    {
        // Find components on this GameObject if not assigned in the Inspector
        if (expSlider == null)
        {
            expSlider = GetComponent<Slider>();
        }
        if (levelText == null)
        {
            levelText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Attempt to find the LevelUpSystem at the start
        FindLevelUpSystem();
    }

    void Update()
    {
        // Continuously check for the LevelUpSystem in case it's not available at Start.
        if (levelUpSystem == null)
        {
            FindLevelUpSystem();
        }

        // If we have a valid reference, update the UI.
        if (levelUpSystem != null)
        {
            // Update the experience slider's value directly.
            if (expSlider != null)
            {
                if (levelUpSystem.IsMaxLevel)
                {
                    expSlider.value = 1f;
                }
                else
                {
                    expSlider.value = (levelUpSystem.ExpToNextLevel > 0) ? (levelUpSystem.CurrentExp / levelUpSystem.ExpToNextLevel) : 0;
                }
            }

            // Update the level text.
            if (levelText != null)
            {
                if (levelUpSystem.IsMaxLevel)
                {
                    levelText.text = $"Current Level {levelUpSystem.CurrentLevel}: MAX";
                }
                else
                {
                    levelText.text = $"Current Level {levelUpSystem.CurrentLevel}: {Mathf.CeilToInt(levelUpSystem.CurrentExp)} / {Mathf.CeilToInt(levelUpSystem.ExpToNextLevel)}";
                }
            }
        }
        else // If no LevelUpSystem is found, set the bar to empty.
        {
            if (expSlider != null)
            {
                expSlider.value = 0;
            }
            if (levelText != null)
            {
                levelText.text = "Current Level: ---";
            }
        }
    }

    void FindLevelUpSystem()
    {
        // The LevelUpSystem component is in the scene, so we can find it.
        levelUpSystem = FindObjectOfType<LevelUpSystem>();
        if (levelUpSystem != null)
        {
            Debug.Log("[ExpBar] Successfully connected to LevelUpSystem.");
        }
    }
}
