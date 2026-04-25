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
    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        current = this;
        cachedCamera = Camera.main;
        cachedCanvas = GetComponentInParent<Canvas>();

        // Pre-warm pool to avoid Instantiate on first hits.
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var obj = Instantiate(dmgPopUpPrefab, transform.parent);
            obj.SetActive(false);
            pool.Enqueue(obj);
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
        current.ShowPopUp(spawnPos, damage.ToString(), color);
    }

    public static void ShowLaserDamage(Vector3 worldPosition, int damage)
    {
        ShowDamage(worldPosition, damage, Color.blue);
    }

    private void ShowPopUp(Vector3 position, string text, Color color)
    {
        GameObject popUp;
        if (pool.Count > 0)
        {
            popUp = pool.Dequeue();
            popUp.transform.SetParent(transform.parent);
            popUp.transform.position = position;
            popUp.transform.rotation = Quaternion.identity;
            popUp.SetActive(true);
        }
        else
        {
            popUp = Instantiate(dmgPopUpPrefab, position, Quaternion.identity, transform.parent);
        }

        var tmp = popUp.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
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

    private IEnumerator ReturnToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
