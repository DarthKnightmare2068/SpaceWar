using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TargetLockUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject normalUI;
    public GameObject machineGunUI;
    public GameObject missileLockUI;
    public TMP_Text laserRangeText;
    public float blinkInterval = 0.5f;

    [Header("AutoTarget Reference")]
    public AutoTargetLock autoTargetLock;

    [Header("Weapon Reference")]
    public MachineGunControl machineGunControl;
    public PlayerWeaponManager weaponManager;
    public MissileLaunch missileLaunch;
    public LaserActive laserActive;

    [Header("Cheat/Debug UI")]
    public TMP_Text CheatHp;
    private float cheatHpDisplayTimer = 0f;
    private const float cheatHpDisplayDuration = 1f;
    private PlaneStats playerStats;
    private bool lastCanTakeDamage = true;

    [Header("Missile Mode UI")]
    public TMP_Text MissileModeText;
    private float missileModeDisplayTimer = 0f;
    private const float missileModeDisplayDuration = 1f;

    private bool isLaserInRange = false;
    private float blinkTimer = 0f;
    private bool referencesChecked = false;
    
    private GameObject cachedPlayer;
    private Camera cachedMainCamera;
    private float playerSearchCooldown = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 0.5f;
    private bool isInitialized = false;
    
    private bool cachedEnemyInMissileView = false;
    private float enemyInMissileViewTimer = 0f;
    private const float ENEMY_IN_MISSILE_VIEW_INTERVAL = 0.2f;

    void Start()
    {
        cachedMainCamera = Camera.main;
        TryInitializeReferences();
    }

    void OnEnable()
    {
        isInitialized = false;
        TryInitializeReferences();
    }

    private void TryInitializeReferences()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            cachedPlayer = GameManager.Instance.currentPlayer;
            CachePlayerComponents();
            isInitialized = true;
        }
        else
        {
            cachedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayer != null)
            {
                CachePlayerComponents();
                isInitialized = true;
            }
        }
        
        if (autoTargetLock == null && cachedPlayer != null)
        {
            autoTargetLock = cachedPlayer.GetComponent<AutoTargetLock>();
        }
        
        if (autoTargetLock != null)
        {
            ConnectToAutoTargetLock();
        }
    }

    private void CachePlayerComponents()
    {
        if (cachedPlayer == null) return;
        
        if (machineGunControl == null)
            machineGunControl = cachedPlayer.GetComponent<MachineGunControl>();
        if (weaponManager == null)
            weaponManager = cachedPlayer.GetComponent<PlayerWeaponManager>();
        if (missileLaunch == null)
            missileLaunch = cachedPlayer.GetComponent<MissileLaunch>();
        if (laserActive == null)
            laserActive = cachedPlayer.GetComponent<LaserActive>();
        if (playerStats == null)
            playerStats = cachedPlayer.GetComponent<PlaneStats>();
        if (autoTargetLock == null)
            autoTargetLock = cachedPlayer.GetComponent<AutoTargetLock>();
    }

    void Update()
    {
        if (!isInitialized || cachedPlayer == null || !cachedPlayer.activeInHierarchy)
        {
            playerSearchCooldown -= Time.deltaTime;
            if (playerSearchCooldown <= 0f)
            {
                playerSearchCooldown = PLAYER_SEARCH_INTERVAL;
                TryInitializeReferences();
            }
            
            if (!isInitialized || cachedPlayer == null)
            {
                return;
            }
        }

        if (!referencesChecked)
        {
            referencesChecked = true;
        }

        UpdateWeaponUI();
        UpdateMissileUI();
        UpdateLaserUI();
        UpdateCheatUI();
        UpdateMissileModeUI();
    }

    private void UpdateWeaponUI()
    {
        bool inFireRange = false;
        if (machineGunUI != null && weaponManager != null)
        {
            inFireRange = weaponManager.IsTargetInRange(weaponManager.machineGunFireRange);
            machineGunUI.SetActive(inFireRange);
        }

        bool missileInRange = false;
        if (missileLockUI != null && weaponManager != null)
        {
            missileInRange = weaponManager.IsTargetInRange(weaponManager.missileFireRange);
            missileLockUI.SetActive(missileInRange);
        }

        if (normalUI != null)
            normalUI.SetActive(!(inFireRange || missileInRange || isLaserInRange));

        if (weaponManager == null) return;

        bool mgInRange = false;
        Ray ray = weaponManager.GetCurrentTargetRay();
        RaycastHit hit;
        LayerMask targetableLayers = weaponManager.GetTargetableLayers();
        
        if (Physics.Raycast(ray, out hit, weaponManager.machineGunFireRange, targetableLayers))
        {
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Turret"))
            {
                float hitDistance = hit.distance;
                mgInRange = hitDistance <= weaponManager.machineGunFireRange;
            }
        }

        bool targetUIActive = machineGunUI != null && machineGunUI.activeInHierarchy;
        bool normalUIActive = normalUI != null && normalUI.activeInHierarchy;

        if (mgInRange && !targetUIActive)
        {
            UpdateUI(true);
        }
        else if (!mgInRange && !normalUIActive)
        {
            UpdateUI(false);
        }
    }

    private string GetTargetType(Collider collider)
    {
        if (collider.CompareTag("Enemy")) return "Enemy Ship";
        
        var turret = collider.GetComponentInParent<TurretControl>();
        if (turret != null) return "Turret";
        
        var smallCanon = collider.GetComponentInParent<SmallCanonControl>();
        if (smallCanon != null) return "Small Cannon";
        
        var bigCanon = collider.GetComponentInParent<BigCanon>();
        if (bigCanon != null) return "Big Cannon";
        
        return "Unknown Weapon";
    }

    private void UpdateMissileUI()
    {
        bool missileUIActive = missileLockUI != null && missileLockUI.activeInHierarchy;
        
        if (missileLaunch != null && !missileLaunch.useAutoTargetLock)
        {
            enemyInMissileViewTimer += Time.deltaTime;
            if (enemyInMissileViewTimer >= ENEMY_IN_MISSILE_VIEW_INTERVAL)
            {
                enemyInMissileViewTimer = 0f;
                cachedEnemyInMissileView = CheckEnemyInMissileView();
            }
            
            if (cachedEnemyInMissileView && !missileUIActive)
            {
                UpdateMissileUIState(true);
            }
            else if (!cachedEnemyInMissileView && missileUIActive)
            {
                UpdateMissileUIState(false);
            }
        }
        else
        {
            if (autoTargetLock == null) return;
            
            bool hasTarget = autoTargetLock.HasTarget();
            bool inMissileRange = false;
            
            if (hasTarget && autoTargetLock.lockedTarget != null && weaponManager != null)
            {
                Vector3 missileFromPos = missileLaunch != null ? missileLaunch.transform.position : transform.position;
                float missileDistance = Vector3.Distance(missileFromPos, autoTargetLock.lockedTarget.position);
                inMissileRange = missileDistance <= weaponManager.missileFireRange;
            }
            
            if (hasTarget && inMissileRange && !missileUIActive)
            {
                UpdateMissileUIState(true);
            }
            else if ((!hasTarget || !inMissileRange) && missileUIActive)
            {
                UpdateMissileUIState(false);
            }
        }
    }

    private bool CheckEnemyInMissileView()
    {
        if (autoTargetLock == null || weaponManager == null || missileLaunch == null) return false;
        if (cachedMainCamera == null) cachedMainCamera = Camera.main;
        if (cachedMainCamera == null) return false;

        foreach (string tag in autoTargetLock.targetTags)
        {
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in candidates)
            {
                if (obj == null) continue;
                float distance = Vector3.Distance(missileLaunch.transform.position, obj.transform.position);
                if (distance <= weaponManager.missileFireRange)
                {
                    Vector3 viewportPos = cachedMainCamera.WorldToViewportPoint(obj.transform.position);
                    float distFromCenter = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
                    if (distFromCenter <= autoTargetLock.lockCircleRadius)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void UpdateLaserUI()
    {
        if (laserActive == null)
        {
            isLaserInRange = false;
        }
        else
        {
            float range = laserActive.CurrentBeamLength;
            Ray ray = laserActive.weaponManager != null ? laserActive.weaponManager.GetCurrentTargetRay() : cachedMainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            bool inRange = false;

            if (Physics.Raycast(ray, out hit, range))
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Turret"))
                {
                    inRange = true;
                }
            }
            isLaserInRange = inRange;
        }

        if (laserRangeText != null)
        {
            if (isLaserInRange)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkInterval)
                {
                    laserRangeText.enabled = !laserRangeText.enabled;
                    blinkTimer = 0f;
                }
                laserRangeText.text = "Laser in Fire Range!";
            }
            else
            {
                laserRangeText.enabled = false;
                blinkTimer = 0f;
            }
        }
    }

    private void UpdateCheatUI()
    {
        if (playerStats != null && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.J))
        {
            playerStats.canTakeDamage = !playerStats.canTakeDamage;
            lastCanTakeDamage = playerStats.canTakeDamage;
            if (CheatHp != null)
            {
                CheatHp.gameObject.SetActive(true);
                CheatHp.text = "CAN TAKE DAMAGE: " + (playerStats.canTakeDamage ? "ON" : "OFF");
                cheatHpDisplayTimer = cheatHpDisplayDuration;
            }
        }

        if (CheatHp != null && CheatHp.gameObject.activeSelf)
        {
            if (cheatHpDisplayTimer > 0f)
            {
                cheatHpDisplayTimer -= Time.deltaTime;
                if (cheatHpDisplayTimer <= 0f)
                {
                    CheatHp.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateMissileModeUI()
    {
        if (MissileModeText != null && MissileModeText.gameObject.activeSelf)
        {
            if (missileModeDisplayTimer > 0f)
            {
                missileModeDisplayTimer -= Time.deltaTime;
                if (missileModeDisplayTimer <= 0f)
                {
                    MissileModeText.gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void ConnectToAutoTargetLock()
    {
        if (autoTargetLock == null) return;
        
        autoTargetLock.OnTargetLocked += OnTargetLocked;
        autoTargetLock.OnTargetLost += OnTargetLost;
    }

    void OnDestroy()
    {
        if (autoTargetLock != null)
        {
            autoTargetLock.OnTargetLocked -= OnTargetLocked;
            autoTargetLock.OnTargetLost -= OnTargetLost;
        }
    }
    
    private void OnTargetLocked(Transform target)
    {
        UpdateUI(true);
    }
    
    private void OnTargetLost(Transform target)
    {
        UpdateUI(false);
    }
    
    private void UpdateUI(bool targetLocked)
    {
        if (normalUI != null)
        {
            normalUI.SetActive(!targetLocked);
        }
        
        if (machineGunUI != null)
        {
            machineGunUI.SetActive(targetLocked);
        }
    }
    
    private void UpdateMissileUIState(bool missileLocked)
    {
        if (missileLockUI != null)
        {
            missileLockUI.SetActive(missileLocked);
        }
    }
    
    public void ForceShowNormal()
    {
        UpdateUI(false);
    }
    
    public void ForceShowTargetLock()
    {
        UpdateUI(true);
    }
    
    public void SetAutoTargetLock(AutoTargetLock targetLock)
    {
        if (autoTargetLock != null)
        {
            autoTargetLock.OnTargetLocked -= OnTargetLocked;
            autoTargetLock.OnTargetLost -= OnTargetLost;
        }
        
        autoTargetLock = targetLock;
        
        if (autoTargetLock != null)
        {
            ConnectToAutoTargetLock();
        }
    }
    
    public void SetTargetLockUI(RectTransform uiElement)
    {
        if (uiElement != null)
        {
            machineGunUI = uiElement.gameObject;
        }
    }

    public void OnPlayerSpawned(GameObject player)
    {
        cachedPlayer = player;
        isInitialized = false;
        referencesChecked = false;
        CachePlayerComponents();
        isInitialized = true;
    }

    public void ShowMissileMode()
    {
        if (MissileModeText != null && missileLaunch != null)
        {
            MissileModeText.gameObject.SetActive(true);
            MissileModeText.color = Color.red;
            if (missileLaunch.useAutoTargetLock)
                MissileModeText.text = "Missile Mode: Auto Target Lock";
            else
                MissileModeText.text = "Missile Mode: Straight";
            missileModeDisplayTimer = missileModeDisplayDuration;
        }
    }
}
