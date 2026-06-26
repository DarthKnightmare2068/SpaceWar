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

    // Throttled to avoid a per-frame raycast; IsTargetInRange reuses the cached hit.
    private RaycastHit currentTargetHit;
    private bool currentTargetHitValid;
    private bool currentTargetIsTargetable;
    private float targetRaycastTimer;
    private const float TARGET_RAYCAST_INTERVAL = 0.1f;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (targetLockUI == null)
        {
            var lockUI = FindAnyObjectByType<TargetLockUI>();
            if (lockUI != null)
                targetLockUI = lockUI.GetComponent<RectTransform>();
        }
        currentBullets = maxBullets;
        currentMissiles = maxMissiles;
        nextLaunchTime = 0f;
        isReloading = false;
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

        targetRaycastTimer -= Time.deltaTime;
        if (targetRaycastTimer <= 0f)
        {
            targetRaycastTimer = TARGET_RAYCAST_INTERVAL;
            currentTargetHitValid = Physics.Raycast(currentTargetRay, out currentTargetHit, machineGunFireRange, targetableLayers);

            if (currentTargetHitValid && currentTargetHit.collider != null)
            {
                // Bolt: Optimized - cache targetable status at the same frequency as the raycast (10Hz)
                // to avoid redundant CompareTag calls in the high-frequency IsTargetInRange path.
                currentTargetIsTargetable = currentTargetHit.collider.CompareTag("Enemy") ||
                                            currentTargetHit.collider.CompareTag("Turret");
            }
            else
            {
                currentTargetIsTargetable = false;
            }
        }

        if (currentTargetHitValid)
        {
            currentTargetPosition = currentTargetHit.point;
        }
        else
        {
            currentTargetPosition = currentTargetRay.origin + currentTargetRay.direction * machineGunFireRange;
        }
    }

    public bool TryGetCurrentTargetHit(out RaycastHit hit)
    {
        hit = currentTargetHit;
        return currentTargetHitValid;
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

        // Bolt: Optimized - use cached targetable status and raycast results.
        // This avoids per-frame native-to-managed CompareTag calls.
        if (!currentTargetHitValid || !currentTargetIsTargetable) return false;

        // Safety: ensure collider wasn't destroyed since the last 10Hz raycast.
        if (currentTargetHit.collider == null) { currentTargetHitValid = false; currentTargetIsTargetable = false; return false; }

        return currentTargetHit.distance <= range;
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
