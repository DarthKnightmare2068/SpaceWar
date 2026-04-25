using System.Collections.Generic;
using UnityEngine;

namespace LuminaryLabs.HologramShieldShader
{
    public class CustomShieldCollisionResponse : MonoBehaviour
    {
        private Renderer shieldRenderer;
        private Material shieldMaterial;

        public float effectDuration = 1f;
        public float collisionRadius = 1f;
        public float collisionIntensity = 2f;

        private const int MAX_COLLISIONS = 10;

        private class CollisionData
        {
            public Vector3 point;
            public float radius;
            public float intensity;
            public float startTime;
        }

        private List<CollisionData> activeCollisions = new List<CollisionData>();

        // Pre-allocated shader arrays — avoids GC allocation every frame
        private Vector4[] collisionPoints;
        private float[] collisionRadii;
        private float[] collisionIntensities;
        private float[] collisionStartTimes;

        // Dirty flag: skip shader push when nothing is active and shader is already cleared
        private bool shaderCleared = true;

        void Awake()
        {
            shieldRenderer = GetComponent<Renderer>();
            shieldMaterial = shieldRenderer.material;

            collisionPoints = new Vector4[MAX_COLLISIONS];
            collisionRadii = new float[MAX_COLLISIONS];
            collisionIntensities = new float[MAX_COLLISIONS];
            collisionStartTimes = new float[MAX_COLLISIONS];
        }

        void Update()
        {
            float currentTime = Time.time;
            activeCollisions.RemoveAll(c => (currentTime - c.startTime) > effectDuration);

            int count = Mathf.Min(activeCollisions.Count, MAX_COLLISIONS);

            // Nothing active and shader already cleared — skip the push entirely
            if (count == 0 && shaderCleared) return;

            for (int i = 0; i < count; i++)
            {
                CollisionData col = activeCollisions[i];
                float fade = Mathf.Clamp01(1f - ((currentTime - col.startTime) / effectDuration));
                collisionPoints[i] = new Vector4(col.point.x, col.point.y, col.point.z, 0f);
                collisionRadii[i] = col.radius;
                collisionIntensities[i] = col.intensity * fade;
                collisionStartTimes[i] = col.startTime;
            }
            for (int i = count; i < MAX_COLLISIONS; i++)
            {
                collisionPoints[i] = Vector4.zero;
                collisionRadii[i] = 0f;
                collisionIntensities[i] = 0f;
                collisionStartTimes[i] = 0f;
            }

            shieldMaterial.SetVectorArray("_CollisionPoints", collisionPoints);
            shieldMaterial.SetFloatArray("_CollisionRadii", collisionRadii);
            shieldMaterial.SetFloatArray("_CollisionIntensities", collisionIntensities);
            shieldMaterial.SetFloatArray("_CollisionStartTimes", collisionStartTimes);
            shieldMaterial.SetInt("_NumCollisions", count);
            shieldMaterial.SetFloat("_EffectDuration", effectDuration);

            shaderCleared = (count == 0);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount > 0)
                RegisterHit(collision.contacts[0].point);
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterHit(other.ClosestPoint(transform.position));
        }

        public void RegisterHit(Vector3 hitPoint)
        {
            CollisionData newCollision = new CollisionData
            {
                point = hitPoint,
                radius = collisionRadius,
                intensity = collisionIntensity,
                startTime = Time.time
            };

            if (activeCollisions.Count < MAX_COLLISIONS)
            {
                activeCollisions.Add(newCollision);
            }
            else
            {
                float minRemaining = float.MaxValue;
                int replaceIndex = 0;
                float currentTime = Time.time;
                for (int i = 0; i < activeCollisions.Count; i++)
                {
                    float remaining = effectDuration - (currentTime - activeCollisions[i].startTime);
                    if (remaining < minRemaining)
                    {
                        minRemaining = remaining;
                        replaceIndex = i;
                    }
                }
                activeCollisions[replaceIndex] = newCollision;
            }

            shaderCleared = false;
        }
    }
}
