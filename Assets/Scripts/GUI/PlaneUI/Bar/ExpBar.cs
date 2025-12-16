using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider expSlider;
    public TextMeshProUGUI levelText;

    private LevelUpSystem levelUpSystem;
    
    private float searchTimer = 0f;
    private const float SEARCH_INTERVAL = 1f;

    void Start()
    {
        if (expSlider == null)
        {
            expSlider = GetComponent<Slider>();
        }
        if (levelText == null)
        {
            levelText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Delay initialization to avoid startup lag
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait a few frames for scene to initialize
        yield return new WaitForSeconds(0.1f);
        
        // Try cached GameManager reference first
        if (GameManager.Instance != null && GameManager.Instance.levelUpSystem != null)
        {
            levelUpSystem = GameManager.Instance.levelUpSystem;
            yield break;
        }
        
        // Fallback only if needed
        FindLevelUpSystem();
    }

    void Update()
    {
        if (levelUpSystem == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= SEARCH_INTERVAL)
            {
                searchTimer = 0f;
                FindLevelUpSystem();
            }
        }

        if (levelUpSystem != null)
        {
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
        else
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
        // Always try cached GameManager reference first (no search)
        if (GameManager.Instance != null && GameManager.Instance.levelUpSystem != null)
        {
            levelUpSystem = GameManager.Instance.levelUpSystem;
            return;
        }
        
        // Only use expensive FindObjectOfType as last resort
        levelUpSystem = FindObjectOfType<LevelUpSystem>();
    }
}
