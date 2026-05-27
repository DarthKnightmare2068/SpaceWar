using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject planeCanvas;
    public GameObject pauseCanvas;
    public GameObject controlButtonSetUp;
    public GameObject continueButton;
    public GameObject controlButton;
    public GameObject returnButton;

    void Start()
    {
        SetActiveSafe(pauseCanvas, false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If pauseCanvas isn't wired we just no-op rather than throw.
            if (pauseCanvas == null) return;

            if (!pauseCanvas.activeSelf)
            {
                ShowPause();
            }
            else if (controlButtonSetUp != null && controlButtonSetUp.activeSelf)
            {
                BackToPause();
            }
            else
            {
                ContinueGame();
            }
        }
    }

    public void ShowPause()
    {
        SetActiveSafe(pauseCanvas, true);
        SetActiveSafe(planeCanvas, false);
        SetActiveSafe(controlButtonSetUp, false);
        SetActiveSafe(continueButton, true);
        SetActiveSafe(controlButton, true);
        SetActiveSafe(returnButton, true);
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        SetActiveSafe(pauseCanvas, false);
        SetActiveSafe(planeCanvas, true);
        SetActiveSafe(controlButtonSetUp, false);
        Time.timeScale = 1f;
    }

    public void ShowPauseMenu()
    {
        SetActiveSafe(controlButtonSetUp, true);
        SetActiveSafe(continueButton, false);
        SetActiveSafe(controlButton, false);
        SetActiveSafe(returnButton, false);
    }

    public void BackToPause()
    {
        SetActiveSafe(controlButtonSetUp, false);
        SetActiveSafe(continueButton, true);
        SetActiveSafe(controlButton, true);
        SetActiveSafe(returnButton, true);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    public void ReturnToEnterGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Enter Scene");
    }
}
