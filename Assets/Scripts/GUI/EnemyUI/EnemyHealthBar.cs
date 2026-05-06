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
    private bool lastWasDefeated = false;

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
        lastDisplayedHP = -1;
        lastDisplayedMaxHP = -1;
        lastWasDefeated = false;
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
            float healthPercent = (targetEnemy.MaxHP > 0) ? (targetEnemy.CurrentHP / targetEnemy.MaxHP) : 0;
            if (targetEnemy.CurrentHP <= 0)
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
                    int hp = Mathf.CeilToInt(targetEnemy.CurrentHP);
                    int maxHp = Mathf.CeilToInt(targetEnemy.MaxHP);
                    if (hp != lastDisplayedHP || maxHp != lastDisplayedMaxHP)
                    {
                        lastDisplayedHP = hp;
                        lastDisplayedMaxHP = maxHp;
                        nameText.text = $"{targetEnemy.name}: {hp} / {maxHp}";
                    }
                }
            }
        }
        else
        {
            ForceSetBars(0f);
            if (nameText != null && !lastWasDefeated)
            {
                nameText.text = "Enemy Defeated";
                lastWasDefeated = true;
            }
        }
    }
} 
