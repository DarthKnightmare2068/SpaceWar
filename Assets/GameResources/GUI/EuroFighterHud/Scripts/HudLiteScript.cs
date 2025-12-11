using UnityEngine;
using UnityEngine.UI;


public class HudLiteScript : MonoBehaviour
{
    public static HudLiteScript current;


    //Config Variables
    public bool isActive = false;

    public Transform aircraft;
    public Rigidbody aircraftRB;
    //

    //Hud Display Variables
    public string activeMsg = "HUD Activated";

    public RectTransform hudPanel;

    public bool useRoll = true;
    public float rollAmplitude = 1, rollOffSet = 0, rollFilterFactor = 0.25f;
    public RectTransform horizonRoll;
    public Text horizonRollTxt;

    public bool usePitch = true;
    public float pitchAmplitude = 1, pitchOffSet = 0, pitchXOffSet = 0, pitchYOffSet = 0, pitchFilterFactor = 0.125f;
    public RectTransform horizonPitch;
    public Text horizonPitchTxt;
    
    public bool useHeading = true;
    public float headingAmplitude = 1, headingOffSet = 0, headingFilterFactor = 0.1f;
    public RectTransform compassHSI;
    public Text headingTxt;
    public CompassBar compassBar;


    public bool useAltitude = true;
    public float altitudeAmplitude = 1, altitudeOffSet = 0, altitudeFilterFactor = 0.5f;
    public Text altitudeTxt;

    public bool useSpeed = true;
    public float speedAmplitude = 1, speedOffSet = 0, speedFilterFactor = 0.25f;
    public Text speedTxt;
    //


    //All Flight Variables
    public float speed, altitude, pitch, roll, heading;


    //Internal Calculation Variables
    Vector3 currentPosition, lastPosition, relativeSpeed, lastSpeed;


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////// Inicialization
    void Awake() { if (aircraft == null && aircraftRB != null) aircraft = aircraftRB.transform; }
    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Debug.Log("[HUD] Start() auto-assigning player.");
            SetAircraft(GameManager.Instance.currentPlayer);
        }
        else
        {
            Debug.Log("[HUD] Start() did not find player to assign.");
        }
        Debug.Log($"[HUD] After Start: aircraft={aircraft}, aircraftRB={aircraftRB}");
    }
    public void toogleHud()
    {
        SndPlayer.playClick();
        hudPanel.gameObject.SetActive(!hudPanel.gameObject.activeSelf);

        if (hudPanel.gameObject.activeSelf)
        {
            current = this;
            if (aircraft == null && aircraftRB != null) aircraft = aircraftRB.transform;

            // Auto-assign player if available
            if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            {
                Debug.Log($"[HUD] Attempting to auto-assign player in toogleHud. Player: {GameManager.Instance.currentPlayer}");
                SetAircraft(GameManager.Instance.currentPlayer);
            }

            Debug.Log($"[HUD] After toogleHud: aircraft={aircraft}, aircraftRB={aircraftRB}");
            isActive = true;
            if (activeMsg != "") DisplayMsg.show(activeMsg, 5);
        }
        else
        {
            isActive = false; current = null;
            DisplayMsg.show("Hud Disabled");
        }
    }
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////// Inicialization



    /////////////////////////////////////////////////////// Updates and Calculations
    void Update()
    {
        // If not active, but player exists, reactivate HUD
        if (!isActive && GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            isActive = true;
            if (hudPanel != null) hudPanel.gameObject.SetActive(true);
            SetAircraft(GameManager.Instance.currentPlayer);
            Debug.Log("[HUD] Reactivated after respawn.");
        }

        // Return if not active
        if (!isActive || !hudPanel.gameObject.activeSelf) return;

        // Robustly auto-assign player if missing (handles respawn)
        if ((aircraft == null || aircraftRB == null || !aircraft.gameObject.activeInHierarchy))
        {
            if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            {
                SetAircraft(GameManager.Instance.currentPlayer);
                Debug.Log("[HUD] Auto-reassigned player aircraft and Rigidbody in Update().");
            }
            else
            {
                isActive = false;
                Debug.LogWarning("[HUD] No player found to assign. HUD deactivated.");
                return;
            }
        }

        //////////////////////////////////////////// Frame Calculations
        lastPosition = currentPosition;
        lastSpeed = relativeSpeed;

        if (aircraft != null && aircraftRB == null) //Mode Transform
        {
            currentPosition = aircraft.transform.position;
            relativeSpeed = aircraft.transform.InverseTransformDirection((currentPosition - lastPosition) / Time.deltaTime);
        }
        else if (aircraft != null && aircraftRB != null)  //Mode RB
        {
            currentPosition = aircraftRB.transform.position;
            relativeSpeed = aircraftRB.transform.InverseTransformDirection(aircraftRB.velocity);
        }
        else
        {
            currentPosition = Vector3.zero;
            relativeSpeed = Vector3.zero;
        }
        //////////////////////////////////////////// Frame Calculations


        //////////////////////////////////////////// Compass, Heading and/or HSI
        if (useHeading)
        {
            heading = Mathf.LerpAngle(heading, aircraft.eulerAngles.y + headingOffSet, headingFilterFactor) % 360f;

            //Send values to Gui and Instruments
            if (compassHSI != null) compassHSI.localRotation = Quaternion.Euler(0, 0, headingAmplitude * heading);
            if (compassBar != null) compassBar.heading = heading;
            if (headingTxt != null) { if (heading < 0) headingTxt.text = (heading + 360f).ToString("000"); else headingTxt.text = heading.ToString("000"); }

        }
        //////////////////////////////////////////// Compass, Heading and/or HSI


        //////////////////////////////////////////// Roll
        if (useRoll)
        {
            roll = Mathf.LerpAngle(roll, aircraft.rotation.eulerAngles.z + rollOffSet, rollFilterFactor) % 360;

            //Send values to Gui and Instruments
            if (horizonRoll != null) horizonRoll.localRotation = Quaternion.Euler(0, 0, rollAmplitude * roll);
            if (horizonRollTxt != null)
            {
                //horizonRollTxt.text = roll.ToString("##");
                if (roll > 180) horizonRollTxt.text = (roll - 360).ToString("00");
                else if (roll < -180) horizonRollTxt.text = (roll + 360).ToString("00");
                else horizonRollTxt.text = roll.ToString("00");
            }
            //
        }
        //////////////////////////////////////////// Roll


        //////////////////////////////////////////// Pitch
        if (usePitch)
        {
            pitch = Mathf.LerpAngle(pitch, -aircraft.eulerAngles.x + pitchOffSet, pitchFilterFactor);

            //Send values to Gui and Instruments
            if (horizonPitch != null) horizonPitch.localPosition = new Vector3(-pitchAmplitude * pitch * Mathf.Sin(horizonPitch.transform.localEulerAngles.z * Mathf.Deg2Rad) + pitchXOffSet, pitchAmplitude * pitch * Mathf.Cos(horizonPitch.transform.localEulerAngles.z * Mathf.Deg2Rad) + pitchYOffSet, 0);
            if (horizonPitchTxt != null) horizonPitchTxt.text = pitch.ToString("0");
        }
        //////////////////////////////////////////// Pitch


        //////////////////////////////////////////// Altitude
        if (useAltitude)
        {
            altitude = Mathf.Lerp(altitude, altitudeOffSet + altitudeAmplitude * currentPosition.y, speedFilterFactor);

            //Send values to Gui and Instruments
            if (altitudeTxt != null) altitudeTxt.text = altitude.ToString("0").PadLeft(5);
        }
        //////////////////////////////////////////// Altitude


        //////////////////////////////////////////// Speed
        if (useSpeed)
        {
            speed = Mathf.Lerp(speed, speedOffSet + speedAmplitude * relativeSpeed.z, speedFilterFactor);

            //Send values to Gui and Instruments
            if (speedTxt != null) speedTxt.text = speed.ToString("0").PadLeft(5);//.ToString("##0");
        }
        //////////////////////////////////////////// Speed

    }
    /////////////////////////////////////////////////////// Updates and Calculations

    // Method to set the aircraft and rigidbody at runtime
    public void SetAircraft(GameObject plane)
    {
        if (plane != null)
        {
            aircraft = plane.transform;
            aircraftRB = plane.GetComponent<Rigidbody>();
            Debug.Log($"[HUD] SetAircraft called. Assigned aircraft={aircraft}, aircraftRB={aircraftRB}");
        }
        else
        {
            Debug.LogWarning("[HUD] SetAircraft called with null plane!");
        }
    }
}

// New script for auto-locking target in camera view
public class HudAutoLockScript : HudLiteScript
{
    public Camera mainCamera;
    public LayerMask enemyLayer;
    public Transform lockedTarget;
    public float lockMaxDistance = 1000f;

    void LateUpdate()
    {
        if (!isActive || mainCamera == null) return;
        Collider[] enemies = Physics.OverlapSphere(aircraft.position, lockMaxDistance, enemyLayer);
        Transform bestTarget = null;
        float bestAngle = float.MaxValue;
        foreach (var enemy in enemies)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(enemy.transform.position);
            bool inView = viewportPoint.z > 0 && viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1;
            if (inView)
            {
                float angle = Vector3.Angle(mainCamera.transform.forward, enemy.transform.position - mainCamera.transform.position);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestTarget = enemy.transform;
                }
            }
        }
        lockedTarget = bestTarget;
        if (lockedTarget != null)
        {
            // Show lock indicator or debug log
            Debug.Log($"Locked target: {lockedTarget.name}");
            // Optionally, add UI indicator here
        }
    }
}