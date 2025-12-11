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

    private Coroutine blinkCoroutine;

    void Start()
    {
        if (winningPopupParent != null)
        {
            winningPopupParent.SetActive(false);
        }
    }

    void Update()
    {
        if (!isActive && AreAllEnemiesDestroyed())
        {
            ActivateWinningPopup();
        }
    }

    bool AreAllEnemiesDestroyed()
    {
        if (GameManager.Instance == null)
            return false;

        if (GameManager.Instance.currentBoss != null)
            return false;

        var activeEnemyShips = GameManager.Instance.GetActiveEnemyShips();
        if (activeEnemyShips != null && activeEnemyShips.Count > 0)
        {
            activeEnemyShips.RemoveAll(ship => ship == null);
            
            if (activeEnemyShips.Count > 0)
                return false;
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
