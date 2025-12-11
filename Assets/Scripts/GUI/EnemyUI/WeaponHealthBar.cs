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
    public bool isFaded = false;

    private int maxHP;
    private int currentHP;

    private Camera activeCamera;
    private Transform playerTransform;

    private float lastDamageTime = -100f;
    private CanvasGroup canvasGroup;

    private TurretControl turretControl;
    private SmallCanonControl cannonControl;
    private BigCanon bigCanonControl;

    private float cameraCheckTimer = 0f;
    private const float CAMERA_CHECK_INTERVAL = 1f;
    private float playerSearchTimer = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 2f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
    }

    void Start()
    {
        FindPlayerAndCamera();
        turretControl = GetComponentInParent<TurretControl>() ?? GetComponent<TurretControl>();
        cannonControl = GetComponentInParent<SmallCanonControl>() ?? GetComponent<SmallCanonControl>();
        bigCanonControl = GetComponentInParent<BigCanon>() ?? GetComponent<BigCanon>();
        
        if (turretControl != null)
        {
            maxHP = turretControl.maxHP;
        }
        else if (cannonControl != null)
        {
            maxHP = cannonControl.maxHP;
        }
        else if (bigCanonControl != null)
        {
            maxHP = bigCanonControl.maxHP;
        }
        
        if ((turretControl != null || cannonControl != null || bigCanonControl != null) && maxHP > 0)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
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

        if (playerTransform == null || activeCamera == null)
        {
            playerSearchTimer += Time.deltaTime;
            if (playerSearchTimer >= PLAYER_SEARCH_INTERVAL)
            {
                playerSearchTimer = 0f;
                FindPlayerAndCamera();
            }
        }
        else
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
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime / fadeDuration);
                }
            }
            else
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime / fadeDuration);
                }
            }
        }
        else
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
    }

    void LateUpdate()
    {
        if (activeCamera != null)
        {
            transform.LookAt(transform.position + activeCamera.transform.rotation * Vector3.forward,
                             activeCamera.transform.rotation * Vector3.up);
        }
    }

    void FindPlayerAndCamera()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerTransform = GameManager.Instance.currentPlayer.transform;
            activeCamera = GetActivePlayerCamera(playerTransform);
            return;
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            activeCamera = GetActivePlayerCamera(playerTransform);
        }
    }

    Camera GetActivePlayerCamera(Transform player)
    {
        if (player == null) return Camera.main;
        
        Camera[] cams = player.GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in cams)
        {
            if (cam.enabled)
                return cam;
        }
        return cams.Length > 0 ? cams[0] : Camera.main;
    }

    public void SetHealth(int current, int max)
    {
        if (current != currentHP)
        {
            lastDamageTime = Time.time;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
        currentHP = current;
        maxHP = max;
    }
}
