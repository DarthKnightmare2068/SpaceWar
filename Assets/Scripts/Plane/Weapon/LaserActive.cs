using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX; // Re-add this for VisualEffect control

public class LaserActive : MonoBehaviour
{
    [Header("Laser Settings")]
    public float laserFireRange = 100f; // Range for damage dealing
    public int laserDamage = 111;
    public float fireTickInterval = 0.1f; // How often to apply damage while firing
    public LayerMask shootableLayers; // Layers the laser can hit for damage
    public float laserCooldown = 2f; // This will be dynamically calculated

    // Threshold/Energy system
    public int maxThreshold = 5;
    public int currentThreshold = 5;
    public int maxLevel = LevelUpSystem.MAX_LEVEL;
    private bool mustRechargeFull = false;
    private float fireTickAccumulator = 0f;
    private float rechargeAccumulator = 0f;

    [Header("References")]
    public GameObject laserVFXPrefab; // The prefab to spawn
    public VisualEffect laserVisualScript; // The beam object containing the VisualEffect script
    public PlayerWeaponManager weaponManager; // Reference to the weapon manager
    public GameObject explosionVFXPrefab; // Explosion VFX to play at hit point

    // Public property for other scripts to read the beam's current visual length
    public float CurrentBeamLength { get; private set; }

    private PlaneStats playerPlane;
    private float tickTimer = 0f;
    private bool isFiring = false;
    private GameObject activeLaserInstance; // To keep track of the spawned laser
    private VisualEffect activeVFX; // Reference to the VFX component on the instance
    private AudioSource loopingAudioSource; // For the looping laser sound

    // Start is called once per frame
    void Start()
    {
        CurrentBeamLength = laserFireRange; // Initialize to max range

        // Ensure the VFX is off at game start
        if (laserVisualScript != null)
            laserVisualScript.gameObject.SetActive(false);

        // Find the PlayerWeaponManager if not assigned
        if (weaponManager == null)
            weaponManager = FindObjectOfType<PlayerWeaponManager>();

        if (weaponManager == null)
            Debug.LogError("[LaserActive] PlayerWeaponManager not found in scene!");

        // Default shootableLayers to everything except the "Player" layer
        if (shootableLayers == 0)
        {
            shootableLayers = ~LayerMask.GetMask("Player");
        }

        // Initialize threshold
        maxThreshold = 5;
        currentThreshold = maxThreshold;
        
        // Try to find player stats - will be updated in Update if not found
        FindPlayerStats();
    }

    // Update is called once per frame
    void Update()
    {
        // Try to find player stats if not found
        if (playerPlane == null)
        {
            FindPlayerStats();
        }
        
        // Recharge threshold when not firing and not at max
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

        // Only allow firing if not forced to recharge full
        if (isFiring && currentThreshold > 0 && !mustRechargeFull)
        {
            fireTickAccumulator += Time.deltaTime;
            tickTimer += Time.deltaTime;
            // Consume threshold every 1 second
            if (fireTickAccumulator >= 1f)
            {
                currentThreshold = Mathf.Max(currentThreshold - 1, 0);
                fireTickAccumulator = 0f;
                if (currentThreshold == 0)
                {
                    // Out of energy, must recharge to full before firing again
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
        // If fully recharged, allow firing again
        if (mustRechargeFull && currentThreshold == maxThreshold)
        {
            mustRechargeFull = false;
        }
    }

    private void FindPlayerStats()
    {
        // Try multiple ways to find player stats
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerPlane = GameManager.Instance.currentPlayer.GetComponent<PlaneStats>();
        }
        
        if (playerPlane == null)
        {
            // Fallback: find any PlaneStats in scene
            playerPlane = FindObjectOfType<PlaneStats>();
        }
        
        if (playerPlane == null)
        {
            Debug.LogWarning("[LaserActive] Could not find playerPlane for damage calculation.");
        }
    }

    void StartFiring()
    {
        if (mustRechargeFull || currentThreshold == 0) return;

        // Check if a target is in range before firing
        if (weaponManager != null && !weaponManager.IsTargetInRange(laserFireRange))
        {
            return; // Not in range, do not fire
        }

        isFiring = true;
        if (laserVFXPrefab != null && activeLaserInstance == null)
        {
            // Instantiate the laser and parent it to this fire point
            activeLaserInstance = Instantiate(laserVFXPrefab, transform.position, transform.rotation, transform);
            activeVFX = activeLaserInstance.GetComponent<VisualEffect>(); // Get the VFX component
        }
        // If you want to use a pre-existing VisualEffect (not from the prefab), assign it to laserVisualScript in the inspector
        if (laserVisualScript != null)
            laserVisualScript.gameObject.SetActive(true);

        // Play sound effect
        if (AudioSetting.Instance != null && AudioSetting.Instance.laserSound != null && loopingAudioSource == null)
        {
            GameObject audioObj = new GameObject("LaserAudio");
            audioObj.transform.SetParent(transform);
            loopingAudioSource = audioObj.AddComponent<AudioSource>();
            loopingAudioSource.clip = AudioSetting.Instance.laserSound;
            loopingAudioSource.volume = AudioSetting.Instance.laserSFXVolume;
            loopingAudioSource.loop = true;
            loopingAudioSource.Play();
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
            activeVFX = null; // Clear the reference
        }
        if (laserVisualScript != null)
            laserVisualScript.gameObject.SetActive(false);
        CurrentBeamLength = laserFireRange; // Reset on stop
        fireTickAccumulator = 0f;
        rechargeAccumulator = 0f;

        // Stop and destroy the looping audio source
        if (loopingAudioSource != null)
        {
            Destroy(loopingAudioSource.gameObject);
            loopingAudioSource = null;
        }
    }

    // Call this method when the player levels up
    public void OnPlayerLevelUp()
    {
        laserDamage += 111;
        if (maxThreshold < maxLevel)
        {
            maxThreshold++;
            currentThreshold = maxThreshold;
        }
    }

    // Fires the laser, applies damage if an enemy is hit within range from screen center
    public void FireLaser()
    {
        if (weaponManager == null) return;

        Ray ray = weaponManager.GetCurrentTargetRay();
        RaycastHit hit;
        float calculatedRange = laserFireRange;

        if (Physics.Raycast(ray, out hit, laserFireRange, shootableLayers))
        {
            calculatedRange = hit.distance;

            // Prioritize Turret first
            if (hit.collider.CompareTag("Turret"))
            {
                // Check for all weapon types since all weapons use "Turret" tag
                var turret = hit.collider.GetComponentInParent<TurretControl>();
                var smallCanon = hit.collider.GetComponentInParent<SmallCanonControl>();
                var bigCanon = hit.collider.GetComponentInParent<BigCanon>();
                
                if (turret != null && playerPlane != null)
                {
                    turret.TakeDamage((int)(laserDamage + playerPlane.attackPoint));
                    DmgPopUp.ShowLaserDamage(hit.point, (int)(laserDamage + playerPlane.attackPoint));
                    Debug.Log($"[LaserActive] Damaged TurretControl: {turret.name}");
                }
                else if (smallCanon != null && playerPlane != null)
                {
                    smallCanon.TakeDamage((int)(laserDamage + playerPlane.attackPoint));
                    DmgPopUp.ShowLaserDamage(hit.point, (int)(laserDamage + playerPlane.attackPoint));
                    Debug.Log($"[LaserActive] Damaged SmallCanonControl: {smallCanon.name}");
                }
                else if (bigCanon != null && playerPlane != null)
                {
                    bigCanon.TakeDamage((int)(laserDamage + playerPlane.attackPoint));
                    DmgPopUp.ShowLaserDamage(hit.point, (int)(laserDamage + playerPlane.attackPoint));
                    Debug.Log($"[LaserActive] Damaged BigCanon: {bigCanon.name}");
                }
                else
                {
                    Debug.LogWarning($"[LaserActive] Hit object with 'Turret' tag but no weapon component found: {hit.collider.name}");
                }
            }
            // Then Enemy
            else if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log($"[LaserActive] Laser hit enemy: {hit.collider.name}");
                // Check for both regular enemies and main boss
                var enemy = hit.collider.GetComponentInParent<EnemyStats>();
                var mainBoss = hit.collider.GetComponentInParent<MainBossStats>();
                
                if (enemy != null && playerPlane != null)
                {
                    enemy.TakeDamage(laserDamage + playerPlane.attackPoint);
                    DmgPopUp.ShowLaserDamage(hit.point, (int)(laserDamage + playerPlane.attackPoint));
                    Debug.Log($"[LaserActive] Damaged EnemyStats: {enemy.name}");
                }
                else if (mainBoss != null && playerPlane != null)
                {
                    mainBoss.TakeDamage(laserDamage + playerPlane.attackPoint);
                    DmgPopUp.ShowLaserDamage(hit.point, (int)(laserDamage + playerPlane.attackPoint));
                    Debug.Log($"[LaserActive] Damaged MainBossStats: {mainBoss.name}");
                }
            }
            // Play explosion VFX at hit point for any hit
            if (explosionVFXPrefab != null)
            {
                GameObject explosion = Instantiate(explosionVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(explosion, 1f);
            }
        }

        CurrentBeamLength = calculatedRange; // Update the public property

        // Update the beam length on the correct VisualEffect
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
