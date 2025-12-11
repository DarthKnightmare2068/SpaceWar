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

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (targetLockUI == null) {
            GameObject uiObj = GameObject.Find("Center");
            if (uiObj != null)
                targetLockUI = uiObj.GetComponent<RectTransform>();
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
        
        RaycastHit hit;
        if (Physics.Raycast(currentTargetRay, out hit, machineGunFireRange, targetableLayers))
        {
            currentTargetPosition = hit.point;
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
        
        RaycastHit hit;
        if (Physics.Raycast(currentTargetRay, out hit, range, targetableLayers))
        {
            bool isEnemy = hit.collider.CompareTag("Enemy");
            bool isTurret = hit.collider.CompareTag("Turret");
            bool inRange = isEnemy || isTurret;
            
            return inRange;
        }
        
        return false;
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
