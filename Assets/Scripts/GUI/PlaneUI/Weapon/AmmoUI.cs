using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [Header("Machine Gun UI")]
    public TextMeshProUGUI machineGunAmmoText;

    [Header("Missile UI")]
    public TextMeshProUGUI missileAmmoText;

    public bool showText = true;

    private PlayerWeaponManager weaponManager;

    private int lastBullets = -1;
    private int lastMaxBullets = -1;
    private bool lastIsInfinite = false;
    private int lastMissiles = -1;
    private int lastMaxMissiles = -1;
    private bool uiResetDone = false;

    private Color originalMGColor;
    private Color originalMissileColor;
    private bool colorsCached = false;

    private float timeSinceLastUpdate = 0f;
    private const float UPDATE_INTERVAL = 0.1f;

    private void OnEnable()
    {
        CacheOriginalColors();
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            HandlePlayerChanged(player);
        else
            HandlePlayerChanged(null);
    }

    private void CacheOriginalColors()
    {
        if (colorsCached) return;

        if (machineGunAmmoText != null)
            originalMGColor = machineGunAmmoText.color;
        if (missileAmmoText != null)
            originalMissileColor = missileAmmoText.color;

        colorsCached = true;
    }

    private void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    private void Update()
    {
        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if (weaponManager == null)
        {
            if (timeSinceLastUpdate >= UPDATE_INTERVAL)
            {
                ResetAmmoUI();
                timeSinceLastUpdate = 0f;
            }
            return;
        }

        if (timeSinceLastUpdate >= UPDATE_INTERVAL)
        {
            UpdateAmmoUI();
            timeSinceLastUpdate = 0f;
        }
    }

    private void UpdateAmmoUI()
    {
        uiResetDone = false;
        if (machineGunAmmoText != null && showText)
        {
            if (weaponManager.isReloading)
            {
                machineGunAmmoText.SetText("RELOADING...");
                machineGunAmmoText.color = new Color(1f, 0.7f, 0f); // Amber
                lastBullets = -2; // Force refresh when reloading ends
            }
            else
            {
                int curBullets = weaponManager.GetCurrentBullets();
                int maxBullets = weaponManager.maxBullets;
                bool infinite = weaponManager.isInfinite;

                if (infinite != lastIsInfinite || curBullets != lastBullets || maxBullets != lastMaxBullets)
                {
                    // Bolt: Optimized - use SetText to avoid string allocations from concatenation
                    if (infinite)
                    {
                        machineGunAmmoText.SetText("inf / {0}", maxBullets);
                        machineGunAmmoText.color = originalMGColor;
                    }
                    else
                    {
                        machineGunAmmoText.SetText("{0} / {1}", curBullets, maxBullets);
                        if (curBullets <= maxBullets * 0.25f)
                            machineGunAmmoText.color = Color.red;
                        else
                            machineGunAmmoText.color = originalMGColor;
                    }

                    lastBullets = curBullets;
                    lastMaxBullets = maxBullets;
                    lastIsInfinite = infinite;
                }
            }
        }

        if (missileAmmoText != null && showText)
        {
            int curMissiles = weaponManager.GetCurrentMissiles();
            int maxMissiles = weaponManager.maxMissiles;
            if (curMissiles != lastMissiles || maxMissiles != lastMaxMissiles)
            {
                // Bolt: Optimized - use SetText to avoid string allocations
                missileAmmoText.SetText("{0} / {1}", curMissiles, maxMissiles);

                if (curMissiles == 0)
                    missileAmmoText.color = Color.red;
                else
                    missileAmmoText.color = originalMissileColor;

                lastMissiles = curMissiles;
                lastMaxMissiles = maxMissiles;
            }
        }
    }

    private void HandlePlayerChanged(GameObject player)
    {
        weaponManager = player != null ? player.GetComponent<PlayerWeaponManager>() : null;
        ResetAmmoUI();
    }

    private void ResetAmmoUI()
    {
        if (uiResetDone) return;

        if (machineGunAmmoText != null && showText)
        {
            machineGunAmmoText.text = "-- / --";
            if (colorsCached) machineGunAmmoText.color = originalMGColor;
        }

        if (missileAmmoText != null && showText)
        {
            missileAmmoText.text = "-- / --";
            if (colorsCached) missileAmmoText.color = originalMissileColor;
        }

        lastBullets = -1;
        lastMaxBullets = -1;
        lastIsInfinite = false;
        lastMissiles = -1;
        lastMaxMissiles = -1;
        uiResetDone = true;
    }
}
