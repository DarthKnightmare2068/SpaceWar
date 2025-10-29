using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI; // Add this for UI components
// using global::HudLiteScript;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    public GameObject deadScreen;
    public VideoPlayer deathVideo;
    public GameObject playerPrefab; // Assign in Inspector
    [HideInInspector] public GameObject currentPlayer; // Reference to the currently spawned player
    public GameObject bossPrefab; // Assign in Inspector
    public float bossMinYSpawn = 500f; // Minimum Y for boss spawn
    public float playerBossYDistance = 200f; // Player always spawns this much below boss
    [HideInInspector] public GameObject currentBoss; // Reference to the currently spawned boss

    [Header("Enemy Formation")]
    public GameObject enemyShip1Prefab;
    public GameObject enemyShip2Prefab;
    public GameObject enemyShip3Prefab;
    [Header("Enemy Formation Distances")]
    public float frontDistance = 500f;
    public float sideDistance = 800f;
    private List<GameObject> activeEnemyShips = new List<GameObject>();
    private Vector3 playerLastKnownPosition = Vector3.zero;

    public float playerBossMinDistance = 100f; // Minimum horizontal distance from boss when respawning
    public GameObject groundPrefab; // Ground reference for boundary checking

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

    public ReviveCD reviveCD; // Assign in Inspector
    public LevelUpSystem levelUpSystem; // Assign in Inspector or in Awake

    [Header("Performance Settings")]
    [Tooltip("Target FPS for the game. Set to 0 to disable FPS lock")]
    public int targetFPS = 60;

    [Header("Audio Settings")]
    [Tooltip("Sound to play when any enemy ship (including boss) is destroyed")]
    public AudioClip enemyDestroyedClip;
    [Tooltip("Volume for enemy destroyed sound")]
    [Range(0f, 1f)] public float enemyDestroyedVolume = 1f;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Ensure DeadScreen is off at start
        if (deadScreen != null)
            deadScreen.SetActive(false);
        if (deathVideo != null)
            deathVideo.Stop();

        // Ensure the UI camera is disabled at launch to prioritize the player's camera
        if (uiCamera != null)
            uiCamera.gameObject.SetActive(false);

        // Auto-assign LevelUpSystem if not set
        if (levelUpSystem == null)
            levelUpSystem = GetComponent<LevelUpSystem>();

        // Setup audio source for enemy destroyed sound
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        // Destroy any existing player objects
        var existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in existingPlayers)
        {
            Destroy(player);
        }

        // Set FPS lock
        SetFPSLock();

        // Spawn the boss at the start of the game
        if (currentBoss == null && bossPrefab != null)
        {
            currentBoss = SpawnBossAtStart();
            SpawnEnemyFormation(currentBoss);
        }
        // Always spawn the player at the start of the game, using the boss's position
        if (playerPrefab != null && currentBoss != null)
        {
            currentPlayer = SpawnPlayerAtRespawn(playerBossYDistance);
        }
    }

    public void ShowDeadScreen()
    {
        Debug.Log("Player is dead from GameManager");
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

        // Link the boss to its health bar
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

        // The Y-position for all escort ships will be 500 units below the boss.
        float escortYPosition = bossPos.y - 500f;

        // Spawn Ship 1 (Front)
        if (enemyShip1Prefab != null)
        {
            Vector3 spawnPos1 = bossPos + Vector3.forward * frontDistance;
            spawnPos1.y = escortYPosition; // Set the correct Y-position
            GameObject ship1 = Instantiate(enemyShip1Prefab, spawnPos1, rotation);
            activeEnemyShips.Add(ship1);
            if (enemyShip1HealthBar != null)
            {
                EnemyStats ship1Stats = ship1.GetComponent<EnemyStats>();
                Debug.Log($"[GameManager] Ship 1 spawned: {ship1.name}, EnemyStats found: {ship1Stats != null}");
                enemyShip1HealthBar.SetTarget(ship1Stats);
            }
            else
            {
                Debug.LogWarning("[GameManager] enemyShip1HealthBar is null!");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] enemyShip1Prefab is null!");
        }

        // Spawn Ship 2 (Left)
        if (enemyShip2Prefab != null)
        {
            Vector3 spawnPos2 = bossPos + Vector3.left * sideDistance;
            spawnPos2.y = escortYPosition; // Set the correct Y-position
            GameObject ship2 = Instantiate(enemyShip2Prefab, spawnPos2, rotation);
            activeEnemyShips.Add(ship2);
            if (enemyShip2HealthBar != null)
            {
                EnemyStats ship2Stats = ship2.GetComponent<EnemyStats>();
                Debug.Log($"[GameManager] Ship 2 spawned: {ship2.name}, EnemyStats found: {ship2Stats != null}");
                enemyShip2HealthBar.SetTarget(ship2Stats);
            }
            else
            {
                Debug.LogWarning("[GameManager] enemyShip2HealthBar is null!");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] enemyShip2Prefab is null!");
        }

        // Spawn Ship 3 (Right)
        if (enemyShip3Prefab != null)
        {
            Vector3 spawnPos3 = bossPos + Vector3.right * sideDistance;
            spawnPos3.y = escortYPosition; // Set the correct Y-position
            GameObject ship3 = Instantiate(enemyShip3Prefab, spawnPos3, rotation);
            activeEnemyShips.Add(ship3);
            if (enemyShip3HealthBar != null)
            {
                EnemyStats ship3Stats = ship3.GetComponent<EnemyStats>();
                Debug.Log($"[GameManager] Ship 3 spawned: {ship3.name}, EnemyStats found: {ship3Stats != null}");
                enemyShip3HealthBar.SetTarget(ship3Stats);
            }
            else
            {
                Debug.LogWarning("[GameManager] enemyShip3HealthBar is null!");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] enemyShip3Prefab is null!");
        }
    }

    public Vector3 GetRespawnPosition(float belowBossYDistance)
    {
        // Clean the list of any destroyed ships before using it
        activeEnemyShips.RemoveAll(ship => ship == null);

        Vector3 referencePosition;
        float spawnDistance;

        // PRIORITIZE ENEMY SHIPS
        if (activeEnemyShips.Count > 0)
        {
            // Find the escort ship closest to where the player died
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
            spawnDistance = 2000f; // Use the new, larger distance
            Debug.Log($"[GameManager] Respawning relative to closest enemy ship: {closestShip.name}");
        }
        // FALLBACK TO MAIN BOSS
        else if (currentBoss != null)
        {
            referencePosition = currentBoss.transform.position;
            spawnDistance = playerBossMinDistance; // Use the original boss distance
            Debug.Log("[GameManager] No enemy ships left. Respawning relative to Main Boss.");
        }
        // FALLBACK TO DEFAULT
        else
        {
            Debug.Log("[GameManager] No enemies found. Respawning at default location.");
            return new Vector3(0, bossMinYSpawn - belowBossYDistance, 0);
        }
        
        // Pick a random horizontal direction (XZ plane) away from the reference position
        Vector3 respawnPos;
        int maxTries = 50;
        int tries = 0;
        bool insideEnemy = true;
        float minSafeDistance = 150f; // Adjust as needed
        do
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * spawnDistance;
            respawnPos = referencePosition + offset;

            // Set Y position relative to the main boss's height, to ensure player is always below the action
            Vector3 bossPos = (currentBoss != null) ? currentBoss.transform.position : new Vector3(0, bossMinYSpawn, 0);
            respawnPos.y = Mathf.Max(bossPos.y, bossMinYSpawn) - belowBossYDistance;

            // Check if inside any enemy ship's bounds or too close to any enemy
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
            // Also check boss
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

        if (insideEnemy)
        {
            Debug.LogWarning("[GameManager] Could not find a respawn position outside enemy ships after many tries. Using last tried position.");
        }

        return respawnPos;
    }

    public GameObject SpawnPlayerAtRespawn(float belowBossYDistance)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned in GameManager!");
            return null;
        }
        Vector3 spawnPos = GetRespawnPosition(belowBossYDistance);
        Quaternion playerRot = Quaternion.Euler(0, 0, 0);
        GameObject player = Instantiate(playerPrefab, spawnPos, playerRot);
        currentPlayer = player;
        
        // A new player with a main camera has spawned, so disable the UI-only camera.
        if (uiCamera != null)
            uiCamera.gameObject.SetActive(false);

        // Set Radar Player reference
        foreach (var radar in FindObjectsOfType<Ilumisoft.RadarSystem.Radar>())
        {
            radar.SetPlayer(player);
        }
        
        // Play respawn sound for the newly spawned player
        AudioSetting.Instance.PlayRespawnSoundForPlayer(player);
        
        // Auto-assign HUD aircraft and rigidbody
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
        Debug.Log($"Player death handled by GameManager for {player.name}");
        playerLastKnownPosition = player.transform.position;
        
        // Spawn explosion VFX at player's position
        if (playerExplosionVFX != null && player != null)
        {
            GameObject explosion = Instantiate(playerExplosionVFX, player.transform.position, Quaternion.identity);
            Destroy(explosion, explosionVFXDuration);
        }

        // The player's camera is about to be destroyed, so enable the UI camera to render the death screen.
        if (uiCamera != null)
            uiCamera.gameObject.SetActive(true);

        ShowDeadScreen();
        if (player != null)
            Destroy(player.gameObject);
        RevivePlayerWithDelay(levelUpSystem != null ? levelUpSystem.CurrentLevel : 1);
        // Add any additional global death logic here
    }

    /// <summary>
    /// Sets the target FPS for the game
    /// </summary>
    public void SetFPSLock()
    {
        if (targetFPS <= 0)
        {
            // Disable FPS lock
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            Debug.Log("[GameManager] FPS lock disabled");
        }
        else
        {
            // Enable FPS lock
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = 0; // Disable VSync to use our custom FPS lock
            Debug.Log($"[GameManager] FPS locked to {targetFPS}");
        }
    }

    /// <summary>
    /// Dynamically change FPS lock during runtime
    /// </summary>
    /// <param name="newTargetFPS">New target FPS. Set to 0 to disable</param>
    public void ChangeFPSLock(int newTargetFPS)
    {
        targetFPS = newTargetFPS;
        SetFPSLock();
    }

    // Add this method to allow other scripts to access the list of active enemy ships
    public List<GameObject> GetActiveEnemyShips()
    {
        return activeEnemyShips;
    }

    /// <summary>
    /// Respawn all enemy side ships (escort ships) at their original formation positions.
    /// Destroys any existing side ships first.
    /// </summary>
    public void RespawnEnemySideShips()
    {
        // Destroy all current side ships
        foreach (var ship in new List<GameObject>(activeEnemyShips))
        {
            if (ship != null)
                Destroy(ship);
        }
        activeEnemyShips.Clear();
        // Respawn the formation using the current boss
        if (currentBoss != null)
        {
            SpawnEnemyFormation(currentBoss);
            Debug.Log("[GameManager] Respawned all enemy side ships.");
        }
        else
        {
            Debug.LogWarning("[GameManager] Cannot respawn side ships: currentBoss is null!");
        }
    }

    public void PlayEnemyDestroyedSound()
    {
        if (enemyDestroyedClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(enemyDestroyedClip, enemyDestroyedVolume);
        }
    }

    // Add this method to get current FPS for UI display
    public float GetCurrentFPS()
    {
        return 1f / Time.unscaledDeltaTime;
    }

    // Add this method to get FPS as a formatted string
    public string GetCurrentFPSString()
    {
        float fps = GetCurrentFPS();
        return Mathf.Round(fps).ToString();
    }

    // Update is called every frame
    void Update()
    {
        
    }
}
