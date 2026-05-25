using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DmgPopUp : MonoBehaviour
{
    public static DmgPopUp current;
    public GameObject dmgPopUpPrefab;

    private Camera cachedCamera;
    private Canvas cachedCanvas;

    private const int POOL_SIZE = 20;
    private const float POPUP_LIFETIME = 1f;
    private ObjectPool<DmgPopUpAnimation> pool;

    private struct ActivePopUp
    {
        public DmgPopUpAnimation animation;
        public float expireAt;
    }
    private readonly Queue<ActivePopUp> activePopUps = new Queue<ActivePopUp>();

    private void Awake()
    {
        if (current != null && current != this) { Destroy(this); return; }
        current = this;
        cachedCamera = Camera.main;
        cachedCanvas = GetComponentInParent<Canvas>();

        // Pre-warm pool to avoid Instantiate on first hits.
        DmgPopUpAnimation prefabAnim = dmgPopUpPrefab != null ? dmgPopUpPrefab.GetComponent<DmgPopUpAnimation>() : null;
        pool = new ObjectPool<DmgPopUpAnimation>(prefabAnim, POOL_SIZE, transform.parent);
    }

    private void Update()
    {
        float now = Time.time;
        // Bolt: Optimized - chronological recycling avoids per-popup Coroutine/WaitForSeconds allocations
        while (activePopUps.Count > 0 && now >= activePopUps.Peek().expireAt)
        {
            var popup = activePopUps.Dequeue();
            if (popup.animation != null)
                pool.Release(popup.animation);
        }
    }

    public static void ShowDamage(Vector3 worldPosition, int damage, Color color)
    {
        if (current == null) return;
        Vector3 spawnPos = worldPosition;
        Canvas canvas = current.cachedCanvas;
        if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera))
        {
            Camera cam = current.cachedCamera != null ? current.cachedCamera : Camera.main;
            spawnPos = cam.WorldToScreenPoint(worldPosition);
        }
        current.ShowPopUp(spawnPos, damage, color);
    }

    public static void ShowLaserDamage(Vector3 worldPosition, int damage)
    {
        ShowDamage(worldPosition, damage, Color.blue);
    }

    private void ShowPopUp(Vector3 position, string text, Color color)
    {
        DmgPopUpAnimation popUp = pool != null ? pool.Get(position, Quaternion.identity) : null;
        if (popUp == null) return;

        popUp.transform.SetParent(transform.parent, worldPositionStays: true);
        popUp.transform.position = position;

        popUp.Setup(text, color);
        activePopUps.Enqueue(new ActivePopUp { animation = popUp, expireAt = Time.time + POPUP_LIFETIME });
    }

    private void ShowPopUp(Vector3 position, int damage, Color color)
    {
        DmgPopUpAnimation popUp = pool != null ? pool.Get(position, Quaternion.identity) : null;
        if (popUp == null) return;

        popUp.transform.SetParent(transform.parent, worldPositionStays: true);
        popUp.transform.position = position;

        popUp.Setup(damage, color);
        activePopUps.Enqueue(new ActivePopUp { animation = popUp, expireAt = Time.time + POPUP_LIFETIME });
    }
}
