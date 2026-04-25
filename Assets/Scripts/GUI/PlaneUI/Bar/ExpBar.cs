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

    private int lastLevel = -1;
    private int lastExp = -1;
    private int lastExpToNext = -1;
    private bool lastWasMaxLevel = false;

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
            bool isMax = levelUpSystem.IsMaxLevel;
            int level = levelUpSystem.CurrentLevel;
            int exp = Mathf.CeilToInt(levelUpSystem.CurrentExp);
            int expToNext = Mathf.CeilToInt(levelUpSystem.ExpToNextLevel);

            if (expSlider != null)
                expSlider.value = isMax ? 1f : ((expToNext > 0) ? (levelUpSystem.CurrentExp / levelUpSystem.ExpToNextLevel) : 0);

            if (levelText != null)
            {
                if (isMax != lastWasMaxLevel || level != lastLevel || exp != lastExp || expToNext != lastExpToNext)
                {
                    lastWasMaxLevel = isMax;
                    lastLevel = level;
                    lastExp = exp;
                    lastExpToNext = expToNext;
                    levelText.text = isMax
                        ? $"Current Level {level}: MAX"
                        : $"Current Level {level}: {exp} / {expToNext}";
                }
            }
        }
        else
        {
            if (expSlider != null)
                expSlider.value = 0;
            if (levelText != null && lastLevel != -1)
            {
                lastLevel = -1;
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
