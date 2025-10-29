using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DmgPopUp : MonoBehaviour
{
    public static DmgPopUp current;
    public GameObject dmgPopUpPrefab;
    private Color color;
    private void Awake()
    {
        current = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // No keypress test code here
    }

    // Static method to show a damage popup from anywhere
    public static void ShowDamage(Vector3 worldPosition, int damage, Color color)
    {
        if (current == null) return;
        // Convert world position to screen position if using Screen Space Canvas
        Vector3 spawnPos = worldPosition;
        Canvas canvas = current.GetComponentInParent<Canvas>();
        if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera))
        {
            spawnPos = Camera.main.WorldToScreenPoint(worldPosition);
        }
        current.CreatePupUp(spawnPos, damage.ToString(), color);
    }

    // Static method to show a blue damage popup for laser weapon
    public static void ShowLaserDamage(Vector3 worldPosition, int damage)
    {
        ShowDamage(worldPosition, damage, Color.blue);
    }

    public void CreatePupUp(Vector3 position, string text, Color color)
    {
        var popUp = Instantiate(dmgPopUpPrefab, position, Quaternion.identity, transform.parent); // parent to canvas
        var temp = popUp.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        temp.text = text;
        temp.color = color;
        var anim = popUp.GetComponent<DmgPopUpAnimation>();
        if (anim != null) anim.baseColor = color;

        //Destroy Timer
        Destroy(popUp, 1f);
    }
}
