using UnityEngine;
using TMPro;

public class PlayerHealthBar : DualSliderBar
{
    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    private PlaneStats playerStats;

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
        healthText.text = $"HP: {hp} / {maxHp}";
    }
    private void HandlePlayerChanged(GameObject player)
    {
        playerStats = player != null ? player.GetComponent<PlaneStats>() : null;
    }
}
