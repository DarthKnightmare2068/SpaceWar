using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("Target scene to load")]
    public string targetSceneName = "Plane Test";
    [Tooltip("Whether to fade out start screen audio when transitioning")]
    public bool fadeOutAudioOnTransition = true;
    [Tooltip("Duration of audio fade out")]
    public float fadeOutDuration = 1f;
    [Tooltip("Skip audio fade for immediate transition")]
    public bool immediateTransition = false;

    [Header("Exit Game Settings")]
    [Tooltip("Whether to fade out audio when exiting game")]
    public bool fadeOutAudioOnExit = true;
    [Tooltip("Duration of audio fade out on exit")]
    public float exitFadeOutDuration = 0.5f;

    void Awake()
    {
        // Initialize scene selection
    }

    public void LoadSceneByName(string sceneName)
    {
        targetSceneName = sceneName;
        StartCoroutine(TransitionToScene());
    }

    public void ExitGame()
    {
        StartCoroutine(ExitGameCoroutine());
    }

    private IEnumerator TransitionToScene()
    {
        // Fade out start screen audio if enabled and not immediate
        if (fadeOutAudioOnTransition && !immediateTransition && StartScreenAudio.Instance != null)
        {
            StartScreenAudio.Instance.FadeOutMusic(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
        }
        else if (StartScreenAudio.Instance != null)
        {
            // Always fade out music before loading the new scene
            StartScreenAudio.Instance.FadeOutMusic(1f); // 1 second fade if not already faded
            yield return new WaitForSeconds(1f);
        }

        // Load the scene
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadSceneAsync(targetSceneName);
        }
        else
        {
            Debug.LogError("No valid scene name specified for transition!");
        }
    }

    private IEnumerator ExitGameCoroutine()
    {
        // Fade out start screen audio if enabled
        if (fadeOutAudioOnExit && StartScreenAudio.Instance != null)
        {
            StartScreenAudio.Instance.FadeOutMusic(exitFadeOutDuration);
            yield return new WaitForSeconds(exitFadeOutDuration);
        }

        // Exit the game
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Get current selected scene name
    public string GetCurrentSelectedScene()
    {
        return targetSceneName;
    }
}
