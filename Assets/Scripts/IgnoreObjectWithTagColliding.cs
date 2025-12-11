using UnityEngine;

public class IgnoreObjectWithTagColliding : MonoBehaviour
{
    public string[] ignoreTags;

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
}
