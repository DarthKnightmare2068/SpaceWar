using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject planeCanvas;      // Assign PlaneCanvas
    public GameObject pauseCanvas;      // Assign PauseCanvas
    public GameObject controlButtonSetUp; // Assign ControlTutorial (the control/tips panel)
    public GameObject continueButton;   // Assign ContinueButton
    public GameObject controlButton;    // Assign ControlButton
    public GameObject returnButton;     // Assign ReturnButton

    // Start is called before the first frame update
    void Start()
    {
        pauseCanvas.SetActive(false); // Ensure PauseCanvas is off at start
    }

    // Update is called once per frame
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
        Debug.Log("ShowPause called (Pause button or Escape pressed)");
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
        Debug.Log("ContinueGame called (Continue button pressed)");
        pauseCanvas.SetActive(false);
        planeCanvas.SetActive(true);
        controlButtonSetUp.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowPauseMenu()
    {
        Debug.Log("ShowPauseMenu called (Control button pressed)");
        controlButtonSetUp.SetActive(true);
        continueButton.SetActive(false);
        controlButton.SetActive(false);
        returnButton.SetActive(false);
    }

    public void BackToPause()
    {
        Debug.Log("BackToPause called (Back button in ControlTutorial pressed)");
        controlButtonSetUp.SetActive(false);
        continueButton.SetActive(true);
        controlButton.SetActive(true);
        returnButton.SetActive(true);
    }

    public void ReturnToEnterGame()
    {
        Debug.Log("ReturnToEnterGame called (Return button pressed)");
        Time.timeScale = 1f;
        SceneManager.LoadScene("Enter Scene"); // Updated scene name
    }
}
