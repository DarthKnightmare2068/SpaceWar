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
    private ObjectPool<Transform> pool;

    private void Awake()
    {
        if (current != null && current != this) { Destroy(this); return; }
        current = this;
        cachedCamera = Camera.main;
        cachedCanvas = GetComponentInParent<Canvas>();

        // Pre-warm pool to avoid Instantiate on first hits.
        Transform prefabTransform = dmgPopUpPrefab != null ? dmgPopUpPrefab.transform : null;
        pool = new ObjectPool<Transform>(prefabTransform, POOL_SIZE, transform.parent);
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
        current.ShowPopUp(spawnPos, damage.ToString(), color);
    }

    public static void ShowLaserDamage(Vector3 worldPosition, int damage)
    {
        ShowDamage(worldPosition, damage, Color.blue);
    }

    private void ShowPopUp(Vector3 position, string text, Color color)
    {
        Transform popUp = pool != null ? pool.Get(position, Quaternion.identity) : null;
        if (popUp == null) return;
        popUp.SetParent(transform.parent, worldPositionStays: true);
        popUp.position = position;

        var tmp = popUp.GetChild(0).GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;

        var anim = popUp.GetComponent<DmgPopUpAnimation>();
        if (anim != null)
        {
            anim.baseColor = color;
            anim.ResetAnimation();
        }

        StartCoroutine(ReturnToPool(popUp, POPUP_LIFETIME));
    }

    private IEnumerator ReturnToPool(Transform obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) pool.Release(obj);
    }
}
