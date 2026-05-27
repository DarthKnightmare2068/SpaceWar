using UnityEngine;
using TMPro;

public class WeaponHealthBar : DualSliderBar
{
    [Header("UI Text (Optional)")]
    public TextMeshProUGUI healthText;

    [Header("Fade Settings")]
    public float hideDelay = 3f;
    public float fadeDuration = 0.5f;
    public bool isFaded = false;

    private int maxHP;
    private int currentHP;

    private Camera activeCamera;
    private Transform activeCameraTransform;
    private Camera cachedMainCamera;
    private Camera[] cachedPlayerCameras;
    private Transform playerTransform;

    private float lastDamageTime = -100f;
    private CanvasGroup canvasGroup;

    private TurretControl turretControl;
    private SmallCanonControl cannonControl;
    private BigCanon bigCanonControl;
    private IHasHealth healthSource;

    private float cameraCheckTimer = 0f;
    private const float CAMERA_CHECK_INTERVAL = 1f;

    private int lastDisplayedHP = -1;
    private int lastDisplayedMaxHP = -1;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
    }

    void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        TryBindPlayerAndCamera();
    }

    void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    void Start()
    {
        cachedMainCamera = Camera.main;
        TryBindPlayerAndCamera();

        turretControl = GetComponentInParent<TurretControl>() ?? GetComponent<TurretControl>();
        cannonControl = GetComponentInParent<SmallCanonControl>() ?? GetComponent<SmallCanonControl>();
        bigCanonControl = GetComponentInParent<BigCanon>() ?? GetComponent<BigCanon>();

        // Single IHasHealth binding replaces three per-frame type branches.
        if (turretControl != null) healthSource = turretControl;
        else if (cannonControl != null) healthSource = cannonControl;
        else if (bigCanonControl != null) healthSource = bigCanonControl;

        if (healthSource != null)
            maxHP = (int)healthSource.MaxHP;

        if (healthSource != null && maxHP > 0 && canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    void Update()
    {
        bool healthChanged = false;
        if (healthSource != null)
        {
            // Bolt: Optimized - Clamp values before comparison to avoid per-frame updates on negative health
            int nextHP = Mathf.Max(0, (int)healthSource.CurrentHP);
            int nextMax = Mathf.Max(1, (int)healthSource.MaxHP);

            if (nextHP != currentHP || nextMax != maxHP)
            {
                currentHP = nextHP;
                maxHP = nextMax;
                healthChanged = true;
            }
        }

        // Bolt: Optimized - avoid per-frame UI property sets and text updates if health hasn't changed
        // and the ease bar has finished its animation.
        bool easeAnimating = easeHealthBarSlider != null && normalHealthBarSlider != null &&
                             easeHealthBarSlider.value != normalHealthBarSlider.value;

        if (healthChanged || easeAnimating)
        {
            float percent = (float)currentHP / maxHP;
            UpdateBars(percent);

            if (healthText != null && (currentHP != lastDisplayedHP || maxHP != lastDisplayedMaxHP))
            {
                lastDisplayedHP = currentHP;
                lastDisplayedMaxHP = maxHP;
                healthText.text = $"{currentHP} / {maxHP}";
            }
        }

        if (playerTransform != null && activeCamera == null)
        {
            SetActiveCamera(GetActivePlayerCamera(playerTransform));
        }
        else if (playerTransform != null)
        {
            cameraCheckTimer += Time.deltaTime;
            if (cameraCheckTimer >= CAMERA_CHECK_INTERVAL)
            {
                cameraCheckTimer = 0f;
                Camera newActiveCam = GetActivePlayerCamera(playerTransform);
                if (newActiveCam != activeCamera)
                    SetActiveCamera(newActiveCam);
            }
        }

        if (isFaded)
        {
            float timeSinceDamage = Time.time - lastDamageTime;
            if (timeSinceDamage < hideDelay)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime / fadeDuration);
            }
            else
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime / fadeDuration);
            }
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    void LateUpdate()
    {
        if (activeCameraTransform != null)
        {
            transform.rotation = activeCameraTransform.rotation;
        }
    }

    private void SetActiveCamera(Camera cam)
    {
        activeCamera = cam;
        activeCameraTransform = cam != null ? cam.transform : null;
    }

    private void TryBindPlayerAndCamera()
    {
        if (cachedMainCamera == null)
            cachedMainCamera = Camera.main;

        if (GameEntityRegistry.TryGetPlayerTransform(out Transform newTransform))
            SetPlayerTransform(newTransform);
        else
            SetPlayerTransform(null);

        SetActiveCamera(GetActivePlayerCamera(playerTransform));
    }

    private void SetPlayerTransform(Transform newTransform)
    {
        if (newTransform == playerTransform)
            return;

        playerTransform = newTransform;
        cachedPlayerCameras = null;
    }

    private Camera GetActivePlayerCamera(Transform player)
    {
        if (player == null) return cachedMainCamera;

        if (cachedPlayerCameras == null || cachedPlayerCameras.Length == 0)
            cachedPlayerCameras = player.GetComponentsInChildren<Camera>(true);

        foreach (Camera cam in cachedPlayerCameras)
        {
            if (cam != null && cam.enabled)
                return cam;
        }

        return cachedPlayerCameras.Length > 0 ? cachedPlayerCameras[0] : cachedMainCamera;
    }

    public void SetHealth(int current, int max)
    {
        if (current != currentHP)
        {
            lastDamageTime = Time.time;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        currentHP = current;
        maxHP = max;
    }

    private void HandlePlayerChanged(GameObject player)
    {
        SetPlayerTransform(player != null ? player.transform : null);
        SetActiveCamera(GetActivePlayerCamera(playerTransform));
    }
}
