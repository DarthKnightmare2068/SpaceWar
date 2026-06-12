using UnityEngine;
using TMPro;

public class PlayerHealthBar : DualSliderBar
{
    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    private PlaneStats playerStats;

    private float lastHP = -1f;
    private float lastMaxHP = -1f;
    private int lastDisplayedHP = -1;
    private int lastDisplayedMaxHP = -1;

    void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        TryBindPlayer();
    }

    void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    void Update()
    {
        if (playerStats != null)
        {
            // Bolt: Optimized - Fetch HP once and clamp to avoid redundant updates on negative health
            float currentHP = Mathf.Max(0, playerStats.CurrentHP);
            float maxHP = Mathf.Max(1, (float)playerStats.MaxHP);

            // Bolt: Optimized - Guard UI and text updates with change detection and animation state
            bool healthChanged = !Mathf.Approximately(currentHP, lastHP) || !Mathf.Approximately(maxHP, lastMaxHP);
            bool easeAnimating = easeHealthBarSlider != null && normalHealthBarSlider != null &&
                                 easeHealthBarSlider.value != normalHealthBarSlider.value;

            if (healthChanged || easeAnimating)
            {
                lastHP = currentHP;
                lastMaxHP = maxHP;
                float percent = currentHP / maxHP;
                UpdateBars(percent);
                UpdateHealthText(currentHP, maxHP);
            }
        }
        else if (lastHP != -1f)
        {
            lastHP = -1f;
            lastMaxHP = -1f;
            ForceSetBars(0f);
            UpdateHealthText(0, 0);
        }
    }

    private void TryBindPlayer()
    {
        if (!GameEntityRegistry.TryGetPlayerComponent(out playerStats))
            playerStats = null;
    }

    void UpdateHealthText(float current, float max)
    {
        if (healthText == null) return;
        int hp = Mathf.CeilToInt(current);
        int maxHp = Mathf.CeilToInt(max);
        if (hp == lastDisplayedHP && maxHp == lastDisplayedMaxHP) return;
        lastDisplayedHP = hp;
        lastDisplayedMaxHP = maxHp;
        // Bolt: Optimized - use SetText to avoid per-update heap allocations
        healthText.SetText("HP: {0} / {1}", hp, maxHp);
    }
    private void HandlePlayerChanged(GameObject player)
    {
        playerStats = player != null ? player.GetComponent<PlaneStats>() : null;
        lastHP = -1f;
        lastMaxHP = -1f;
        lastDisplayedHP = -1;
        lastDisplayedMaxHP = -1;
    }
}
