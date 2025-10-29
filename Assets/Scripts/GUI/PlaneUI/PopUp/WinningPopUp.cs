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

    // Start is called before the first frame update
    void Start()
    {
        // Ensure the popup is off by default
        if (winningPopupParent != null)
        {
            winningPopupParent.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if all enemies are destroyed
        if (!isActive && AreAllEnemiesDestroyed())
        {
            ActivateWinningPopup();
        }
    }

    /// <summary>
    /// Checks if all enemy ships (main boss and side ships) are destroyed
    /// </summary>
    bool AreAllEnemiesDestroyed()
    {
        if (GameManager.Instance == null)
            return false;

        // Check if main boss is destroyed
        if (GameManager.Instance.currentBoss != null)
            return false;

        // Check if any side ships are still active
        var activeEnemyShips = GameManager.Instance.GetActiveEnemyShips();
        if (activeEnemyShips != null && activeEnemyShips.Count > 0)
        {
            // Remove any null references (destroyed ships)
            activeEnemyShips.RemoveAll(ship => ship == null);
            
            // If there are still active ships, not all enemies are destroyed
            if (activeEnemyShips.Count > 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Activates the winning popup and starts blinking
    /// </summary>
    public void ActivateWinningPopup()
    {
        if (isActive || winningPopupParent == null)
            return;

        isActive = true;
        winningPopupParent.SetActive(true);
        
        // Start blinking
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkText());
        
        Debug.Log("Winning popup activated - All enemies destroyed!");
    }

    /// <summary>
    /// Deactivates the winning popup and stops blinking
    /// </summary>
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

    /// <summary>
    /// Coroutine that makes the text blink every blinkInterval seconds
    /// </summary>
    IEnumerator BlinkText()
    {
        while (isActive)
        {
            // Show text
            if (winningPopupParent != null)
            {
                winningPopupParent.SetActive(true);
            }
            
            yield return new WaitForSeconds(blinkInterval);
            
            // Hide text
            if (winningPopupParent != null)
            {
                winningPopupParent.SetActive(false);
            }
            
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
