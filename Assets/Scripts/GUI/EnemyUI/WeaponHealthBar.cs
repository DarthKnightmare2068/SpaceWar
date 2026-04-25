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
    private Camera cachedMainCamera;
    private Camera[] cachedPlayerCameras;
    private Transform playerTransform;

    private float lastDamageTime = -100f;
    private CanvasGroup canvasGroup;

    private TurretControl turretControl;
    private SmallCanonControl cannonControl;
    private BigCanon bigCanonControl;

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

        if (turretControl != null)
            maxHP = turretControl.maxHP;
        else if (cannonControl != null)
            maxHP = cannonControl.maxHP;
        else if (bigCanonControl != null)
            maxHP = bigCanonControl.maxHP;

        if ((turretControl != null || cannonControl != null || bigCanonControl != null) && maxHP > 0 && canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    void Update()
    {
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

        if (maxHP <= 0) maxHP = 1;
        if (currentHP < 0) currentHP = 0;

        float percent = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        UpdateBars(percent);

        if (healthText != null && (currentHP != lastDisplayedHP || maxHP != lastDisplayedMaxHP))
        {
            lastDisplayedHP = currentHP;
            lastDisplayedMaxHP = maxHP;
            healthText.text = $"{currentHP} / {maxHP}";
        }

        if (playerTransform != null && activeCamera == null)
        {
            activeCamera = GetActivePlayerCamera(playerTransform);
        }
        else if (playerTransform != null)
        {
            cameraCheckTimer += Time.deltaTime;
            if (cameraCheckTimer >= CAMERA_CHECK_INTERVAL)
            {
                cameraCheckTimer = 0f;
                Camera newActiveCam = GetActivePlayerCamera(playerTransform);
                if (newActiveCam != activeCamera)
                    activeCamera = newActiveCam;
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
        if (activeCamera != null)
        {
            transform.LookAt(
                transform.position + activeCamera.transform.rotation * Vector3.forward,
                activeCamera.transform.rotation * Vector3.up);
        }
    }

    private void TryBindPlayerAndCamera()
    {
        if (cachedMainCamera == null)
            cachedMainCamera = Camera.main;

        if (GameEntityRegistry.TryGetPlayerTransform(out Transform newTransform))
            SetPlayerTransform(newTransform);
        else
            SetPlayerTransform(null);

        activeCamera = GetActivePlayerCamera(playerTransform);
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
        activeCamera = GetActivePlayerCamera(playerTransform);
    }
}
