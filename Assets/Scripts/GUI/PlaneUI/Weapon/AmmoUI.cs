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

    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.1f;
    private bool uiResetDone = false;

    private void OnEnable()
    {
        GameEntityRegistry.PlayerChanged += HandlePlayerChanged;
        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            HandlePlayerChanged(player);
        else
            HandlePlayerChanged(null);
    }

    private void OnDisable()
    {
        GameEntityRegistry.PlayerChanged -= HandlePlayerChanged;
    }

    private void Update()
    {
        if (weaponManager == null)
        {
            ResetAmmoUI();
            return;
        }

        updateTimer += Time.unscaledDeltaTime;
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;
            UpdateAmmoUI();
        }
    }

    private void UpdateAmmoUI()
    {
        uiResetDone = false;
        if (machineGunAmmoText != null && showText)
        {
            int curBullets = weaponManager.GetCurrentBullets();
            int maxBullets = weaponManager.maxBullets;
            bool infinite = weaponManager.isInfinite;
            if (infinite != lastIsInfinite || curBullets != lastBullets || maxBullets != lastMaxBullets)
            {
                if (infinite)
                    machineGunAmmoText.SetText("inf / {0}", (float)maxBullets);
                else
                    machineGunAmmoText.SetText("{0} / {1}", (float)curBullets, (float)maxBullets);

                lastBullets = curBullets;
                lastMaxBullets = maxBullets;
                lastIsInfinite = infinite;
            }
        }

        if (missileAmmoText != null && showText)
        {
            int curMissiles = weaponManager.GetCurrentMissiles();
            int maxMissiles = weaponManager.maxMissiles;
            if (curMissiles != lastMissiles || maxMissiles != lastMaxMissiles)
            {
                missileAmmoText.SetText("{0} / {1}", (float)curMissiles, (float)maxMissiles);
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
            machineGunAmmoText.text = "-- / --";

        if (missileAmmoText != null && showText)
            missileAmmoText.text = "-- / --";

        lastBullets = -1;
        lastMaxBullets = -1;
        lastIsInfinite = false;
        lastMissiles = -1;
        lastMaxMissiles = -1;
        uiResetDone = true;
    }
}
