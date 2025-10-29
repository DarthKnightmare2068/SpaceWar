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

    // Internals
    private float howCloseToPlayer; // This will be set from WeaponDmgControl
    private List<Transform> players = new List<Transform>();
    private Dictionary<SmallCanonControl, Transform> canonTargets = new Dictionary<SmallCanonControl, Transform>();
    private float reviveTimer = 0f;
    private bool reviveTimerRunning = false;

    void Awake()
    {
        // Always rebuild the canons list from all children (active and inactive)
        canons = new List<SmallCanonControl>(GetComponentsInChildren<SmallCanonControl>(true));

        // Initialize from WeaponDmgControl
        WeaponDmgControl dmgControl = FindObjectOfType<WeaponDmgControl>();
        if (dmgControl != null)
        {
            howCloseToPlayer = dmgControl.GetSmallCanonFireRange();
        }
        else
        {
            howCloseToPlayer = 100f; // Fallback
            Debug.LogWarning("WeaponDmgControl not found. Using default fire range for SmallCanonManager.");
        }

        SetAllCanonsHP();
        maxCanonCount = canons.Count;
        currentCanonCount = maxCanonCount;

        // Set tracking mode for all canons at start
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
        // Handle revive timer
        if (reviveTimerRunning)
        {
            reviveTimer -= Time.deltaTime;
            if (reviveTimer <= 0f)
            {
                reviveTimerRunning = false;
                // Only revive if at least one canon is still alive
                if (currentCanonCount > 0)
                {
                    ReviveAllCanons();
                }
            }
        }

        // Sync tracking mode for all canons every frame (in case toggled at runtime)
        foreach (var canon in canons)
        {
            if (canon != null)
                canon.SetTrackingMode(trackPlayerInstantly);
        }

        // Failsafe: recount canons every frame
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

    // Call this from SmallCanonControl when a canon is destroyed
    public void OnCanonDestroyed()
    {
        currentCanonCount = Mathf.Max(currentCanonCount - 1, 0);
        Debug.Log($"[SmallCanonManager] OnCanonDestroyed called. currentCanonCount: {currentCanonCount}");
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
                // Optionally, respawn or reset position/state if needed
                canon.gameObject.SetActive(true);
            }
        }
        currentCanonCount = maxCanonCount;
        Debug.Log("[SmallCanonManager] All canons revived!");
    }

    // Failsafe recount method
    public void RecountCanons()
    {
        int count = 0;
        foreach (var canon in canons)
        {
            if (canon != null && canon.gameObject.activeInHierarchy && canon.currentHP > 0)
                count++;
        }
        if (currentCanonCount != count)
        {
            Debug.Log($"[SmallCanonManager] Recounted canons. currentCanonCount: {count}");
        }
        currentCanonCount = count;
    }

    public void CleanCanonList()
    {
        canons.RemoveAll(c => c == null);
    }
}
