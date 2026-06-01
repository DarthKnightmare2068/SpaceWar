using System.Collections;
using UnityEngine;
using TMPro;

public enum HealthTargetType { Enemy, MainBoss, Custom }

public class EnemyHealthBar : DualSliderBar
{
    [Header("UI Text (Optional)")]
    public TextMeshProUGUI nameText;

    [Header("Health Target Selection")]
    public HealthTargetType healthTargetType = HealthTargetType.Enemy;
    public EnemyStats enemyTarget;
    public MainBossStats bossTarget;
    public MonoBehaviour customTarget; // Must implement IHasHealth

    private IHasHealth targetEnemy;

    private int lastDisplayedHP = -1;
    private int lastDisplayedMaxHP = -1;
    private float lastHP = -1f;
    private float lastMaxHP = -1f;
    private bool lastWasDefeated = false;
    private string cachedTargetName = string.Empty;

    void Start()
    {
        // Delay initialization to avoid startup lag - wait for enemies to spawn
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait for enemies to spawn (they spawn after boss)
        yield return new WaitForSeconds(0.2f);
        AssignTargetFromSelection();
    }

    void OnValidate()
    {
        AssignTargetFromSelection();
    }

    private void AssignTargetFromSelection()
    {
        switch (healthTargetType)
        {
            case HealthTargetType.Enemy:
                if (enemyTarget != null)
                    SetTarget((IHasHealth)enemyTarget);
                break;
            case HealthTargetType.MainBoss:
                if (bossTarget != null)
                    SetTarget((IHasHealth)bossTarget);
                break;
            case HealthTargetType.Custom:
                if (customTarget != null && customTarget is IHasHealth)
                    SetTarget((IHasHealth)customTarget);
                break;
        }
    }

    // The GameManager or other scripts can still call this to override
    public void SetTarget(IHasHealth enemy)
    {
        targetEnemy = enemy;
        lastHP = -1f;
        lastMaxHP = -1f;
        lastDisplayedHP = -1;
        lastDisplayedMaxHP = -1;
        lastWasDefeated = false;
        // Bolt: Optimized - cache target name to avoid per-frame native calls and allocations
        cachedTargetName = (targetEnemy != null) ? targetEnemy.name : string.Empty;

        if (targetEnemy != null)
        {
            gameObject.SetActive(true);
            float healthPercent = (targetEnemy.MaxHP > 0) ? (targetEnemy.CurrentHP / targetEnemy.MaxHP) : 0;
            ForceSetBars(healthPercent);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (targetEnemy == null)
        {
            if (!lastWasDefeated)
            {
                ForceSetBars(0f);
                if (nameText != null)
                    nameText.text = "Enemy Defeated";
                lastWasDefeated = true;
            }
            return;
        }

        float currentHP = targetEnemy.CurrentHP;
        float maxHP = targetEnemy.MaxHP;

        if (currentHP <= 0)
        {
            if (!lastWasDefeated)
            {
                ForceSetBars(0f);
                if (nameText != null)
                    nameText.text = "Enemy Defeated";
                lastWasDefeated = true;
            }
            return;
        }

        lastWasDefeated = false;

        int hpInt = Mathf.CeilToInt(currentHP);
        int maxHpInt = Mathf.CeilToInt(maxHP);

        bool textChanged = (hpInt != lastDisplayedHP || maxHpInt != lastDisplayedMaxHP);
        bool healthChanged = !Mathf.Approximately(currentHP, lastHP) || !Mathf.Approximately(maxHP, lastMaxHP);

        // Bolt: Optimized - check if ease bar is still animating to avoid redundant UpdateBars calls
        bool easeAnimating = easeHealthBarSlider != null && normalHealthBarSlider != null &&
                             !Mathf.Approximately(easeHealthBarSlider.value, normalHealthBarSlider.value);

        if (healthChanged || easeAnimating)
        {
            lastHP = currentHP;
            lastMaxHP = maxHP;

            float healthPercent = (maxHP > 0) ? (currentHP / maxHP) : 0;
            UpdateBars(healthPercent);

            if (nameText != null && textChanged)
            {
                lastDisplayedHP = hpInt;
                lastDisplayedMaxHP = maxHpInt;
                // Bolt: Optimized - use cachedTargetName to avoid per-frame native calls/allocations
                nameText.text = $"{cachedTargetName}: {hpInt} / {maxHpInt}";
            }
        }
    }
} 
