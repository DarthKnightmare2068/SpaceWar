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

    private float lastHP = -1f;
    private float lastMaxHP = -1f;
    private int lastDisplayedHP = -1;
    private int lastDisplayedMaxHP = -1;
    private bool lastWasDefeated = false;
    private string cachedTargetName;

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

        if (targetEnemy != null)
        {
            // Bolt: Optimized - cache name to avoid per-frame native-to-managed call
            cachedTargetName = targetEnemy.name;
            gameObject.SetActive(true);
            float healthPercent = (targetEnemy.MaxHP > 0) ? (targetEnemy.CurrentHP / targetEnemy.MaxHP) : 0;
            ForceSetBars(healthPercent);
        }
        else
        {
            cachedTargetName = string.Empty;
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (targetEnemy == null)
        {
            ForceSetBars(0f);
            if (nameText != null && !lastWasDefeated)
            {
                nameText.text = "Enemy Defeated";
                lastWasDefeated = true;
            }
            return;
        }

        // Bolt: Optimized - Fetch HP once and clamp to avoid redundant updates on negative health
        float currentHP = Mathf.Max(0, targetEnemy.CurrentHP);
        float maxHP = Mathf.Max(1, targetEnemy.MaxHP);

        bool healthChanged = !Mathf.Approximately(currentHP, lastHP) || !Mathf.Approximately(maxHP, lastMaxHP);
        bool easeAnimating = easeHealthBarSlider != null && normalHealthBarSlider != null &&
                             easeHealthBarSlider.value != normalHealthBarSlider.value;

        if (healthChanged || easeAnimating)
        {
            lastHP = currentHP;
            lastMaxHP = maxHP;

            float healthPercent = currentHP / maxHP;

            if (currentHP <= 0)
            {
                if (normalHealthBarSlider != null)
                    normalHealthBarSlider.value = 0f;
                if (easeHealthBarSlider != null)
                    easeHealthBarSlider.value = 0f;

                if (nameText != null && !lastWasDefeated)
                {
                    nameText.text = "Enemy Defeated";
                    lastWasDefeated = true;
                }
            }
            else
            {
                lastWasDefeated = false;
                UpdateBars(healthPercent);

                if (nameText != null)
                {
                    int hpInt = Mathf.CeilToInt(currentHP);
                    int maxHpInt = Mathf.CeilToInt(maxHP);

                    if (hpInt != lastDisplayedHP || maxHpInt != lastDisplayedMaxHP)
                    {
                        lastDisplayedHP = hpInt;
                        lastDisplayedMaxHP = maxHpInt;
                        // Bolt: Optimized - use cached target name and interpolation only on integer change
                        nameText.text = $"{cachedTargetName}: {hpInt} / {maxHpInt}";
                    }
                }
            }
        }
    }
} 
