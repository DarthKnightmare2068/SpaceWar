using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class TargetLockUI : MonoBehaviour
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

    [Header("Missile Mode UI")]
    public TMP_Text MissileModeText;
    private float missileModeDisplayTimer = 0f;
    private const float missileModeDisplayDuration = 1f;

    private bool isLaserInRange = false;
    private float blinkTimer = 0f;
    
    private GameObject cachedPlayer;
    private Camera cachedMainCamera;
    private bool isInitialized = false;
    
    private bool cachedEnemyInMissileView = false;
    private float enemyInMissileViewTimer = 0f;
    private const float ENEMY_IN_MISSILE_VIEW_INTERVAL = 0.2f;
    
    // Cached enemy lists to avoid FindGameObjectsWithTag every frame
    private GameObject[] cachedEnemyTargets;
    private float enemyTargetCacheTimer = 0f;
    private const float ENEMY_TARGET_CACHE_INTERVAL = 0.5f;

    // Throttle UI raycasts to ~10 Hz to cut canvas-rebuild churn during turns.
    private float weaponUIRaycastTimer = 0f;
    private float laserUIRaycastTimer = 0f;
    private const float UI_RAYCAST_INTERVAL = 0.1f;
    private bool cachedMgInRange = false;
    private bool cachedLaserHitInRange = false;

    private float sqrMissileFireRange;
    private float sqrLockCircleRadius;

    // Last-applied SetActive state — skip the call when nothing changed.
    private bool lastMachineGunUIActive;
    private bool lastMissileLockUIActive;
    private bool lastNormalUIActive;
    private bool hasAppliedMachineGunUI = false;
    private bool hasAppliedMissileLockUI = false;
    private bool hasAppliedNormalUI = false;

    void Start()
    {
        cachedMainCamera = Camera.main;
    }

    void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        isInitialized = false;
        TryInitializeReferences();
    }

    void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
        DisconnectFromAutoTargetLock();
    }


    void Update()
    {
        if (!isInitialized || cachedPlayer == null || !cachedPlayer.activeInHierarchy)
            return;

        UpdateWeaponUI();
        UpdateMissileUI();
        UpdateLaserUI();
        UpdateCheatUI();
        UpdateMissileModeUI();
    }

    private void UpdateWeaponUI()
    {
        if (weaponManager == null) return;

        bool inFireRange = weaponManager.IsTargetInRange(weaponManager.machineGunFireRange);
        bool missileInRange = weaponManager.IsTargetInRange(weaponManager.missileFireRange);

        SetUIActive(ref lastMachineGunUIActive, ref hasAppliedMachineGunUI, machineGunUI, inFireRange);
        SetUIActive(ref lastMissileLockUIActive, ref hasAppliedMissileLockUI, missileLockUI, missileInRange);

        bool normalShouldBeActive = !(inFireRange || missileInRange || isLaserInRange);
        SetUIActive(ref lastNormalUIActive, ref hasAppliedNormalUI, normalUI, normalShouldBeActive);

        weaponUIRaycastTimer -= Time.deltaTime;
        if (weaponUIRaycastTimer <= 0f)
        {
            weaponUIRaycastTimer = UI_RAYCAST_INTERVAL;
            cachedMgInRange = false;
            if (weaponManager.TryGetCurrentTargetHit(out RaycastHit hit))
            {
                if ((hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Turret")) &&
                    hit.distance <= weaponManager.machineGunFireRange)
                {
                    cachedMgInRange = true;
                }
            }
        }

        bool targetUIActive = lastMachineGunUIActive;
        bool normalUIActive = lastNormalUIActive;

        if (cachedMgInRange && !targetUIActive)
        {
            UpdateUI(true);
        }
        else if (!cachedMgInRange && !normalUIActive)
        {
            UpdateUI(false);
        }
    }

    private void SetUIActive(ref bool lastState, ref bool hasApplied, GameObject target, bool active)
    {
        if (target == null) return;
        if (hasApplied && lastState == active) return;
        target.SetActive(active);
        lastState = active;
        hasApplied = true;
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
                // Bolt: Optimized - replaced Vector3.Distance with sqrMagnitude to save a square root calculation
                float missileSqrDistance = (autoTargetLock.lockedTarget.position - missileFromPos).sqrMagnitude;
                inMissileRange = missileSqrDistance <= sqrMissileFireRange;
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

    private void RefreshEnemyTargetCache()
    {
        // Pull from the registry-backed list to avoid scene-wide FindGameObjectsWithTag scans.
        var ships = GameEntityRegistry.GetEnemyShips();
        if (ships == null)
        {
            cachedEnemyTargets = null;
            return;
        }

        int count = ships.Count;
        if (cachedEnemyTargets == null || cachedEnemyTargets.Length != count)
            cachedEnemyTargets = new GameObject[count];
        for (int i = 0; i < count; i++)
            cachedEnemyTargets[i] = ships[i];
    }
    
    private bool CheckEnemyInMissileView()
    {
        if (autoTargetLock == null || weaponManager == null || missileLaunch == null) return false;
        if (cachedMainCamera == null) cachedMainCamera = Camera.main;
        if (cachedMainCamera == null) return false;

        // Refresh cache at intervals instead of every call
        enemyTargetCacheTimer += Time.deltaTime;
        if (enemyTargetCacheTimer >= ENEMY_TARGET_CACHE_INTERVAL || cachedEnemyTargets == null)
        {
            enemyTargetCacheTimer = 0f;
            RefreshEnemyTargetCache();
        }
        
        if (cachedEnemyTargets == null) return false;

        // Use cached enemy list
        foreach (GameObject obj in cachedEnemyTargets)
        {
            if (obj == null || !obj.activeInHierarchy) continue;
            // Bolt: Optimized - replaced Vector3.Distance with sqrMagnitude
            float sqrDist = (obj.transform.position - missileLaunch.transform.position).sqrMagnitude;
            if (sqrDist <= sqrMissileFireRange)
            {
                Vector3 viewportPos = cachedMainCamera.WorldToViewportPoint(obj.transform.position);
                // Bolt: Optimized - manual sqrDist calculation instead of Vector2.Distance to avoid sqrt
                float dx = viewportPos.x - 0.5f;
                float dy = viewportPos.y - 0.5f;
                float sqrDistFromCenter = dx * dx + dy * dy;
                if (sqrDistFromCenter <= sqrLockCircleRadius)
                {
                    return true;
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
            laserUIRaycastTimer -= Time.deltaTime;
            if (laserUIRaycastTimer <= 0f)
            {
                laserUIRaycastTimer = UI_RAYCAST_INTERVAL;
                float range = laserActive.CurrentBeamLength;
                Ray ray = laserActive.weaponManager != null ? laserActive.weaponManager.GetCurrentTargetRay() : cachedMainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                cachedLaserHitInRange = false;
                if (Physics.Raycast(ray, out RaycastHit hit, range))
                {
                    if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Turret"))
                    {
                        cachedLaserHitInRange = true;
                    }
                }
            }
            isLaserInRange = cachedLaserHitInRange;
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

                // Bolt: Optimized - only set text if it's different to avoid native property writes
                if (laserRangeText.text != "Laser in Fire Range!")
                {
                    laserRangeText.text = "Laser in Fire Range!";
                }
            }
            else
            {
                laserRangeText.enabled = false;
                blinkTimer = 0f;
            }
        }
    }

    void OnDestroy()
    {
        DisconnectFromAutoTargetLock();
    }
    
    private void UpdateUI(bool targetLocked)
    {
        SetUIActive(ref lastNormalUIActive, ref hasAppliedNormalUI, normalUI, !targetLocked);
        SetUIActive(ref lastMachineGunUIActive, ref hasAppliedMachineGunUI, machineGunUI, targetLocked);
    }

    private void UpdateMissileUIState(bool missileLocked)
    {
        SetUIActive(ref lastMissileLockUIActive, ref hasAppliedMissileLockUI, missileLockUI, missileLocked);
    }
    
    public void ForceShowNormal()
    {
        UpdateUI(false);
    }
    
    public void ForceShowTargetLock()
    {
        UpdateUI(true);
    }
    
    public void SetTargetLockUI(RectTransform uiElement)
    {
        if (uiElement != null)
        {
            machineGunUI = uiElement.gameObject;
        }
    }
}
