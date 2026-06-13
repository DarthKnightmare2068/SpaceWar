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
    private float lastRawExp = -1f;
    private float lastRawExpToNext = -1f;
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

            if (levelUpSystem == null)
            {
                if (expSlider != null && expSlider.value != 0)
                    expSlider.value = 0;
                if (levelText != null && lastLevel != -1)
                {
                    lastLevel = -1;
                    levelText.text = "Current Level: ---";
                }
                return;
            }
        }

        // Bolt: Optimized - centralized change detection and non-allocating text updates
        bool isMax = levelUpSystem.IsMaxLevel;
        int level = levelUpSystem.CurrentLevel;
        float currentExp = levelUpSystem.CurrentExp;
        float expToNext = levelUpSystem.ExpToNextLevel;

        bool rawChanged = isMax != lastWasMaxLevel ||
                          level != lastLevel ||
                          !Mathf.Approximately(currentExp, lastRawExp) ||
                          !Mathf.Approximately(expToNext, lastRawExpToNext);

        if (rawChanged)
        {
            if (expSlider != null)
                expSlider.value = isMax ? 1f : ((expToNext > 0) ? (currentExp / expToNext) : 0);

            int displayExp = Mathf.CeilToInt(currentExp);
            int displayExpToNext = Mathf.CeilToInt(expToNext);

            // Bolt: Throttled text updates to integer-only changes to avoid redundant mesh rebuilds.
            bool displayChanged = isMax != lastWasMaxLevel ||
                                 level != lastLevel ||
                                 displayExp != Mathf.CeilToInt(lastRawExp) ||
                                 displayExpToNext != Mathf.CeilToInt(lastRawExpToNext);

            if (displayChanged && levelText != null)
            {
                if (isMax)
                {
                    levelText.SetText("Current Level {0}: MAX", level);
                }
                else
                {
                    // Bolt: Optimized - use SetText with format specifiers to ensure integer display and avoid GC allocations.
                    levelText.SetText("Current Level {0:0}: {1:0} / {2:0}", (float)level, (float)displayExp, (float)displayExpToNext);
                }
            }

            lastWasMaxLevel = isMax;
            lastLevel = level;
            lastRawExp = currentExp;
            lastRawExpToNext = expToNext;
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
        levelUpSystem = FindAnyObjectByType<LevelUpSystem>();
    }
}
