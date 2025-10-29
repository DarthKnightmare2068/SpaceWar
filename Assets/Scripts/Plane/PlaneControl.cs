using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlaneControl : MonoBehaviour
{
    [Header("Flight Settings")]
    public float currentSpeed = 200f;      // Default forward speed
    public float pitchPower = 50f;         // Rotation speed around X (nose up/down)
    public float yawPower = 50f;           // Rotation speed around Y (bank/turn)
    public float liftPower = 5f;           // How much upward force when moving forward
    public float gravityMultiplier = 2f;   // Strength of simulated gravity
    public float fallMultiplier = 3.5f;    // Extra downward force when stalling

    [Header("Flip Settings")]
    public float flipSpeed = 360f;         // Degrees per second for flip
    public float sideShiftAmount = 5f;     // How much to shift sideways during flip
    private float lastAPressTime = 0f;     // Time of last A press
    private float lastDPressTime = 0f;     // Time of last D press
    private float doublePressWindow = 0.3f; // Time window for double press
    private bool isFlipping = false;       // Whether plane is currently flipping
    private float currentFlipProgress = 0f; // Current progress of flip (0-360)
    private Vector3 flipDirection = Vector3.zero; // Direction of current flip

    [Header("Auto-Balance Settings")]
    public float autoBalanceStrength = 2f;  // How quickly the plane returns to level
    public float autoBalanceThreshold = 0.1f; // Minimum roll angle before auto-balance kicks in
    private float lastRollInput = 0f;      // Track the last roll input
    private float rollInputTimeout = 1f;    // How long to wait after roll input before auto-balancing
    private float rollInputTimer = 0f;      // Timer for roll input timeout

    [Header("Speed Settings")]
    public float acceleration = 1f;        // How quickly speed lerps
    public float maxSpeedAir = 150f;       // Top cruise speed

    [Header("Thruster Settings")]
    public float boostTargetSpeed = 500f; // Maximum speed during boost
    public float boostAcceleration = 50f; // Speed increase per second during boost
    public int maxThrusterThreshold = 10;
    public int currentThrusterThreshold = 5;
    private bool mustRechargeThrusterFull = false;
    private float thrusterConsumptionAccumulator = 0f;
    private bool isBoosting = false;

    [Header("Effects Settings")]
    [Tooltip("Particle systems for boost effects")]
    public List<ParticleSystem> planeEffects;

    [Header("Audio Settings")]
    private AudioSource currentAudioSource; // Current playing audio source

    [Header("Camera Settings")]
    public Transform planeCamera; // Assign in Inspector or auto-find
    private Quaternion cameraOriginalLocalRotation;

    // Cached components
    private Rigidbody rb;
    private bool isOutsideGround = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;

        // Test if AudioSetting exists
        if (AudioSetting.Instance == null)
        {
            Debug.LogWarning("[PlaneControl] AudioSetting.Instance is null! Creating a temporary audio system.");
        }
        else
        {
            Debug.Log("[PlaneControl] AudioSetting.Instance found successfully.");
        }

        // Find camera if not assigned
        if (planeCamera == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null)
                planeCamera = cam.transform;
        }
        if (planeCamera != null)
            cameraOriginalLocalRotation = planeCamera.localRotation;

        // Turn off all particle effects initially
        if(planeEffects != null)
        {
            foreach(var fx in planeEffects)
                if(fx != null)
                    fx.Stop();
        }

        // Initialize thruster threshold system
        maxThrusterThreshold = 5;
        currentThrusterThreshold = maxThrusterThreshold;
    }

    void Update()
    {
        if (!isFlipping)
        {
            AirControl();
            HandleThruster();
            ControlPlaneEffects();
            HandleAutoBalance();

            // Restore camera's local rotation if not flipping
            if (planeCamera != null)
                planeCamera.localRotation = cameraOriginalLocalRotation;
        }
        else
        {
            HandleFlip();

            // Lock camera roll during flip
            if (planeCamera != null)
            {
                Vector3 euler = planeCamera.localEulerAngles;
                euler.z = -transform.localEulerAngles.z; // Counteract the plane's roll
                planeCamera.localEulerAngles = euler;
            }
        }
        CheckGroundBounds();

        // Thruster Energy Management (similar to laser logic)
        ManageThrusterEnergy();
    }

    void FixedUpdate()
    {
        ApplyFlightForces();
    }

    /// <summary>
    /// Handles thruster energy management similar to laser threshold system
    /// </summary>
    void ManageThrusterEnergy()
    {
        // Recharge threshold when not boosting and not at max
        if (!isBoosting && currentThrusterThreshold < maxThrusterThreshold)
        {
            thrusterConsumptionAccumulator += Time.deltaTime;
            if (thrusterConsumptionAccumulator >= 1f)
            {
                currentThrusterThreshold = Mathf.Min(currentThrusterThreshold + 1, maxThrusterThreshold);
                thrusterConsumptionAccumulator = 0f;
            }
        }

        // If fully recharged, allow thruster use again
        if (mustRechargeThrusterFull && currentThrusterThreshold == maxThrusterThreshold)
        {
            mustRechargeThrusterFull = false;
        }
    }

    /// <summary>
    /// Handles mouse & keyboard input to pitch, yaw, and throttle.
    /// </summary>
    void AirControl()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Handle double press detection for A and D
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastAPressTime < doublePressWindow)
            {
                // Double press detected - start left flip
                StartFlip(Vector3.forward);
            }
            lastAPressTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastDPressTime < doublePressWindow)
            {
                // Double press detected - start right flip
                StartFlip(Vector3.back);
            }
            lastDPressTime = Time.time;
        }

        // Only process normal controls if not flipping
        if (!isFlipping)
        {
            // Track roll input for auto-balance
            if (Mathf.Abs(mouseX) > 0.1f)
            {
                lastRollInput = mouseX;
                rollInputTimer = rollInputTimeout;
            }

            // Rotate based on mouse
            Quaternion rotChange = Quaternion.Euler(
                -mouseY * pitchPower * Time.deltaTime,
                 mouseX * yawPower * Time.deltaTime,
                 0f);
            rb.MoveRotation(rb.rotation * rotChange);

            // Throttle control: S to slow to zero, otherwise accelerate toward maxSpeedAir
            if(Input.GetKey(KeyCode.S))
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * acceleration);
            else
                currentSpeed = Mathf.Lerp(currentSpeed, maxSpeedAir, Time.deltaTime * acceleration);
        }
    }

    /// <summary>
    /// Handles automatic leveling of the plane when not actively rolling.
    /// </summary>
    void HandleAutoBalance()
    {
        // Update roll input timer
        if (rollInputTimer > 0)
        {
            rollInputTimer -= Time.deltaTime;
        }

        // Only auto-balance if we haven't had roll input recently
        if (rollInputTimer <= 0)
        {
            // Get current roll angle
            float currentRoll = transform.eulerAngles.z;
            if (currentRoll > 180f) currentRoll -= 360f;

            // If roll angle is significant, apply auto-balance
            if (Mathf.Abs(currentRoll) > autoBalanceThreshold)
            {
                // Calculate target rotation to return to level
                float targetRoll = 0f;
                float rollCorrection = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * autoBalanceStrength);
                
                // Apply the correction
                Vector3 currentRotation = transform.eulerAngles;
                currentRotation.z = rollCorrection;
                transform.eulerAngles = currentRotation;
            }
        }
    }

    /// <summary>
    /// Applies lift, gravity, and forward velocity.
    /// </summary>
    void ApplyFlightForces()
    {
        // Forward speed factor for lift
        float speedFactor = Mathf.Clamp(currentSpeed * 0.02f, 0f, 0.5f);
        float pitchAngle = Vector3.Dot(transform.forward, Vector3.up);

        // Lift when nose is not pointed too downward and speed is sufficient
        if(pitchAngle > -0.2f && currentSpeed > 15f)
            rb.AddForce(transform.up * liftPower * speedFactor, ForceMode.Acceleration);

        // Stall / drop behavior
        if(currentSpeed < 60f)
            rb.AddForce(Vector3.down * fallMultiplier * 10f, ForceMode.Acceleration);
        else if(currentSpeed < 15f || pitchAngle < -0.3f)
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        else
            rb.AddForce(-Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

        // Always propel forward
        rb.velocity = transform.forward * currentSpeed;
    }

    /// <summary>
    /// Starts thruster boost when pressing Space.
    /// </summary>
    void HandleThruster()
    {
        if(Input.GetKeyDown(KeyCode.Space) && !mustRechargeThrusterFull && currentThrusterThreshold > 0)
            StartCoroutine(ThrusterBoost());
    }

    IEnumerator ThrusterBoost()
    {
        if (isBoosting)
            yield break;

        isBoosting = true;
        float originalMax = maxSpeedAir;
        maxSpeedAir = boostTargetSpeed; // Use your boostTargetSpeed variable

        // --- THRUSTER SOUND (always 2D, always audible) ---
        if (AudioSetting.Instance != null && AudioSetting.Instance.thrusterSound != null)
        {
            if (currentAudioSource != null)
            {
                currentAudioSource.Stop();
                Destroy(currentAudioSource.gameObject);
            }
            GameObject audioObj = new GameObject("ThrusterAudio");
            audioObj.transform.SetParent(transform);
            currentAudioSource = audioObj.AddComponent<AudioSource>();
            currentAudioSource.clip = AudioSetting.Instance.thrusterSound;
            currentAudioSource.loop = true;
            currentAudioSource.volume = AudioSetting.Instance.thrusterSoundVolume;
            // Do NOT set spatialBlend, keep it 2D!
            currentAudioSource.Play();
        }

        // --- BOOST EFFECTS ---
        if (planeEffects != null)
            foreach (var fx in planeEffects)
                if (fx != null && !fx.isPlaying)
                    fx.Play();

        // --- SMOOTH SPEED RAMP-UP ---
        while (currentThrusterThreshold > 0 && !mustRechargeThrusterFull && Input.GetKey(KeyCode.Space))
        {
            // Smoothly ramp up speed
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeedAir, boostAcceleration * Time.deltaTime);

            // Consume threshold every 1 second
            thrusterConsumptionAccumulator += Time.deltaTime;
            if (thrusterConsumptionAccumulator >= 1f)
            {
                currentThrusterThreshold = Mathf.Max(currentThrusterThreshold - 1, 0);
                thrusterConsumptionAccumulator = 0f;
                if (currentThrusterThreshold == 0)
                {
                    mustRechargeThrusterFull = true;
                    break;
                }
            }
            yield return null;
        }

        // --- END BOOST ---
        maxSpeedAir = originalMax;
        isBoosting = false;
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeedAir);

        // --- SWITCH BACK TO NORMAL FLIGHT SOUND ---
        if (AudioSetting.Instance != null && AudioSetting.Instance.normalFlightSound != null)
        {
            if (currentAudioSource != null)
            {
                currentAudioSource.Stop();
                Destroy(currentAudioSource.gameObject);
            }
            GameObject audioObj = new GameObject("FlightAudio");
            audioObj.transform.SetParent(transform);
            currentAudioSource = audioObj.AddComponent<AudioSource>();
            currentAudioSource.clip = AudioSetting.Instance.normalFlightSound;
            currentAudioSource.loop = true;
            currentAudioSource.volume = AudioSetting.Instance.normalFlightSoundVolume;
            // Do NOT set spatialBlend, keep it 2D!
            currentAudioSource.Play();
        }

        // --- STOP EFFECTS ---
        if (planeEffects != null)
            foreach (var fx in planeEffects)
                if (fx != null && fx.isPlaying)
                    fx.Stop();

        thrusterConsumptionAccumulator = 0f;
    }

    /// <summary>
    /// Ensures particle effects match boosting state.
    /// </summary>
    void ControlPlaneEffects()
    {
        if(planeEffects == null)
            return;

        if(isBoosting)
        {
            foreach(var fx in planeEffects)
                if(fx != null && !fx.isPlaying)
                    fx.Play();
        }
        else
        {
            foreach(var fx in planeEffects)
                if(fx != null && fx.isPlaying)
                    fx.Stop();
        }
    }

    void HandleFlip()
    {
        if (isFlipping)
        {
            // Calculate rotation for this frame
            float rotationThisFrame = flipSpeed * Time.deltaTime;
            currentFlipProgress += rotationThisFrame;

            // Apply rotation
            transform.Rotate(flipDirection * rotationThisFrame, Space.Self);

            // Apply side shift based on flip direction
            Vector3 sideShift = Vector3.Cross(transform.forward, Vector3.up).normalized * sideShiftAmount * Time.deltaTime;
            if (flipDirection.z < 0) // Right flip (D key)
                sideShift = -sideShift;
            transform.position += sideShift;

            // Check if flip is complete
            if (currentFlipProgress >= 360f)
            {
                isFlipping = false;
                currentFlipProgress = 0f;
            }
        }
    }

    void StartFlip(Vector3 direction)
    {
        if (!isFlipping)
        {
            isFlipping = true;
            currentFlipProgress = 0f;
            flipDirection = direction;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Enemy"))
        {
            // gameObject.SetActive(false); // Removed to let GameManager handle deactivation
            var stats = GetComponent<PlaneStats>();
            if (stats != null)
            {
                stats.TakeDamage(stats.CurrentHP); // This will trigger HandleDeath and GameManager
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Bullet") || other.CompareTag("Missile"))
        {
            // Damage is now handled by MachineGunControl or bullet/missile logic
        }
    }

    void CheckGroundBounds()
    {
        if (GameManager.Instance != null && GameManager.Instance.groundPrefab != null)
        {
            Collider groundCollider = GameManager.Instance.groundPrefab.GetComponent<Collider>();
            if (groundCollider != null)
            {
                Bounds bounds = groundCollider.bounds;
                Vector3 pos = transform.position;
                bool inside =
                    pos.x >= bounds.min.x && pos.x <= bounds.max.x &&
                    pos.z >= bounds.min.z && pos.z <= bounds.max.z;
                if (!inside && !isOutsideGround)
                {
                    isOutsideGround = true;
                    // Turn the plane around (rotate 180 degrees on Y axis)
                    transform.Rotate(0f, 180f, 0f);
                    Debug.Log($"[PlaneControl] Plane exited ground area, turning around at {pos}");
                }
                else if (inside && isOutsideGround)
                {
                    isOutsideGround = false;
                }
            }
        }
    }
}
