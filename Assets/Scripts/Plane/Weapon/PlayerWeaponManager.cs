using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform targetLockUI;
    [SerializeField] private Camera mainCamera;

    [Header("Weapon Ranges")]
    public float machineGunFireRange = 1000f;
    public float missileFireRange = 800f;

    [Header("Targeting")]
    [SerializeField] private LayerMask targetableLayers = 1;

    [Header("Machine Gun Settings")]
    public float machineGunFireRate = 0.1f;
    public int maxBullets = 30;
    public bool isInfinite = false;
    public int currentBullets;
    public bool isReloading = false;
    public float reloadTime = 2f;

    [Header("Missile Settings")]
    public float missileLaunchDelay = 3f;
    public int maxMissiles = 3;
    public int currentMissiles;
    public float nextLaunchTime = 0f;

    private Vector3 currentTargetPosition;
    private Ray currentTargetRay;

    private RaycastHit cachedHit;
    private bool hasCachedHit;
    private int lastRaycastFrame = -1;
    private float maxSearchRange;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (targetLockUI == null)
        {
            var lockUI = FindObjectOfType<TargetLockUI>();
            if (lockUI != null)
                targetLockUI = lockUI.GetComponent<RectTransform>();
        }
        currentBullets = maxBullets;
        currentMissiles = maxMissiles;
        nextLaunchTime = 0f;
        isReloading = false;

        // Bolt: Optimized - Pre-calculate max range for cached raycast
        UpdateMaxSearchRange();
    }

    private void UpdateMaxSearchRange()
    {
        maxSearchRange = Mathf.Max(machineGunFireRange, missileFireRange);
        // Laser range is typically smaller (100f) but we should be safe
        var laser = GetComponentInChildren<LaserActive>();
        if (laser != null) maxSearchRange = Mathf.Max(maxSearchRange, laser.laserFireRange);
    }

    private void Update()
    {
        UpdateTargetPosition();
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentBullets < maxBullets)
        {
            StartCoroutine(Reload());
        }
    }

    private void UpdateTargetPosition()
    {
        if (mainCamera == null) return;

        // Bolt: Optimized - Ensure raycast is performed only once per frame
        if (lastRaycastFrame == Time.frameCount) return;
        lastRaycastFrame = Time.frameCount;

        Vector3 viewportPoint;
        if (targetLockUI != null)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetLockUI.position);
            viewportPoint = mainCamera.ScreenToViewportPoint(screenPoint);
        }
        else
        {
            viewportPoint = new Vector3(0.5f, 0.5f, 0f);
        }
        currentTargetRay = mainCamera.ViewportPointToRay(viewportPoint);
        
        hasCachedHit = Physics.Raycast(currentTargetRay, out cachedHit, maxSearchRange, targetableLayers);
        if (hasCachedHit)
        {
            currentTargetPosition = cachedHit.point;
        }
        else
        {
            currentTargetPosition = currentTargetRay.origin + currentTargetRay.direction * machineGunFireRange;
        }
    }

    public bool CanFireBullet() => isInfinite || (currentBullets > 0 && !isReloading);
    public void UseBullet() { if (!isInfinite && currentBullets > 0) currentBullets--; }
    public int GetCurrentBullets() => currentBullets;

    public bool CanFireMissile() => currentMissiles > 0;
    public void UseMissile() { if (currentMissiles > 0) currentMissiles--; }
    public int GetCurrentMissiles() => currentMissiles;

    public Vector3 GetCurrentTargetPosition()
    {
        return currentTargetPosition;
    }

    public Ray GetCurrentTargetRay()
    {
        return currentTargetRay;
    }

    public LayerMask GetTargetableLayers()
    {
        return targetableLayers;
    }

    public bool IsTargetInRange(float range)
    {
        if (targetLockUI == null) 
        {
            return false;
        }

        // Bolt: Optimized - Use cached raycast result instead of performing a new one
        UpdateTargetPosition();
        
        if (hasCachedHit && cachedHit.distance <= range)
        {
            bool isEnemy = cachedHit.collider.CompareTag("Enemy");
            bool isTurret = cachedHit.collider.CompareTag("Turret");
            bool inRange = isEnemy || isTurret;
            
            return inRange;
        }
        
        return false;
    }

    public bool TryGetCachedHit(out RaycastHit hit)
    {
        UpdateTargetPosition();
        hit = cachedHit;
        return hasCachedHit;
    }

    public void SetTargetLockUI(RectTransform uiElement)
    {
        if (uiElement != null)
        {
            targetLockUI = uiElement;
        }
    }

    public void LevelUp()
    {
        maxBullets += 11;
        maxMissiles += 1;
        currentBullets = maxBullets;
        currentMissiles = maxMissiles;
    }

    public IEnumerator Reload()
    {
        if (isReloading) yield break;
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentBullets = maxBullets;
        isReloading = false;
    }
}
