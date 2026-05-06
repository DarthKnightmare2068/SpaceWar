using UnityEngine;
using UnityEngine.Events;

// Shared base for float-HP entities (EnemyStats, MainBossStats). PlaneStats keeps its int-typed
// HP for backwards compatibility with existing prefab/inspector data — see A-16 note.
public abstract class HealthBase : MonoBehaviour, IHasHealth, IHittable
{
    [Header("Health Settings")]
    [Tooltip("Maximum hit points.")]
    public float maxHP = 1000f;
    [SerializeField, Tooltip("Current HP at runtime.")]
    protected float currentHP;

    [Header("Events")]
    public UnityEvent onDeath;

    [Header("Death VFX")]
    [Tooltip("Prefab to spawn when destroyed.")]
    public GameObject deathVFX;
    [Tooltip("Lifetime of the spawned death VFX before it is destroyed.")]
    public float deathVFXLifetime = 5f;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => currentHP <= 0 || !gameObject.activeInHierarchy;

    protected virtual void Awake()
    {
        currentHP = maxHP;
    }

    public virtual void TakeDamage(float amount)
    {
        if (amount <= 0 || IsDead || !CanTakeDamage()) return;
        currentHP -= amount;
        OnDamageTaken(amount);
        if (currentHP <= 0)
        {
            currentHP = 0;
            HandleDeath();
        }
    }

    protected virtual bool CanTakeDamage() => true;
    protected virtual void OnDamageTaken(float amount) { }

    protected virtual void HandleDeath()
    {
        if (deathVFX != null)
        {
            if (VFXPool.Instance != null)
                VFXPool.Instance.Get(deathVFX, transform.position, transform.rotation, deathVFXLifetime);
            else
            {
                var vfx = Instantiate(deathVFX, transform.position, transform.rotation);
                Destroy(vfx, deathVFXLifetime);
            }
        }
        onDeath?.Invoke();
        OnDeath();
    }

    protected abstract void OnDeath();
}
