using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlaneControl : MonoBehaviour
{
    [Header("Flight Settings")]
    public float currentSpeed = 200f;
    public float pitchPower = 50f;
    public float yawPower = 50f;
    public float liftPower = 5f;
    public float gravityMultiplier = 2f;
    public float fallMultiplier = 3.5f;

    [Header("Flip Settings")]
    public float flipSpeed = 360f;
    public float sideShiftAmount = 5f;
    private float lastAPressTime = 0f;
    private float lastDPressTime = 0f;
    private float doublePressWindow = 0.3f;
    private bool isFlipping = false;
    private float currentFlipProgress = 0f;
    private Vector3 flipDirection = Vector3.zero;

    [Header("Auto-Balance Settings")]
    public float autoBalanceStrength = 2f;
    public float autoBalanceThreshold = 0.1f;
    private float rollInputTimer = 0f;
    private float rollInputTimeout = 1f;

    [Header("Speed Settings")]
    public float acceleration = 1f;
    public float maxSpeedAir = 150f;

    [Header("Thruster Settings")]
    public float boostTargetSpeed = 500f;
    public float boostAcceleration = 50f;
    public int maxThrusterThreshold = 10;
    public int currentThrusterThreshold = 5;
    private bool mustRechargeThrusterFull = false;
    private float thrusterConsumptionAccumulator = 0f;
    private bool isBoosting = false;

    [Header("Effects Settings")]
    public List<ParticleSystem> planeEffects;

    [Header("Audio Settings")]
    private AudioSource flightAudioSource;
    private AudioSource thrusterAudioSource;

    [Header("Camera Settings")]
    public Transform planeCamera;
    private Quaternion cameraOriginalLocalRotation;

    private Rigidbody rb;
    private bool isOutsideGround = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;

        InitializeAudioSources();

        if (planeCamera == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null)
                planeCamera = cam.transform;
        }
        if (planeCamera != null)
            cameraOriginalLocalRotation = planeCamera.localRotation;

        if(planeEffects != null)
        {
            foreach(var fx in planeEffects)
                if(fx != null)
                    fx.Stop();
        }

        maxThrusterThreshold = 5;
        currentThrusterThreshold = maxThrusterThreshold;
    }

    private void InitializeAudioSources()
    {
        if (AudioSetting.Instance == null)
        {
            return;
        }

        if (flightAudioSource == null)
        {
            GameObject flightAudioObj = new GameObject("FlightAudio");
            flightAudioObj.transform.SetParent(transform);
            flightAudioObj.transform.localPosition = Vector3.zero;
            flightAudioSource = flightAudioObj.AddComponent<AudioSource>();
            flightAudioSource.loop = true;
            flightAudioSource.playOnAwake = false;
            flightAudioSource.spatialBlend = 0f;
        }

        if (thrusterAudioSource == null)
        {
            GameObject thrusterAudioObj = new GameObject("ThrusterAudio");
            thrusterAudioObj.transform.SetParent(transform);
            thrusterAudioObj.transform.localPosition = Vector3.zero;
            thrusterAudioSource = thrusterAudioObj.AddComponent<AudioSource>();
            thrusterAudioSource.loop = true;
            thrusterAudioSource.playOnAwake = false;
            thrusterAudioSource.spatialBlend = 0f;
        }

        PlayFlightSound();
    }

    private void PlayFlightSound()
    {
        if (AudioSetting.Instance == null || AudioSetting.Instance.normalFlightSound == null) return;
        if (flightAudioSource == null) return;

        flightAudioSource.clip = AudioSetting.Instance.normalFlightSound;
        flightAudioSource.volume = AudioSetting.Instance.normalFlightSoundVolume;
        if (!flightAudioSource.isPlaying)
        {
            flightAudioSource.Play();
        }
    }

    private void PlayThrusterSound()
    {
        if (AudioSetting.Instance == null || AudioSetting.Instance.thrusterSound == null) return;
        if (thrusterAudioSource == null) return;

        thrusterAudioSource.clip = AudioSetting.Instance.thrusterSound;
        thrusterAudioSource.volume = AudioSetting.Instance.thrusterSoundVolume;
        if (!thrusterAudioSource.isPlaying)
        {
            thrusterAudioSource.Play();
        }
    }

    private void StopThrusterSound()
    {
        if (thrusterAudioSource != null && thrusterAudioSource.isPlaying)
        {
            thrusterAudioSource.Stop();
        }
    }

    void OnDestroy()
    {
        if (AudioSetting.Instance != null)
        {
            AudioSetting.Instance.CleanupPlayerAudio(gameObject);
        }
    }

    void Update()
    {
        if (!isFlipping)
        {
            AirControl();
            HandleThruster();
            ControlPlaneEffects();
            HandleAutoBalance();

            if (planeCamera != null)
                planeCamera.localRotation = cameraOriginalLocalRotation;
        }
        else
        {
            HandleFlip();

            if (planeCamera != null)
            {
                Vector3 euler = planeCamera.localEulerAngles;
                euler.z = -transform.localEulerAngles.z;
                planeCamera.localEulerAngles = euler;
            }
        }
        CheckGroundBounds();
        ManageThrusterEnergy();
    }

    void FixedUpdate()
    {
        ApplyFlightForces();
    }

    void ManageThrusterEnergy()
    {
        if (!isBoosting && currentThrusterThreshold < maxThrusterThreshold)
        {
            thrusterConsumptionAccumulator += Time.deltaTime;
            if (thrusterConsumptionAccumulator >= 1f)
            {
                currentThrusterThreshold = Mathf.Min(currentThrusterThreshold + 1, maxThrusterThreshold);
                thrusterConsumptionAccumulator = 0f;
            }
        }

        if (mustRechargeThrusterFull && currentThrusterThreshold == maxThrusterThreshold)
        {
            mustRechargeThrusterFull = false;
        }
    }

    void AirControl()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastAPressTime < doublePressWindow)
            {
                StartFlip(Vector3.forward);
            }
            lastAPressTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastDPressTime < doublePressWindow)
            {
                StartFlip(Vector3.back);
            }
            lastDPressTime = Time.time;
        }

        if (!isFlipping)
        {
            if (Mathf.Abs(mouseX) > 0.1f)
            {
                rollInputTimer = rollInputTimeout;
            }

            Quaternion rotChange = Quaternion.Euler(
                -mouseY * pitchPower * Time.deltaTime,
                 mouseX * yawPower * Time.deltaTime,
                 0f);
            rb.MoveRotation(rb.rotation * rotChange);

            if(Input.GetKey(KeyCode.S))
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * acceleration);
            else
                currentSpeed = Mathf.Lerp(currentSpeed, maxSpeedAir, Time.deltaTime * acceleration);
        }
    }

    void HandleAutoBalance()
    {
        if (rollInputTimer > 0)
        {
            rollInputTimer -= Time.deltaTime;
        }

        if (rollInputTimer <= 0)
        {
            float currentRoll = transform.eulerAngles.z;
            if (currentRoll > 180f) currentRoll -= 360f;

            if (Mathf.Abs(currentRoll) > autoBalanceThreshold)
            {
                float targetRoll = 0f;
                float rollCorrection = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * autoBalanceStrength);
                
                Vector3 currentRotation = transform.eulerAngles;
                currentRotation.z = rollCorrection;
                transform.eulerAngles = currentRotation;
            }
        }
    }

    void ApplyFlightForces()
    {
        float speedFactor = Mathf.Clamp(currentSpeed * 0.02f, 0f, 0.5f);
        float pitchAngle = Vector3.Dot(transform.forward, Vector3.up);

        if(pitchAngle > -0.2f && currentSpeed > 15f)
            rb.AddForce(transform.up * liftPower * speedFactor, ForceMode.Acceleration);

        if(currentSpeed < 60f)
            rb.AddForce(Vector3.down * fallMultiplier * 10f, ForceMode.Acceleration);
        else if(currentSpeed < 15f || pitchAngle < -0.3f)
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        else
            rb.AddForce(-Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

        rb.velocity = transform.forward * currentSpeed;
    }

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
        maxSpeedAir = boostTargetSpeed;

        if (flightAudioSource != null)
        {
            flightAudioSource.Stop();
        }
        PlayThrusterSound();

        if (planeEffects != null)
            foreach (var fx in planeEffects)
                if (fx != null && !fx.isPlaying)
                    fx.Play();

        while (currentThrusterThreshold > 0 && !mustRechargeThrusterFull && Input.GetKey(KeyCode.Space))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeedAir, boostAcceleration * Time.deltaTime);

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

        maxSpeedAir = originalMax;
        isBoosting = false;
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeedAir);

        StopThrusterSound();
        PlayFlightSound();

        if (planeEffects != null)
            foreach (var fx in planeEffects)
                if (fx != null && fx.isPlaying)
                    fx.Stop();

        thrusterConsumptionAccumulator = 0f;
    }

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
            float rotationThisFrame = flipSpeed * Time.deltaTime;
            currentFlipProgress += rotationThisFrame;

            transform.Rotate(flipDirection * rotationThisFrame, Space.Self);

            Vector3 sideShift = Vector3.Cross(transform.forward, Vector3.up).normalized * sideShiftAmount * Time.deltaTime;
            if (flipDirection.z < 0)
                sideShift = -sideShift;
            transform.position += sideShift;

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
            var stats = GetComponent<PlaneStats>();
            if (stats != null)
            {
                stats.TakeDamage(stats.CurrentHP);
            }
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
                    transform.Rotate(0f, 180f, 0f);
                }
                else if (inside && isOutsideGround)
                {
                    isOutsideGround = false;
                }
            }
        }
    }
}
