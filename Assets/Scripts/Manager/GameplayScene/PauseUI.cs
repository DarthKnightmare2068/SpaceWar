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
        pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!pauseCanvas.activeSelf)
            {
                ShowPause();
            }
            else if (controlButtonSetUp.activeSelf)
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
        pauseCanvas.SetActive(true);
        planeCanvas.SetActive(false);
        controlButtonSetUp.SetActive(false);
        continueButton.SetActive(true);
        controlButton.SetActive(true);
        returnButton.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        pauseCanvas.SetActive(false);
        planeCanvas.SetActive(true);
        controlButtonSetUp.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowPauseMenu()
    {
        controlButtonSetUp.SetActive(true);
        continueButton.SetActive(false);
        controlButton.SetActive(false);
        returnButton.SetActive(false);
    }

    public void BackToPause()
    {
        controlButtonSetUp.SetActive(false);
        continueButton.SetActive(true);
        controlButton.SetActive(true);
        returnButton.SetActive(true);
    }

    public void ReturnToEnterGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Enter Scene");
    }
}
