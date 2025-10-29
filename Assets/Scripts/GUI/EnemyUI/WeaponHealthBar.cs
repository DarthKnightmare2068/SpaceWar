using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponHealthBar : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider normalHealthBarSlider;
    public Slider easeHealthBarSlider;

    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    [Header("Animation Settings")]
    public float lerpSpeed = 0.05f;

    [Header("Fade Settings")]
    public float hideDelay = 3f;
    public float fadeDuration = 0.5f;
    public bool isFaded = false; // If true, enable fade logic. If false, bar is always visible.

    private int maxHP;
    private int currentHP;

    private Camera activeCamera;
    private Transform playerTransform;

    private float lastDamageTime = -100f;
    private CanvasGroup canvasGroup;

    // Reference to TurretControl or SmallCanonControl if this bar is for a weapon
    private TurretControl turretControl;
    private SmallCanonControl cannonControl;
    private BigCanon bigCanonControl;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f; // Start visible for testing
    }

    void Start()
    {
        FindPlayerAndCamera();
        // Try to find TurretControl or SmallCanonControl in parent or self
        turretControl = GetComponentInParent<TurretControl>() ?? GetComponent<TurretControl>();
        cannonControl = GetComponentInParent<SmallCanonControl>() ?? GetComponent<SmallCanonControl>();
        bigCanonControl = GetComponentInParent<BigCanon>() ?? GetComponent<BigCanon>();
        
        // Debug logging to check connections
        Debug.Log($"[WeaponHealthBar] {gameObject.name}: Checking weapon connections...");
        if (turretControl != null)
        {
            maxHP = turretControl.maxHP;
            Debug.Log($"[WeaponHealthBar] {gameObject.name}: ✅ Connected to TurretControl - MaxHP: {maxHP}");
        }
        else if (cannonControl != null)
        {
            maxHP = cannonControl.maxHP;
            Debug.Log($"[WeaponHealthBar] {gameObject.name}: ✅ Connected to SmallCanonControl - MaxHP: {maxHP}");
        }
        else if (bigCanonControl != null)
        {
            maxHP = bigCanonControl.maxHP;
            Debug.Log($"[WeaponHealthBar] {gameObject.name}: ✅ Connected to BigCanon - MaxHP: {maxHP}");
        }
        else
        {
            Debug.LogWarning($"[WeaponHealthBar] {gameObject.name}: ❌ No weapon control found! Check hierarchy.");
        }
        
        Debug.Log($"[WeaponHealthBar] {gameObject.name}: isFaded = {isFaded}, CanvasGroup alpha = {canvasGroup.alpha}");
        
        // If connected to a weapon and has health, make sure the bar is visible
        if ((turretControl != null || cannonControl != null || bigCanonControl != null) && maxHP > 0)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                Debug.Log($"[WeaponHealthBar] {gameObject.name}: Making health bar visible for weapon with {maxHP} max HP");
            }
        }
    }

    void Update()
    {
        // If connected to a turret or cannon, update HP from it
        if (turretControl != null)
        {
            currentHP = turretControl.currentHP;
            maxHP = turretControl.maxHP;
        }
        else if (cannonControl != null)
        {
            currentHP = cannonControl.currentHP;
            maxHP = cannonControl.maxHP;
        }
        else if (bigCanonControl != null)
        {
            currentHP = bigCanonControl.currentHP;
            maxHP = bigCanonControl.maxHP;
        }

        // Safety checks
        if (maxHP <= 0) maxHP = 1;
        if (currentHP < 0) currentHP = 0;

        float percent = (maxHP > 0) ? (float)currentHP / maxHP : 0;
        if (normalHealthBarSlider != null)
            normalHealthBarSlider.value = percent;

        if (easeHealthBarSlider != null && normalHealthBarSlider != null && easeHealthBarSlider.value != normalHealthBarSlider.value)
        {
            easeHealthBarSlider.value = Mathf.Lerp(easeHealthBarSlider.value, normalHealthBarSlider.value, lerpSpeed);
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHP} / {maxHP}";
        }

        // Check if player or camera has changed (e.g., respawned)
        if (playerTransform == null || activeCamera == null)
        {
            FindPlayerAndCamera();
        }
        else
        {
            // Check if the active camera has changed (front/back switch)
            Camera newActiveCam = GetActivePlayerCamera(playerTransform);
            if (newActiveCam != activeCamera)
                activeCamera = newActiveCam;
        }

        // Handle fade in/out only if isFaded is true
        if (isFaded)
        {
            float timeSinceDamage = Time.time - lastDamageTime;
            if (timeSinceDamage < hideDelay)
            {
                // Fade in
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime / fadeDuration);
                }
            }
            else
            {
                // Fade out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime / fadeDuration);
                }
            }
        }
        else
        {
            // Always visible
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
    }

    void LateUpdate()
    {
        if (activeCamera != null)
        {
            // Billboard to camera
            transform.LookAt(transform.position + activeCamera.transform.rotation * Vector3.forward,
                             activeCamera.transform.rotation * Vector3.up);
        }
    }

    void FindPlayerAndCamera()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            activeCamera = GetActivePlayerCamera(playerTransform);
        }
    }

    Camera GetActivePlayerCamera(Transform player)
    {
        // Find all Camera components in player children
        Camera[] cams = player.GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in cams)
        {
            if (cam.enabled)
                return cam;
        }
        // Fallback: return first camera if none are enabled
        return cams.Length > 0 ? cams[0] : Camera.main;
    }

    // Call this to update health from your weapon logic (still available for non-turret/cannon use)
    public void SetHealth(int current, int max)
    {
        // Only reset timer if value changes
        if (current != currentHP)
        {
            lastDamageTime = Time.time;
            Debug.Log($"[WeaponHealthBar] {gameObject.name}: Health updated - {current}/{max}, Timer reset");
            
            // Force the health bar to be visible when damage is taken
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                Debug.Log($"[WeaponHealthBar] {gameObject.name}: Forcing health bar visibility after damage");
            }
        }
        currentHP = current;
        maxHP = max;
    }

    // Debug method to force show the health bar
    [ContextMenu("Force Show Health Bar")]
    public void ForceShowHealthBar()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            Debug.Log($"[WeaponHealthBar] {gameObject.name}: Force showing health bar");
        }
    }

    // Debug method to check current status
    [ContextMenu("Debug Health Bar Status")]
    public void DebugHealthBarStatus()
    {
        Debug.Log($"[WeaponHealthBar] {gameObject.name}: === HEALTH BAR STATUS ===");
        Debug.Log($"  Connected to TurretControl: {turretControl != null}");
        Debug.Log($"  Connected to SmallCanonControl: {cannonControl != null}");
        Debug.Log($"  Connected to BigCanon: {bigCanonControl != null}");
        Debug.Log($"  Current HP: {currentHP}");
        Debug.Log($"  Max HP: {maxHP}");
        Debug.Log($"  Health Percent: {(maxHP > 0 ? (float)currentHP / maxHP : 0):F2}");
        Debug.Log($"  isFaded: {isFaded}");
        Debug.Log($"  CanvasGroup Alpha: {canvasGroup?.alpha:F2}");
        Debug.Log($"  Last Damage Time: {lastDamageTime:F2}");
        Debug.Log($"  Time Since Damage: {Time.time - lastDamageTime:F2}");
        Debug.Log($"  Hide Delay: {hideDelay}");
        Debug.Log($"  Normal Slider: {normalHealthBarSlider != null}");
        Debug.Log($"  Ease Slider: {easeHealthBarSlider != null}");
        Debug.Log($"  Health Text: {healthText != null}");
        Debug.Log("=== END STATUS ===");
    }
}
