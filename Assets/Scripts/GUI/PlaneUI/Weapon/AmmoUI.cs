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

    private void Update()
    {
        // Keep searching for the weapon manager if not found
        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<PlayerWeaponManager>();
            // If still not found, skip updating UI this frame
            if (weaponManager == null)
                return;
        }

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        // Machine Gun
        if (machineGunAmmoText != null && showText)
        {
            if (weaponManager.isInfinite)
                machineGunAmmoText.text = "∞ / " + weaponManager.maxBullets;
            else
                machineGunAmmoText.text = weaponManager.GetCurrentBullets() + " / " + weaponManager.maxBullets;
        }

        // Missile
        if (missileAmmoText != null && showText)
            missileAmmoText.text = weaponManager.GetCurrentMissiles() + " / " + weaponManager.maxMissiles;
    }
} 