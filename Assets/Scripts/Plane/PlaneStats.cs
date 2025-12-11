using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class PlaneStats : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum hit points of the plane.")]
    public int maxHP = 100;
    [SerializeField, Tooltip("Current HP at runtime.")]
    private int currentHP;

    [Header("Health Regeneration")]
    [Tooltip("Time in seconds without taking damage before regeneration starts")]
    [SerializeField] private float regenerationDelay = 3f;
    [Tooltip("Percentage of max HP regenerated per second")]
    [SerializeField] private float regenerationRate = 0.2f;
    private float lastDamageTime;

    [Header("Attack Settings")]
    [Tooltip("Base damage dealt by the plane's attack.")]
    public int attackPoint = 10;

    [Header("Damage Control")]
    [Tooltip("If false, the plane will not take damage.")]
    public bool canTakeDamage = true;

    void Awake()
    {
        currentHP = maxHP;
        lastDamageTime = Time.time;
    }

    public void SetCanTakeDamage(bool value)
    {
        canTakeDamage = value;
    }

    public void TakeDamage(int amount)
    {
        if (!canTakeDamage) return;
        if(amount <= 0 || currentHP <= 0)
            return;
        currentHP -= amount;
        lastDamageTime = Time.time;
        if(currentHP <= 0)
        {
            currentHP = 0;
            HandleDeath();
        }
    }

    public void Heal(int amount)
    {
        if(amount <= 0 || currentHP >= maxHP)
            return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    private void HandleDeath()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDeath(this);
    }

    void Update()
    {
        if (Time.time - lastDamageTime >= regenerationDelay && currentHP < maxHP)
        {
            float regenerationAmount = maxHP * regenerationRate * Time.deltaTime;
            Heal(Mathf.RoundToInt(regenerationAmount));
        }
    }

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int AttackPoint => attackPoint;
    
    public bool IsDead()
    {
        return currentHP <= 0 || !gameObject.activeInHierarchy;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Turret"))
        {
            TakeDamage(maxHP);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Turret"))
        {
            TakeDamage(maxHP);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Turret"))
        {
            TakeDamage(maxHP);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Turret"))
        {
            TakeDamage(maxHP);
        }
    }
}
