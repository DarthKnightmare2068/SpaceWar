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
        lastWasDefeated = false;

        // Bolt: Optimized - cache name once to avoid per-frame native property calls
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
        if (targetEnemy != null)
        {
            float currentHP = targetEnemy.CurrentHP;
            float maxHP = targetEnemy.MaxHP;

            // Bolt: Optimized - guard logic with change detection and animation state checks
            bool healthChanged = !Mathf.Approximately(currentHP, lastHP) || !Mathf.Approximately(maxHP, lastMaxHP);
            bool easeAnimating = easeHealthBarSlider != null && normalHealthBarSlider != null &&
                                 !Mathf.Approximately(easeHealthBarSlider.value, normalHealthBarSlider.value);

            if (currentHP <= 0)
            {
                if (!lastWasDefeated)
                {
                    ForceSetBars(0f);
                    if (nameText != null) nameText.SetText("Enemy Defeated");
                    lastWasDefeated = true;
                }
            }
            else
            {
                lastWasDefeated = false;
                if (healthChanged || easeAnimating)
                {
                    float healthPercent = (maxHP > 0) ? (currentHP / maxHP) : 0;
                    UpdateBars(healthPercent);

                    if (nameText != null && healthChanged)
                    {
                        // Bolt: Optimized - use cached name and interpolation only on health changes.
                        // TMP_Text.SetText format overloads only support numeric types to avoid boxing;
                        // we use interpolation here but it's guarded by healthChanged to minimize GC.
                        nameText.text = $"{cachedTargetName}: {currentHP:F0} / {maxHP:F0}";
                    }
                }
            }

            lastHP = currentHP;
            lastMaxHP = maxHP;
        }
        else
        {
            if (!lastWasDefeated)
            {
                ForceSetBars(0f);
                if (nameText != null) nameText.SetText("Enemy Defeated");
                lastWasDefeated = true;
            }
        }
    }
} 
