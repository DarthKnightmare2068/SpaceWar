using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerHealthBar : DualSliderBar
{
    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    private PlaneStats playerStats;

    private float playerSearchTimer = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 0.5f;

    private int lastDisplayedHP = -1;
    private int lastDisplayedMaxHP = -1;

    void Start()
    {
        // Delay initialization to avoid startup lag - wait for player to spawn
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait a few frames for scene to initialize
        yield return new WaitForSeconds(0.1f);
        
        // Try to find player using cached GameManager reference first
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerStats = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
            if (playerStats != null)
            {
                yield break;
            }
        }
        
        // Fallback to tag search only if GameManager doesn't have player yet
        FindPlayer();
    }

    void Update()
    {
        if (playerStats == null || !playerStats.gameObject.activeInHierarchy)
        {
            playerSearchTimer += Time.deltaTime;
            if (playerSearchTimer >= PLAYER_SEARCH_INTERVAL)
            {
                playerSearchTimer = 0f;
                FindPlayer();
            }
        }

        if (playerStats != null)
        {
            float percent = (playerStats.MaxHP > 0) ? (playerStats.CurrentHP / (float)playerStats.MaxHP) : 0;
            UpdateBars(percent);
            UpdateHealthText(playerStats.CurrentHP, playerStats.MaxHP);
        }
        else
        {
            ForceSetBars(0f);
            UpdateHealthText(0, 0);
        }
    }

    void FindPlayer()
    {
        // Always try GameManager first (cached reference, no search)
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerStats = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
            if (playerStats != null)
            {
                return;
            }
        }
        
        // Only use expensive tag search as last resort
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlaneStats>();
        }
    }

    void UpdateHealthText(float current, float max)
    {
        if (healthText == null) return;
        int hp = Mathf.CeilToInt(current);
        int maxHp = Mathf.CeilToInt(max);
        if (hp == lastDisplayedHP && maxHp == lastDisplayedMaxHP) return;
        lastDisplayedHP = hp;
        lastDisplayedMaxHP = maxHp;
        healthText.text = $"HP: {hp} / {maxHp}";
    }
    
    public void OnPlayerSpawned(GameObject player)
    {
        if (player != null)
        {
            playerStats = player.GetComponent<PlaneStats>();
        }
    }
}
