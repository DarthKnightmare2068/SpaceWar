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
    private float searchTimer = 0f;
    private const float SEARCH_INTERVAL = 1f;

    private int lastBullets = -1;
    private int lastMaxBullets = -1;
    private bool lastIsInfinite = false;
    private int lastMissiles = -1;
    private int lastMaxMissiles = -1;

    private void Start()
    {
        // Try GameManager cached reference first — avoids a scene-wide FindObjectOfType.
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            weaponManager = GameManager.Instance.currentPlayer.GetComponent<PlayerWeaponManager>();

        if (weaponManager == null)
            weaponManager = FindObjectOfType<PlayerWeaponManager>();
    }

    // Called by GameManager after player spawns.
    public void OnPlayerSpawned(GameObject player)
    {
        if (player != null)
            weaponManager = player.GetComponent<PlayerWeaponManager>();
    }

    private void Update()
    {
        if (weaponManager == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer < SEARCH_INTERVAL) return;
            searchTimer = 0f;

            if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
                weaponManager = GameManager.Instance.currentPlayer.GetComponent<PlayerWeaponManager>();
            if (weaponManager == null)
                weaponManager = FindObjectOfType<PlayerWeaponManager>();
            if (weaponManager == null) return;
        }

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (machineGunAmmoText != null && showText)
        {
            int curBullets = weaponManager.GetCurrentBullets();
            int maxBullets = weaponManager.maxBullets;
            bool infinite = weaponManager.isInfinite;
            if (infinite != lastIsInfinite || curBullets != lastBullets || maxBullets != lastMaxBullets)
            {
                machineGunAmmoText.text = infinite ? ("∞ / " + maxBullets) : (curBullets + " / " + maxBullets);
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
                missileAmmoText.text = curMissiles + " / " + maxMissiles;
                lastMissiles = curMissiles;
                lastMaxMissiles = maxMissiles;
            }
        }
    }
}
