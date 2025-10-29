using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
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

    [Header("Events")]
    // public UnityEvent onDeath; // Removed, handled by GameManager

    [Header("Damage Control")]
    [Tooltip("If false, the plane will not take damage.")]
    public bool canTakeDamage = true;

    void Awake()
    {
        currentHP = maxHP;
        lastDamageTime = Time.time;
        // UpdateHpScreens(); // Removed UI logic
    }

    void Start()
    {
        // Only assign if not already set (allows for manual override if needed)
        // if (hpScreen == null)
        //     hpScreen = GameObject.Find("HpScreen");
        // if (playerLostHpScreen == null)
        //     playerLostHpScreen = GameObject.Find("PlayerLostHpScreen");
        // if (playerHpLowScreen == null)
        //     playerHpLowScreen = GameObject.Find("PlayerHpLowScreen");

        // UpdateHpScreens(); // Removed UI logic
    }

    /// <summary>Enable or disable taking damage.</summary>
    public void SetCanTakeDamage(bool value)
    {
        canTakeDamage = value;
    }

    /// <summary>Inflict damage; fires onDeath if HP ≤ 0.</summary>
    public void TakeDamage(int amount)
    {
        if (!canTakeDamage) return;
        if(amount <= 0 || currentHP <= 0)
            return;
        currentHP -= amount;
        lastDamageTime = Time.time; // Update last damage time
        // UpdateHpScreens(); // Removed UI logic
        if(currentHP <= 0)
        {
            currentHP = 0;
            HandleDeath();
        }
    }

    /// <summary>Heal the plane up to maxHP.</summary>
    public void Heal(int amount)
    {
        if(amount <= 0 || currentHP >= maxHP)
            return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        Debug.Log($"{name} healed for {amount}, HP now {currentHP}/{maxHP}");
        // UpdateHpScreens(); // Removed UI logic
    }

    private void HandleDeath()
    {
        Debug.Log("Player is dead from PlaneStats");
        Debug.Log($"{name} is destroyed!");
        // onDeath?.Invoke(); // Removed, handled by GameManager

        // Delegate all death handling to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDeath(this);
    }

#if UNITY_EDITOR
    void Update()
    {
        // Test key: O for lost HP
        if (UnityEngine.Input.GetKeyDown(KeyCode.O))
        {
            TakeDamage(maxHP); // Instantly die for testing
        }
        // Test key: P to take 10% damage
        if (UnityEngine.Input.GetKeyDown(KeyCode.P))
        {
            // Debug: Take 10% of max HP as damage
            int damageToTake = Mathf.RoundToInt(maxHP * 0.1f);
            TakeDamage(damageToTake);
            Debug.Log($"[PlaneStats Debug] 'P' key pressed. Taking {damageToTake} damage.");
        }
        // Test key: L to log collision setup
        if (UnityEngine.Input.GetKeyDown(KeyCode.L))
        {
            DebugCollisionSetup();
        }

        // Check if enough time has passed since last damage
        if (Time.time - lastDamageTime >= regenerationDelay && currentHP < maxHP)
        {
            // Calculate regeneration amount (20% of max HP per second)
            float regenerationAmount = maxHP * regenerationRate * Time.deltaTime;
            Heal(Mathf.RoundToInt(regenerationAmount));
        }
    }
#else
    void Update()
    {
        // Check if enough time has passed since last damage
        if (Time.time - lastDamageTime >= regenerationDelay && currentHP < maxHP)
        {
            // Calculate regeneration amount (20% of max HP per second)
            float regenerationAmount = maxHP * regenerationRate * Time.deltaTime;
            Heal(Mathf.RoundToInt(regenerationAmount));
        }
    }
#endif

    private void UpdateHpScreens()
    {
        // Removed UI logic
    }

    // Getters for current stats
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int AttackPoint => attackPoint;
    public bool IsDead()
    {
        return currentHP <= 0 || !gameObject.activeInHierarchy;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[PlaneStats] OnCollisionEnter with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Turret"))
        {
            Debug.Log($"[PlaneStats] Collision with {collision.gameObject.tag} detected! Taking {maxHP} damage.");
            TakeDamage(maxHP); // Instantly die
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PlaneStats] OnTriggerEnter with: {other.gameObject.name}, Tag: {other.gameObject.tag}");
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Turret"))
        {
            Debug.Log($"[PlaneStats] Trigger with {other.gameObject.tag} detected! Taking {maxHP} damage.");
            TakeDamage(maxHP); // Instantly die
        }
    }

    // Additional collision detection methods for better coverage
    void OnCollisionStay(Collision collision)
    {
        Debug.Log($"[PlaneStats] OnCollisionStay with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Turret"))
        {
            Debug.Log($"[PlaneStats] CollisionStay with {collision.gameObject.tag} detected! Taking {maxHP} damage.");
            TakeDamage(maxHP); // Instantly die
        }
    }

    void OnTriggerStay(Collider other)
    {
        Debug.Log($"[PlaneStats] OnTriggerStay with: {other.gameObject.name}, Tag: {other.gameObject.tag}");
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Turret"))
        {
            Debug.Log($"[PlaneStats] TriggerStay with {other.gameObject.tag} detected! Taking {maxHP} damage.");
            TakeDamage(maxHP); // Instantly die
        }
    }

    // Debug method to check collision setup
    private void DebugCollisionSetup()
    {
        Debug.Log($"[PlaneStats] === Collision Setup Debug ===");
        Debug.Log($"[PlaneStats] GameObject: {gameObject.name}");
        Debug.Log($"[PlaneStats] Tag: {gameObject.tag}");
        Debug.Log($"[PlaneStats] Layer: {gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})");
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"[PlaneStats] Collider: {col.GetType().Name}");
            Debug.Log($"[PlaneStats] IsTrigger: {col.isTrigger}");
            Debug.Log($"[PlaneStats] Enabled: {col.enabled}");
        }
        else
        {
            Debug.LogWarning($"[PlaneStats] No Collider found on {gameObject.name}!");
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"[PlaneStats] Rigidbody: {rb.GetType().Name}");
            Debug.Log($"[PlaneStats] IsKinematic: {rb.isKinematic}");
            Debug.Log($"[PlaneStats] UseGravity: {rb.useGravity}");
        }
        else
        {
            Debug.LogWarning($"[PlaneStats] No Rigidbody found on {gameObject.name}!");
        }
        
        Debug.Log($"[PlaneStats] Current HP: {currentHP}/{maxHP}");
        Debug.Log($"[PlaneStats] CanTakeDamage: {canTakeDamage}");
        Debug.Log($"[PlaneStats] IsDead: {IsDead()}");
        Debug.Log($"[PlaneStats] === End Debug ===");
    }
}
