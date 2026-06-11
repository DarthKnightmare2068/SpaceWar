using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WinningPopUp : MonoBehaviour
{
    [Header("Winning Popup Settings")]
    public GameObject winningPopupParent;
    [Tooltip("How often the text blinks (in seconds)")]
    public float blinkInterval = 1f;
    [Tooltip("Whether the popup is currently active")]
    public bool isActive = false;
    [Tooltip("Seconds after scene load before win detection is allowed")]
    public float winCheckDelay = 3f;
    [Tooltip("Frequency of victory condition checks (in seconds). ~5Hz is plenty.")]
    public float winCheckInterval = 0.2f;

    private Coroutine blinkCoroutine;
    private float lastWinCheckTime = 0f;

    void Awake()
    {
        // Ensure popup is fully hidden on startup, even if not wired in inspector
        if (winningPopupParent == null)
        {
            winningPopupParent = gameObject;
        }

        isActive = false;
        if (winningPopupParent != null)
        {
            winningPopupParent.SetActive(false);
        }
    }

    void Start()
    {
        // Redundant safety in case Awake didn't run for some reason
        if (winningPopupParent != null)
        {
            winningPopupParent.SetActive(false);
        }
    }

    void Update()
    {
        if (isActive) return;

        // Don't check for win until after initial spawn / intro delay
        if (Time.timeSinceLevelLoad < winCheckDelay)
            return;

        // Bolt: Optimized - throttle the win check to save CPU cycles
        if (Time.time >= lastWinCheckTime + winCheckInterval)
        {
            lastWinCheckTime = Time.time;
            if (AreAllEnemiesDestroyed())
            {
                ActivateWinningPopup();
            }
        }
    }

    bool AreAllEnemiesDestroyed()
    {
        if (GameManager.Instance == null)
            return false;

        if (GameManager.Instance.currentBoss != null)
            return false;

        var activeEnemyShips = GameManager.Instance.GetActiveEnemyShips();
        if (activeEnemyShips != null)
        {
            // Bolt: Optimized - use a non-modifying loop with early exit to avoid per-frame allocations
            // and prevent unintended mutation of the global list.
            for (int i = 0; i < activeEnemyShips.Count; i++)
            {
                if (activeEnemyShips[i] != null)
                    return false;
            }
        }

        return true;
    }

    public void ActivateWinningPopup()
    {
        if (isActive || winningPopupParent == null)
            return;

        isActive = true;
        winningPopupParent.SetActive(true);
        
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkText());
    }

    public void DeactivateWinningPopup()
    {
        if (!isActive)
            return;

        isActive = false;
        
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (winningPopupParent != null)
        {
            winningPopupParent.SetActive(false);
        }
    }

    IEnumerator BlinkText()
    {
        while (isActive)
        {
            if (winningPopupParent != null)
            {
                winningPopupParent.SetActive(true);
            }
            
            yield return new WaitForSeconds(blinkInterval);
            
            if (winningPopupParent != null)
            {
                winningPopupParent.SetActive(false);
            }
            
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
