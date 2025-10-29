using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum HealthTargetType { Enemy, MainBoss, Custom }

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI Sliders")]
    [Tooltip("The main health bar (should be on the bottom in the hierarchy).")]
    public Slider normalHealthBarSlider;
    [Tooltip("The 'ease' bar that animates (should be on top in the hierarchy).")]
    public Slider easeHealthBarSlider;

    [Header("UI Text (Optional)")]
    public TextMeshProUGUI nameText;

    [Header("Animation Settings")]
    [Tooltip("Controls how quickly the ease bar animates. Smaller is slower.")]
    public float lerpSpeed = 0.05f;

    [Header("Health Target Selection")]
    public HealthTargetType healthTargetType = HealthTargetType.Enemy;
    public EnemyStats enemyTarget;
    public MainBossStats bossTarget;
    public MonoBehaviour customTarget; // Must implement IHasHealth

    private IHasHealth targetEnemy;

    void Start()
    {
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
        // When a new target is set, instantly set both bars to the correct health
        if (targetEnemy != null)
        {
            gameObject.SetActive(true); // Activate the health bar GameObject
            float healthPercent = (targetEnemy.MaxHP > 0) ? (targetEnemy.CurrentHP / targetEnemy.MaxHP) : 0;
            if(normalHealthBarSlider != null) normalHealthBarSlider.value = healthPercent;
            if(easeHealthBarSlider != null) easeHealthBarSlider.value = healthPercent;
        }
        else
        {
            gameObject.SetActive(false); // If target is null, hide the bar
        }
    }

    void Update()
    {
        if (targetEnemy != null)
        {
            float healthPercent = (targetEnemy.MaxHP > 0) ? (targetEnemy.CurrentHP / targetEnemy.MaxHP) : 0;
            // If the target is dead, force the bar to zero and show defeated text
            if (targetEnemy.CurrentHP <= 0)
            {
                if (normalHealthBarSlider != null)
                    normalHealthBarSlider.value = 0f;
                if (easeHealthBarSlider != null)
                    easeHealthBarSlider.value = 0f;
                if (nameText != null)
                    nameText.text = "Enemy Defeated";
            }
            else
            {
                normalHealthBarSlider.value = healthPercent;
                if (easeHealthBarSlider.value != normalHealthBarSlider.value)
                {
                    easeHealthBarSlider.value = Mathf.Lerp(easeHealthBarSlider.value, normalHealthBarSlider.value, lerpSpeed);
                }
                if (nameText != null)
                {
                    nameText.text = $"{targetEnemy.name}: {Mathf.CeilToInt(targetEnemy.CurrentHP)} / {Mathf.CeilToInt(targetEnemy.MaxHP)}";
                }
            }
        }
        else
        {
            // Force health bar to stay on and show defeated status
            if (normalHealthBarSlider != null)
                normalHealthBarSlider.value = 0f;
            if (easeHealthBarSlider != null)
                easeHealthBarSlider.value = 0f;
            if (nameText != null)
            {
                nameText.text = "Enemy Defeated";
            }
        }
        
        // Always force the health bar to stay active
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
} 