using System.Collections.Generic;
using UnityEngine;

public class SmallCanonManager : MonoBehaviour
{
    [Tooltip("Max number of canons that can lock on a single player")]
    public int maxCanonsPerPlayer = 1;
    [Tooltip("All your canon instances")]
    public List<SmallCanonControl> canons = new List<SmallCanonControl>();
    [Tooltip("HP for all canons")]
    public int canonHP = 10000;
    [Tooltip("VFX prefab to play when a canon is destroyed")]
    public GameObject canonDestroyedVFX;

    [Header("Revive Settings")]
    [Tooltip("Time in seconds before canons revive if not all are destroyed")]
    public float reviveTime = 60f;
    [Tooltip("Initial number of canons at start")]
    public int maxCanonCount = 0;
    [Tooltip("Current number of canons alive")]
    public int currentCanonCount = 0;

    [Header("Tracking Mode")]
    public bool trackPlayerInstantly = false;

    private float howCloseToPlayer;
    private float reviveTimer = 0f;
    private bool reviveTimerRunning = false;
    private bool lastTrackPlayerInstantly;

    void Awake()
    {
        canons = new List<SmallCanonControl>(GetComponentsInChildren<SmallCanonControl>(true));

        // Use GetComponentInParent first — avoids a scene-wide search.
        WeaponDmgControl dmgControl = GetComponentInParent<WeaponDmgControl>();
        if (dmgControl == null)
            dmgControl = FindAnyObjectByType<WeaponDmgControl>();

        howCloseToPlayer = dmgControl != null ? dmgControl.GetSmallCanonFireRange() : 100f;

        SetAllCanonsHP();
        maxCanonCount = canons.Count;
        currentCanonCount = maxCanonCount;
        lastTrackPlayerInstantly = trackPlayerInstantly;

        foreach (var canon in canons)
        {
            if (canon != null)
                canon.SetTrackingMode(trackPlayerInstantly);
        }
    }

    void Update()
    {
        // CleanCanonList removed — canons use SetActive(false) on death, never Destroy(),
        // so the list never contains null references during normal gameplay.
        // RecountCanons removed — count is maintained by OnCanonDestroyed() and ReviveAllCanons().

        if (reviveTimerRunning)
        {
            reviveTimer -= Time.deltaTime;
            if (reviveTimer <= 0f)
            {
                reviveTimerRunning = false;
                ReviveAllCanons();
            }
        }

        if (trackPlayerInstantly != lastTrackPlayerInstantly)
        {
            lastTrackPlayerInstantly = trackPlayerInstantly;
            foreach (var canon in canons)
            {
                if (canon != null)
                    canon.SetTrackingMode(trackPlayerInstantly);
            }
        }
    }

    public void SetAllCanonsHP()
    {
        foreach (var canon in canons)
        {
            if (canon != null)
            {
                canon.maxHP = canonHP;
                canon.currentHP = canonHP;
            }
        }
    }

    public void OnCanonDestroyed()
    {
        currentCanonCount = Mathf.Max(currentCanonCount - 1, 0);
        if (!reviveTimerRunning)
        {
            reviveTimer = reviveTime;
            reviveTimerRunning = true;
        }
    }

    private void ReviveAllCanons()
    {
        foreach (var canon in canons)
        {
            if (canon != null)
            {
                canon.currentHP = canonHP;
                canon.gameObject.SetActive(true);
            }
        }
        currentCanonCount = maxCanonCount;
    }
}
