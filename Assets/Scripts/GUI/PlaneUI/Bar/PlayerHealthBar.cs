using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider normalHealthBarSlider;
    public Slider easeHealthBarSlider;

    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    [Header("Animation Settings")]
    private float lerpSpeed = 0.05f;

    private PlaneStats playerStats;
    
    private float playerSearchTimer = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 0.5f;

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
            normalHealthBarSlider.value = (playerStats.MaxHP > 0) ? (playerStats.CurrentHP / (float)playerStats.MaxHP) : 0;

            if (easeHealthBarSlider.value != normalHealthBarSlider.value)
            {
                easeHealthBarSlider.value = Mathf.Lerp(easeHealthBarSlider.value, normalHealthBarSlider.value, lerpSpeed);
            }

            UpdateHealthText(playerStats.CurrentHP, playerStats.MaxHP);
        }
        else
        {
            normalHealthBarSlider.value = 0;
            easeHealthBarSlider.value = 0;
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
        
        // Fallback to GameManager (already tried, but for safety)
        GameObject playerObj = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlaneStats>();
        }
    }

    void UpdateHealthText(float current, float max)
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
    
    public void OnPlayerSpawned(GameObject player)
    {
        if (player != null)
        {
            playerStats = player.GetComponent<PlaneStats>();
        }
    }
}
