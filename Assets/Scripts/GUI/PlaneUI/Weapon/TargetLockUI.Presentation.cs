using TMPro;
using UnityEngine;

public partial class TargetLockUI
{
    private void TryInitializeReferences()
    {
        if (cachedMainCamera == null)
            cachedMainCamera = Camera.main;

        if (GameEntityRegistry.TryGetPlayerObject(out GameObject player))
            HandlePlayerChanged(player);
        else
            HandlePlayerChanged(null);
    }

    private void CachePlayerComponents(GameObject player)
    {
        cachedPlayer = player;

        machineGunControl = cachedPlayer != null ? cachedPlayer.GetComponent<MachineGunControl>() : null;
        weaponManager = cachedPlayer != null ? cachedPlayer.GetComponent<PlayerWeaponManager>() : null;
        missileLaunch = cachedPlayer != null ? cachedPlayer.GetComponent<MissileLaunch>() : null;
        laserActive = cachedPlayer != null ? cachedPlayer.GetComponent<LaserActive>() : null;
        playerStats = cachedPlayer != null ? cachedPlayer.GetComponent<PlaneStats>() : null;
        SetAutoTargetLock(cachedPlayer != null ? cachedPlayer.GetComponent<AutoTargetLock>() : null);
    }

    private void UpdateCheatUI()
    {
        if (playerStats != null && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.J))
        {
            playerStats.canTakeDamage = !playerStats.canTakeDamage;
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

        autoTargetLock.OnTargetLocked -= OnTargetLocked;
        autoTargetLock.OnTargetLost -= OnTargetLost;
        autoTargetLock.OnTargetLocked += OnTargetLocked;
        autoTargetLock.OnTargetLost += OnTargetLost;
    }

    private void DisconnectFromAutoTargetLock()
    {
        if (autoTargetLock == null) return;

        autoTargetLock.OnTargetLocked -= OnTargetLocked;
        autoTargetLock.OnTargetLost -= OnTargetLost;
    }

    private void OnTargetLocked(Transform target)
    {
        UpdateUI(true);
    }

    private void OnTargetLost(Transform target)
    {
        UpdateUI(false);
    }

    public void SetAutoTargetLock(AutoTargetLock targetLock)
    {
        DisconnectFromAutoTargetLock();

        autoTargetLock = targetLock;

        if (autoTargetLock != null)
        {
            ConnectToAutoTargetLock();
        }
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

    private void HandlePlayerChanged(GameObject player)
    {
        CachePlayerComponents(player);
        cachedEnemyTargets = null;
        enemyTargetCacheTimer = 0f;
        isInitialized = cachedPlayer != null;
    }
}
