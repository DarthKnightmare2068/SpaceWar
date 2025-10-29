using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreObjectWithTagColliding : MonoBehaviour
{
    public string[] ignoreTags;

    // Start is called before the first frame update
    void Start()
    {
        Collider thisCollider = GetComponent<Collider>();
        if (thisCollider == null) return;
        foreach (string tag in ignoreTags)
        {
            GameObject[] ignoreObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in ignoreObjects)
            {
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    Physics.IgnoreCollision(thisCollider, col);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Add this method to log trigger collisions
    void OnTriggerEnter(Collider other)
    {
        // Debug.Log removed as requested
    }

    // Add this method to log non-trigger collisions
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[IgnoreObjectWithTagColliding] {gameObject.name} (Tag: {gameObject.tag}) collided with {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
    }
}
