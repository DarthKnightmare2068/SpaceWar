using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject deadScreen;
    public VideoPlayer deathVideo;
    public GameObject playerPrefab;
    [HideInInspector] public GameObject currentPlayer;
    public GameObject bossPrefab;
    public float bossMinYSpawn = 500f;
    public float playerBossYDistance = 200f;
    [HideInInspector] public GameObject currentBoss;

    [Header("Enemy Formation")]
    public GameObject enemyShip1Prefab;
    public GameObject enemyShip2Prefab;
    public GameObject enemyShip3Prefab;
    [Header("Enemy Formation Distances")]
    public float frontDistance = 500f;
    public float sideDistance = 800f;
    private List<GameObject> activeEnemyShips = new List<GameObject>();
    private Vector3 playerLastKnownPosition = Vector3.zero;

    public float playerBossMinDistance = 100f;
    public GameObject groundPrefab;

    [Header("UI Settings")]
    public EnemyHealthBar mainBossHealthBar;
    public EnemyHealthBar enemyShip1HealthBar;
    public EnemyHealthBar enemyShip2HealthBar;
    public EnemyHealthBar enemyShip3HealthBar;
    [Tooltip("The dedicated camera for rendering UI elements. Should be persistent in the scene.")]
    public Camera uiCamera;

    [Header("Death Effects")]
    [Tooltip("Explosion VFX prefab to spawn when the player dies")]
    public GameObject playerExplosionVFX;
    [Tooltip("Duration of the explosion VFX before destroying it")]
    public float explosionVFXDuration = 2f;

    public ReviveCD reviveCD;
    public LevelUpSystem levelUpSystem;

    [Header("Performance Settings")]
    [Tooltip("Target FPS for the game. Set to 0 to disable FPS lock")]
    public int targetFPS = 60;

    [Header("Audio Settings")]
    [Tooltip("Sound to play when any enemy ship (including boss) is destroyed")]
    public AudioClip enemyDestroyedClip;
    [Tooltip("Volume for enemy destroyed sound")]
    [Range(0f, 1f)] public float enemyDestroyedVolume = 1f;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (deadScreen != null)
            deadScreen.SetActive(false);
        if (deathVideo != null)
            deathVideo.Stop();

        if (uiCamera != null)
            uiCamera.gameObject.SetActive(false);

        if (levelUpSystem == null)
            levelUpSystem = GetComponent<LevelUpSystem>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        var existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in existingPlayers)
        {
            Destroy(player);
        }

        SetFPSLock();

        if (currentBoss == null && bossPrefab != null)
        {
            currentBoss = SpawnBossAtStart();
            SpawnEnemyFormation(currentBoss);
        }
        if (playerPrefab != null && currentBoss != null)
        {
            currentPlayer = SpawnPlayerAtRespawn(playerBossYDistance);
        }
    }

    public void ShowDeadScreen()
    {
        if (deadScreen != null)
            deadScreen.SetActive(true);
        if (deathVideo != null)
            deathVideo.Play();
    }

    public void HideDeadScreen()
    {
        if (deadScreen != null)
            deadScreen.SetActive(false);
        if (deathVideo != null)
            deathVideo.Stop();
    }

    public GameObject SpawnBossAtStart()
    {
        Vector3 bossSpawnPos = new Vector3(0, bossMinYSpawn, 0);
        Quaternion bossRot = Quaternion.Euler(0, 0, 0);
        GameObject boss = Instantiate(bossPrefab, bossSpawnPos, bossRot);
        currentBoss = boss;

        if (mainBossHealthBar != null)
        {
            var mainBossStats = boss.GetComponent<MainBossStats>();
            if (mainBossStats != null)
                mainBossHealthBar.SetTarget(mainBossStats);
            else
                mainBossHealthBar.SetTarget(boss.GetComponent<EnemyStats>());
        }

        return boss;
    }

    public void SpawnEnemyFormation(GameObject boss)
    {
        if (boss == null) return;

        Vector3 bossPos = boss.transform.position;
        Quaternion rotation = Quaternion.identity;
        float escortYPosition = bossPos.y - 500f;

        if (enemyShip1Prefab != null)
        {
            Vector3 spawnPos1 = bossPos + Vector3.forward * frontDistance;
            spawnPos1.y = escortYPosition;
            GameObject ship1 = Instantiate(enemyShip1Prefab, spawnPos1, rotation);
            activeEnemyShips.Add(ship1);
            if (enemyShip1HealthBar != null)
            {
                EnemyStats ship1Stats = ship1.GetComponent<EnemyStats>();
                enemyShip1HealthBar.SetTarget(ship1Stats);
            }
        }

        if (enemyShip2Prefab != null)
        {
            Vector3 spawnPos2 = bossPos + Vector3.left * sideDistance;
            spawnPos2.y = escortYPosition;
            GameObject ship2 = Instantiate(enemyShip2Prefab, spawnPos2, rotation);
            activeEnemyShips.Add(ship2);
            if (enemyShip2HealthBar != null)
            {
                EnemyStats ship2Stats = ship2.GetComponent<EnemyStats>();
                enemyShip2HealthBar.SetTarget(ship2Stats);
            }
        }

        if (enemyShip3Prefab != null)
        {
            Vector3 spawnPos3 = bossPos + Vector3.right * sideDistance;
            spawnPos3.y = escortYPosition;
            GameObject ship3 = Instantiate(enemyShip3Prefab, spawnPos3, rotation);
            activeEnemyShips.Add(ship3);
            if (enemyShip3HealthBar != null)
            {
                EnemyStats ship3Stats = ship3.GetComponent<EnemyStats>();
                enemyShip3HealthBar.SetTarget(ship3Stats);
            }
        }
    }

    public Vector3 GetRespawnPosition(float belowBossYDistance)
    {
        activeEnemyShips.RemoveAll(ship => ship == null);

        Vector3 referencePosition;
        float spawnDistance;

        if (activeEnemyShips.Count > 0)
        {
            GameObject closestShip = null;
            float minDistance = float.MaxValue;

            foreach (var ship in activeEnemyShips)
            {
                float dist = Vector3.Distance(ship.transform.position, playerLastKnownPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestShip = ship;
                }
            }
            
            referencePosition = closestShip.transform.position;
            spawnDistance = 2000f;
        }
        else if (currentBoss != null)
        {
            referencePosition = currentBoss.transform.position;
            spawnDistance = playerBossMinDistance;
        }
        else
        {
            return new Vector3(0, bossMinYSpawn - belowBossYDistance, 0);
        }
        
        Vector3 respawnPos;
        int maxTries = 50;
        int tries = 0;
        bool insideEnemy = true;
        float minSafeDistance = 150f;
        do
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * spawnDistance;
            respawnPos = referencePosition + offset;

            Vector3 bossPos = (currentBoss != null) ? currentBoss.transform.position : new Vector3(0, bossMinYSpawn, 0);
            respawnPos.y = Mathf.Max(bossPos.y, bossMinYSpawn) - belowBossYDistance;

            insideEnemy = false;
            foreach (var ship in activeEnemyShips)
            {
                Collider col = ship.GetComponentInChildren<Collider>();
                if (col != null && (col.bounds.Contains(respawnPos) || Vector3.Distance(respawnPos, ship.transform.position) < minSafeDistance))
                {
                    insideEnemy = true;
                    break;
                }
            }
            if (!insideEnemy && currentBoss != null)
            {
                Collider bossCol = currentBoss.GetComponentInChildren<Collider>();
                if (bossCol != null && (bossCol.bounds.Contains(respawnPos) || Vector3.Distance(respawnPos, currentBoss.transform.position) < minSafeDistance))
                {
                    insideEnemy = true;
                }
            }
            tries++;
        } while (insideEnemy && tries < maxTries);

        return respawnPos;
    }

    public GameObject SpawnPlayerAtRespawn(float belowBossYDistance)
    {
        if (playerPrefab == null)
        {
            return null;
        }
        Vector3 spawnPos = GetRespawnPosition(belowBossYDistance);
        Quaternion playerRot = Quaternion.Euler(0, 0, 0);
        GameObject player = Instantiate(playerPrefab, spawnPos, playerRot);
        currentPlayer = player;
        
        if (uiCamera != null)
            uiCamera.gameObject.SetActive(false);

        foreach (var radar in FindObjectsOfType<Ilumisoft.RadarSystem.Radar>())
        {
            radar.SetPlayer(player);
        }
        
        AudioSetting.Instance.PlayRespawnSoundForPlayer(player);
        
        if (HudLiteScript.current != null)
            HudLiteScript.current.SetAircraft(player);
        
        return player;
    }

    public void RevivePlayerWithDelay(int playerLevel)
    {
        StartCoroutine(RevivePlayerCoroutine(playerLevel));
    }

    private IEnumerator RevivePlayerCoroutine(int playerLevel)
    {
        float reviveTime = 10f + playerLevel;
        for (int i = (int)reviveTime; i > 0; i--)
        {
            if (reviveCD != null)
                reviveCD.SetCountdown(i);
            yield return new WaitForSeconds(1f);
        }
        if (reviveCD != null)
            reviveCD.ShowRevived();

        HideDeadScreen();
        SpawnPlayerAtRespawn(playerBossYDistance);
        if (reviveCD != null)
            reviveCD.Clear();
    }

    public void OnPlayerDeath(PlaneStats player)
    {
        playerLastKnownPosition = player.transform.position;
        
        if (playerExplosionVFX != null && player != null)
        {
            GameObject explosion = Instantiate(playerExplosionVFX, player.transform.position, Quaternion.identity);
            Destroy(explosion, explosionVFXDuration);
        }

        if (uiCamera != null)
            uiCamera.gameObject.SetActive(true);

        ShowDeadScreen();
        if (player != null)
            Destroy(player.gameObject);
        RevivePlayerWithDelay(levelUpSystem != null ? levelUpSystem.CurrentLevel : 1);
    }

    public void SetFPSLock()
    {
        if (targetFPS <= 0)
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = 0;
        }
    }

    public void ChangeFPSLock(int newTargetFPS)
    {
        targetFPS = newTargetFPS;
        SetFPSLock();
    }

    public List<GameObject> GetActiveEnemyShips()
    {
        return activeEnemyShips;
    }

    public void RespawnEnemySideShips()
    {
        foreach (var ship in new List<GameObject>(activeEnemyShips))
        {
            if (ship != null)
                Destroy(ship);
        }
        activeEnemyShips.Clear();
        if (currentBoss != null)
        {
            SpawnEnemyFormation(currentBoss);
        }
    }

    public void PlayEnemyDestroyedSound()
    {
        if (enemyDestroyedClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(enemyDestroyedClip, enemyDestroyedVolume);
        }
    }

    public float GetCurrentFPS()
    {
        return 1f / Time.unscaledDeltaTime;
    }

    public string GetCurrentFPSString()
    {
        float fps = GetCurrentFPS();
        return Mathf.Round(fps).ToString();
    }
}
