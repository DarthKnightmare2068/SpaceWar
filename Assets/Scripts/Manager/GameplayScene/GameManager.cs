using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private struct SideShipSpawnRequest
    {
        public GameObject Prefab;
        public Vector3 Position;
        public EnemyHealthBar HealthBar;

        public SideShipSpawnRequest(GameObject prefab, Vector3 position, EnemyHealthBar healthBar)
        {
            Prefab = prefab;
            Position = position;
            HealthBar = healthBar;
        }
    }

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

    [Header("Performance Settings")]
    [Tooltip("Delay between spawning objects on scene start (in frames)")]
    [SerializeField] private int spawnDelayFrames = 2;

    // Cached references to avoid expensive FindObjectOfType calls
    private Ilumisoft.RadarSystem.Radar[] cachedRadars;
    private bool radarsCached = false;

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
        // Use a more efficient check - only search if we suspect there are leftover players
        // Most of the time this will be empty on fresh scene load
        var existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in existingPlayers)
        {
            Destroy(player);
        }

        SetFPSLock();

        // Spread out expensive instantiation across multiple frames to prevent lag spike
        StartCoroutine(InitializeSceneAsync());
    }

    private IEnumerator InitializeSceneAsync()
    {
        // Wait a frame to let other systems initialize
        yield return null;

        // Spawn boss first
        if (currentBoss == null && bossPrefab != null)
        {
            currentBoss = SpawnBossAtStart();
            yield return new WaitForEndOfFrame();
        }

        // Spawn enemy formation with delays
        if (currentBoss != null)
        {
            StartCoroutine(SpawnEnemyFormationAsync(currentBoss));
            
            // Wait a few frames before spawning player
            for (int i = 0; i < spawnDelayFrames; i++)
            {
                yield return null;
            }

            if (playerPrefab != null)
            {
                currentPlayer = SpawnPlayerAtRespawn(playerBossYDistance);
            }
        }
    }

    private MainBossStats GetMainBossStats() =>
        currentBoss != null ? currentBoss.GetComponent<MainBossStats>() : null;

    private void RegisterSideShip(GameObject ship, EnemyHealthBar healthBar)
    {
        activeEnemyShips.Add(ship);
        EnemyStats stats = ship.GetComponent<EnemyStats>();
        if (stats != null)
        {
            healthBar?.SetTarget(stats);
            GetMainBossStats()?.TrackSideShip(stats);
        }
    }

    private IEnumerator SpawnEnemyFormationAsync(GameObject boss)
    {
        if (boss == null) yield break;

        Quaternion rotation = Quaternion.identity;
        foreach (var request in GetSideShipSpawnRequests(boss))
        {
            RegisterSideShip(Instantiate(request.Prefab, request.Position, rotation), request.HealthBar);
            yield return null;
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

    private void CacheRadars()
    {
        if (!radarsCached)
        {
            cachedRadars = FindObjectsByType<Ilumisoft.RadarSystem.Radar>(FindObjectsSortMode.None);
            radarsCached = true;
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
                if (ship == null) continue;
                // Bolt: Optimized - replaced Vector3.Distance with sqrMagnitude
                float distSqr = (ship.transform.position - playerLastKnownPosition).sqrMagnitude;
                if (distSqr < minDistance)
                {
                    minDistance = distSqr;
                    closestShip = ship;
                }
            }
            
            if (closestShip != null)
            {
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
        int maxTries = 20; // Reduced from 50 - faster startup
        int tries = 0;
        bool insideEnemy = true;
        float minSafeDistance = 150f;
        float minSafeDistanceSqr = minSafeDistance * minSafeDistance;
        
        // Bolt: Optimized - cache colliders and bounds to avoid repeated native property access and GetComponent calls
        struct ColliderInfo
        {
            public Collider col;
            public Bounds bounds;
            public Transform transform;
        }
        List<ColliderInfo> enemyColliderInfos = new List<ColliderInfo>();
        foreach (var ship in activeEnemyShips)
        {
            if (ship != null)
            {
                Collider col = ship.GetComponentInChildren<Collider>();
                if (col != null) enemyColliderInfos.Add(new ColliderInfo { col = col, bounds = col.bounds, transform = col.transform });
            }
        }

        Collider bossCol = (currentBoss != null) ? currentBoss.GetComponentInChildren<Collider>() : null;
        Bounds bossBounds = bossCol != null ? bossCol.bounds : default;
        Transform bossTransform = bossCol != null ? bossCol.transform : null;
        
        do
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * spawnDistance;
            respawnPos = referencePosition + offset;

            Vector3 bossPos = (currentBoss != null) ? currentBoss.transform.position : new Vector3(0, bossMinYSpawn, 0);
            respawnPos.y = Mathf.Max(bossPos.y, bossMinYSpawn) - belowBossYDistance;

            insideEnemy = false;
            
            // Bolt: Optimized - use cached bounds and sqrMagnitude for faster collision/proximity checks
            foreach (var info in enemyColliderInfos)
            {
                if (info.col != null && info.col.gameObject.activeInHierarchy)
                {
                    if (info.bounds.Contains(respawnPos) || (respawnPos - info.transform.position).sqrMagnitude < minSafeDistanceSqr)
                    {
                        insideEnemy = true;
                        break;
                    }
                }
            }
            
            if (!insideEnemy && bossCol != null && bossCol.gameObject.activeInHierarchy)
            {
                if (bossBounds.Contains(respawnPos) || (respawnPos - bossTransform.position).sqrMagnitude < minSafeDistanceSqr)
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
        GameEntityRegistry.RegisterPlayer(player);

        if (uiCamera != null)
            uiCamera.gameObject.SetActive(false);

        // Cache radar references to avoid expensive FindObjectOfType on every spawn
        CacheRadars();
        if (cachedRadars != null)
        {
            foreach (var radar in cachedRadars)
            {
                if (radar != null)
                    radar.SetPlayer(player);
            }
        }
        
        if (AudioSetting.Instance != null)
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
        // Save stats before the player GameObject is destroyed.
        levelUpSystem?.SaveProgress();

        playerLastKnownPosition = player.transform.position;
        
        if (playerExplosionVFX != null && player != null)
        {
            GameObject explosion = Instantiate(playerExplosionVFX, player.transform.position, Quaternion.identity);
            Destroy(explosion, explosionVFXDuration);
        }

        if (uiCamera != null)
            uiCamera.gameObject.SetActive(true);

        GameEntityRegistry.UnregisterPlayer();
        currentPlayer = null;
        ShowDeadScreen();
        if (player != null)
            Destroy(player.gameObject);
        RevivePlayerWithDelay(levelUpSystem != null ? levelUpSystem.CurrentLevel : 1);
    }

    void OnApplicationQuit()
    {
        levelUpSystem?.DeleteSaveFile();
    }

    public void SetFPSLock()
    {
        if (targetFPS <= 0)
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            return;
        }

        // When monitor refresh rate matches the target, vsync gives a hard cap.
        // Otherwise disable vsync and let targetFrameRate soft-limit the frame rate.
        int screenHz = (int)Screen.currentResolution.refreshRateRatio.value;
        if (screenHz > 0 && Mathf.Abs(screenHz - targetFPS) <= 2)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
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
            // Use async version to avoid frame spike
            StartCoroutine(SpawnEnemyFormationAsync(currentBoss));
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

    private IEnumerable<SideShipSpawnRequest> GetSideShipSpawnRequests(GameObject boss)
    {
        if (boss == null)
            yield break;

        Vector3 bossPos = boss.transform.position;
        float escortYPosition = bossPos.y - 500f;

        if (enemyShip1Prefab != null)
        {
            Vector3 spawnPos = bossPos + Vector3.forward * frontDistance;
            spawnPos.y = escortYPosition;
            yield return new SideShipSpawnRequest(enemyShip1Prefab, spawnPos, enemyShip1HealthBar);
        }

        if (enemyShip2Prefab != null)
        {
            Vector3 spawnPos = bossPos + Vector3.left * sideDistance;
            spawnPos.y = escortYPosition;
            yield return new SideShipSpawnRequest(enemyShip2Prefab, spawnPos, enemyShip2HealthBar);
        }

        if (enemyShip3Prefab != null)
        {
            Vector3 spawnPos = bossPos + Vector3.right * sideDistance;
            spawnPos.y = escortYPosition;
            yield return new SideShipSpawnRequest(enemyShip3Prefab, spawnPos, enemyShip3HealthBar);
        }
    }
}
