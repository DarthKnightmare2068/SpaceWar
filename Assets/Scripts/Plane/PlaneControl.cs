using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public partial class PlaneControl : MonoBehaviour
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
    private Collider cachedGroundCollider;
    private Bounds cachedGroundBounds;
    private float pendingMouseX = 0f;
    private float pendingMouseY = 0f;
    private float lastSpeed = float.NegativeInfinity;
    private Vector3 lastForward = Vector3.zero;

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

        if (GameManager.Instance != null && GameManager.Instance.groundPrefab != null)
        {
            cachedGroundCollider = GameManager.Instance.groundPrefab.GetComponent<Collider>();
            if (cachedGroundCollider != null)
            {
                // Bolt: Optimized - cache bounds to avoid native-to-managed call in Update
                cachedGroundBounds = cachedGroundCollider.bounds;
            }
        }
    }


    void Update()
    {
        if (!isFlipping)
        {
            AirControl();
            HandleThruster();
            // Bolt: Optimized - removed redundant ControlPlaneEffects call as the ThrusterBoost coroutine manages these effects
            // Auto-balance moved to FixedUpdate so it doesn't fight the physics step.

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
        Quaternion baseRotation = rb.rotation;

        if (!isFlipping)
        {
            Quaternion rotChange = Quaternion.Euler(
                -pendingMouseY * pitchPower * Time.fixedDeltaTime,
                 pendingMouseX * yawPower * Time.fixedDeltaTime,
                 0f);
            baseRotation = baseRotation * rotChange;
            pendingMouseX = 0f;
            pendingMouseY = 0f;

            baseRotation = ApplyAutoBalance(baseRotation, Time.fixedDeltaTime);
            rb.MoveRotation(baseRotation);
        }

        ApplyFlightForces();
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

            pendingMouseX += mouseX;
            pendingMouseY += mouseY;

            if(Input.GetKey(KeyCode.S))
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * acceleration);
            else
                currentSpeed = Mathf.Lerp(currentSpeed, maxSpeedAir, Time.deltaTime * acceleration);
        }
    }

    // Runs inside FixedUpdate and applies via rb.MoveRotation so the physics engine
    // doesn't fight against a direct transform.eulerAngles write each frame.
    // Returns the adjusted rotation; FixedUpdate then commits with rb.MoveRotation once.
    private Quaternion ApplyAutoBalance(Quaternion currentRotation, float deltaTime)
    {
        if (rollInputTimer > 0f)
        {
            rollInputTimer -= deltaTime;
            return currentRotation;
        }

        Vector3 euler = currentRotation.eulerAngles;
        float currentRoll = euler.z;
        if (currentRoll > 180f) currentRoll -= 360f;

        if (Mathf.Abs(currentRoll) <= autoBalanceThreshold)
            return currentRotation;

        // Framerate-independent smoothing: exponential approach to 0 roll.
        float t = 1f - Mathf.Exp(-deltaTime * autoBalanceStrength);
        float rollCorrection = Mathf.Lerp(currentRoll, 0f, t);
        euler.z = rollCorrection;
        return Quaternion.Euler(euler);
    }

    void ApplyFlightForces()
    {
        float speedFactor = Mathf.Clamp(currentSpeed * 0.02f, 0f, 0.5f);
        // Bolt: Optimized - replaced Vector3.Dot(transform.forward, Vector3.up) with transform.forward.y
        float pitchAngle = transform.forward.y;

        if(pitchAngle > -0.2f && currentSpeed > 15f)
            rb.AddForce(transform.up * liftPower * speedFactor, ForceMode.Acceleration);

        if(currentSpeed < 60f)
            rb.AddForce(Vector3.down * fallMultiplier * 10f, ForceMode.Acceleration);
        else if(currentSpeed < 15f || pitchAngle < -0.3f)
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        else
            rb.AddForce(-Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

        Vector3 forward = transform.forward;
        bool speedChanged = !Mathf.Approximately(currentSpeed, lastSpeed);
        bool forwardChanged = (forward - lastForward).sqrMagnitude > 1e-6f;
        if (speedChanged || forwardChanged)
        {
            rb.linearVelocity = forward * currentSpeed;
            lastSpeed = currentSpeed;
            lastForward = forward;
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

    void CheckGroundBounds()
    {
        if (cachedGroundCollider == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.groundPrefab != null)
            {
                cachedGroundCollider = GameManager.Instance.groundPrefab.GetComponent<Collider>();
                if (cachedGroundCollider != null)
                    cachedGroundBounds = cachedGroundCollider.bounds;
            }
            if (cachedGroundCollider == null) return;
        }

        // Bolt: Optimized - use cached bounds to avoid expensive native property access every frame
        Bounds bounds = cachedGroundBounds;
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
