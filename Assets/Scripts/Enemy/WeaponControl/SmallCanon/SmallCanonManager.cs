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
    private List<Transform> players = new List<Transform>();
    private Dictionary<SmallCanonControl, Transform> canonTargets = new Dictionary<SmallCanonControl, Transform>();
    private float reviveTimer = 0f;
    private bool reviveTimerRunning = false;

    void Awake()
    {
        canons = new List<SmallCanonControl>(GetComponentsInChildren<SmallCanonControl>(true));

        WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
        if (dmgControl != null)
        {
            howCloseToPlayer = dmgControl.GetSmallCanonFireRange();
        }
        else
        {
            howCloseToPlayer = 100f;
        }

        SetAllCanonsHP();
        maxCanonCount = canons.Count;
        currentCanonCount = maxCanonCount;

        foreach (var canon in canons)
        {
            if (canon != null)
                canon.SetTrackingMode(trackPlayerInstantly);
        }
    }

    void Update()
    {
        CleanCanonList();
        currentCanonCount = canons.Count;
        if (reviveTimerRunning)
        {
            reviveTimer -= Time.deltaTime;
            if (reviveTimer <= 0f)
            {
                reviveTimerRunning = false;
                if (currentCanonCount > 0)
                {
                    ReviveAllCanons();
                }
            }
        }

        foreach (var canon in canons)
        {
            if (canon != null)
                canon.SetTrackingMode(trackPlayerInstantly);
        }

        RecountCanons();
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

    public void RecountCanons()
    {
        int count = 0;
        foreach (var canon in canons)
        {
            if (canon != null && canon.gameObject.activeInHierarchy && canon.currentHP > 0)
                count++;
        }
        currentCanonCount = count;
    }

    public void CleanCanonList()
    {
        canons.RemoveAll(c => c == null);
    }
}
