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
        public float returnTime;
    }
    private Queue<ActivePopUp> activePopUps = new Queue<ActivePopUp>();

    private void Awake()
    {
        if (current != null && current != this) { Destroy(this); return; }
        current = this;
        cachedCamera = Camera.main;
        cachedCanvas = GetComponentInParent<Canvas>();

        // Bolt: Optimized - Pre-warm pool with DmgPopUpAnimation to avoid per-popup GetComponent calls.
        DmgPopUpAnimation prefabAnim = dmgPopUpPrefab != null ? dmgPopUpPrefab.GetComponent<DmgPopUpAnimation>() : null;
        pool = new ObjectPool<DmgPopUpAnimation>(prefabAnim, POOL_SIZE, transform.parent);
    }

    private void Update()
    {
        // Bolt: Optimized - centralized recycling system avoids per-popup Coroutine and WaitForSeconds allocations.
        while (activePopUps.Count > 0 && Time.time >= activePopUps.Peek().returnTime)
        {
            ActivePopUp popUp = activePopUps.Dequeue();
            if (popUp.animation != null) pool.Release(popUp.animation);
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
        // Bolt: Optimized - pass int damage directly to avoid ToString() string allocation.
        current.ShowPopUp(spawnPos, damage, color);
    }

    public static void ShowLaserDamage(Vector3 worldPosition, int damage)
    {
        ShowDamage(worldPosition, damage, Color.blue);
    }

    private void ShowPopUp(Vector3 position, int damage, Color color)
    {
        // Bolt: Optimized - pool.Get returns DmgPopUpAnimation directly.
        DmgPopUpAnimation anim = pool != null ? pool.Get(position, Quaternion.identity) : null;
        if (anim == null) return;

        Transform popUpTransform = anim.transform;
        popUpTransform.SetParent(transform.parent, worldPositionStays: true);
        popUpTransform.position = position;

        // Bolt: Optimized - SetData handles text and color updates efficiently.
        anim.SetData(damage, color);
        anim.ResetAnimation();

        activePopUps.Enqueue(new ActivePopUp
        {
            animation = anim,
            returnTime = Time.time + POPUP_LIFETIME
        });
    }
}
