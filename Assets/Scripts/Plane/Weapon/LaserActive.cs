using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class LaserActive : MonoBehaviour
{
    [Header("Laser Settings")]
    public float laserFireRange = 100f;
    public int laserDamage = 111;
    public float fireTickInterval = 0.1f;
    public LayerMask shootableLayers;
    public float laserCooldown = 2f;

    public int maxThreshold = 5;
    public int currentThreshold = 5;
    public int maxLevel = LevelUpSystem.MAX_LEVEL;
    private bool mustRechargeFull = false;
    private float fireTickAccumulator = 0f;
    private float rechargeAccumulator = 0f;

    [Header("References")]
    public GameObject laserVFXPrefab;
    public VisualEffect laserVisualScript;
    public PlayerWeaponManager weaponManager;
    public GameObject explosionVFXPrefab;

    public float CurrentBeamLength { get; private set; }

    private PlaneStats playerPlane;
    private float tickTimer = 0f;
    private bool isFiring = false;
    private GameObject activeLaserInstance;
    private VisualEffect activeVFX;
    private AudioSource laserAudioSource;
    private bool audioSourceInitialized = false;

    void Start()
    {
        CurrentBeamLength = laserFireRange;

        if (laserVisualScript != null)
            laserVisualScript.gameObject.SetActive(false);

        if (weaponManager == null)
            weaponManager = GetComponent<PlayerWeaponManager>();
        if (weaponManager == null)
            weaponManager = GetComponentInParent<PlayerWeaponManager>();

        if (shootableLayers == 0)
        {
            shootableLayers = ~LayerMask.GetMask("Player");
        }

        maxThreshold = 5;
        currentThreshold = maxThreshold;
        
        FindPlayerStats();
        InitializeAudioSource();
    }

    private void InitializeAudioSource()
    {
        if (audioSourceInitialized) return;
        
        if (laserAudioSource == null)
        {
            GameObject audioObj = new GameObject("LaserAudio");
            audioObj.transform.SetParent(transform);
            audioObj.transform.localPosition = Vector3.zero;
            laserAudioSource = audioObj.AddComponent<AudioSource>();
            laserAudioSource.loop = true;
            laserAudioSource.playOnAwake = false;
            laserAudioSource.spatialBlend = 0f;
            
            if (AudioSetting.Instance != null)
            {
                laserAudioSource.clip = AudioSetting.Instance.laserSound;
                laserAudioSource.volume = AudioSetting.Instance.laserSFXVolume;
            }
        }
        
        audioSourceInitialized = true;
    }

    void OnDestroy()
    {
        if (laserAudioSource != null)
        {
            laserAudioSource.Stop();
        }
    }

    void Update()
    {
        if (playerPlane == null)
        {
            FindPlayerStats();
        }
        
        if (!isFiring && currentThreshold < maxThreshold)
        {
            rechargeAccumulator += Time.deltaTime;
            if (rechargeAccumulator >= 1f)
            {
                currentThreshold = Mathf.Min(currentThreshold + 1, maxThreshold);
                rechargeAccumulator = 0f;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
            StartFiring();
        if (Input.GetKeyUp(KeyCode.E))
            StopFiring();

        if (isFiring && currentThreshold > 0 && !mustRechargeFull)
        {
            fireTickAccumulator += Time.deltaTime;
            tickTimer += Time.deltaTime;
            if (fireTickAccumulator >= 1f)
            {
                currentThreshold = Mathf.Max(currentThreshold - 1, 0);
                fireTickAccumulator = 0f;
                if (currentThreshold == 0)
                {
                    mustRechargeFull = true;
                    StopFiring();
                }
            }
            if (tickTimer >= fireTickInterval)
            {
                tickTimer = 0f;
                FireLaser();
            }
        }
        if (mustRechargeFull && currentThreshold == maxThreshold)
        {
            mustRechargeFull = false;
        }
    }

    private void FindPlayerStats()
    {
        playerPlane = GetComponent<PlaneStats>();
        if (playerPlane == null)
            playerPlane = GetComponentInParent<PlaneStats>();
        if (playerPlane == null)
            GameEntityRegistry.TryGetPlayerComponent(out playerPlane);
    }

    void StartFiring()
    {
        if (mustRechargeFull || currentThreshold == 0) return;

        if (weaponManager != null && !weaponManager.IsTargetInRange(laserFireRange))
        {
            return;
        }

        isFiring = true;
        if (laserVFXPrefab != null && activeLaserInstance == null)
        {
            activeLaserInstance = Instantiate(laserVFXPrefab, transform.position, transform.rotation, transform);
            activeVFX = activeLaserInstance.GetComponent<VisualEffect>();
        }
        if (laserVisualScript != null)
            laserVisualScript.gameObject.SetActive(true);

        PlayLaserSound();
    }

    private void PlayLaserSound()
    {
        if (!audioSourceInitialized)
        {
            InitializeAudioSource();
        }
        
        if (laserAudioSource != null)
        {
            if (AudioSetting.Instance != null)
            {
                laserAudioSource.clip = AudioSetting.Instance.laserSound;
                laserAudioSource.volume = AudioSetting.Instance.laserSFXVolume;
            }
            
            if (!laserAudioSource.isPlaying)
            {
                laserAudioSource.Play();
            }
        }
    }

    private void StopLaserSound()
    {
        if (laserAudioSource != null && laserAudioSource.isPlaying)
        {
            laserAudioSource.Stop();
        }
    }

    void StopFiring()
    {
        if (!isFiring) return;
        isFiring = false;
        if (activeLaserInstance != null)
        {
            Destroy(activeLaserInstance);
            activeLaserInstance = null;
            activeVFX = null;
        }
        if (laserVisualScript != null)
            laserVisualScript.gameObject.SetActive(false);
        CurrentBeamLength = laserFireRange;
        fireTickAccumulator = 0f;
        rechargeAccumulator = 0f;

        StopLaserSound();
    }

    public void OnPlayerLevelUp()
    {
        laserDamage += 111;
        if (maxThreshold < maxLevel)
        {
            maxThreshold++;
            currentThreshold = maxThreshold;
        }
    }

    public void FireLaser()
    {
        if (weaponManager == null) return;

        Ray ray = weaponManager.GetCurrentTargetRay();
        RaycastHit hit;
        float calculatedRange = laserFireRange;

        if (Physics.Raycast(ray, out hit, laserFireRange, shootableLayers))
        {
            calculatedRange = hit.distance;

            if (playerPlane != null)
            {
                float finalDamage = laserDamage + playerPlane.attackPoint;
                DamageHelper.TryDealDamage(hit, finalDamage, Color.blue);
            }

            if (explosionVFXPrefab != null)
            {
                GameObject explosion = Instantiate(explosionVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(explosion, 1f);
            }
        }

        CurrentBeamLength = calculatedRange;

        if (activeVFX != null)
        {
            Vector3 beamScale = activeVFX.GetVector3("BeamsScale");
            beamScale.y = CurrentBeamLength;
            activeVFX.SetVector3("BeamsScale", beamScale);
        }
        if (laserVisualScript != null)
        {
            Vector3 beamScale = laserVisualScript.GetVector3("BeamsScale");
            beamScale.y = CurrentBeamLength;
            laserVisualScript.SetVector3("BeamsScale", beamScale);
        }
    }
}
