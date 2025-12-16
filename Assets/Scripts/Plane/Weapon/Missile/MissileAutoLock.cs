using UnityEngine;

public class MissileAutoLock : MonoBehaviour
{
    [Header("Homing Settings")]
    public Transform target; // Set this externally when launching the missile
    public float maxTurnRate = 180f; // Maximum degrees per second the missile can turn

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }
    }

    void FixedUpdate()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            // Calculate direction to target
            Vector3 direction = (target.position - transform.position).normalized;
            
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
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
