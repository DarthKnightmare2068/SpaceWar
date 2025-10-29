using UnityEngine;

public class MissileAutoLock : MonoBehaviour
{
    [Header("Homing Settings")]
    public Transform target; // Set this externally when launching the missile
    public float rotateSpeed = 200f;
    public float maxTurnRate = 180f; // Maximum degrees per second the missile can turn
    public float updateInterval = 0.5f; // How often to log targeting info

    private Rigidbody rb;
    private float lastLogTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }
        lastLogTime = Time.time;
    }

    void FixedUpdate()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            // Calculate direction to target
            Vector3 direction = (target.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target.position);
            
            // Calculate rotation needed
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Smoothly rotate towards target
            Quaternion newRotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                maxTurnRate * Time.fixedDeltaTime
            );
            
            // Apply rotation
            rb.MoveRotation(newRotation);
            
            // Log targeting info at intervals
            if (Time.time - lastLogTime >= updateInterval)
            {
                lastLogTime = Time.time;
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
