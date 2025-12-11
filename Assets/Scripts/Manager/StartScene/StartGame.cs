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
        if (fadeOutAudioOnTransition && !immediateTransition && StartScreenAudio.Instance != null)
        {
            StartScreenAudio.Instance.FadeOutMusic(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
        }
        else if (StartScreenAudio.Instance != null)
        {
            StartScreenAudio.Instance.FadeOutMusic(1f);
            yield return new WaitForSeconds(1f);
        }

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadSceneAsync(targetSceneName);
        }
    }

    private IEnumerator ExitGameCoroutine()
    {
        if (fadeOutAudioOnExit && StartScreenAudio.Instance != null)
        {
            StartScreenAudio.Instance.FadeOutMusic(exitFadeOutDuration);
            yield return new WaitForSeconds(exitFadeOutDuration);
        }

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public string GetCurrentSelectedScene()
    {
        return targetSceneName;
    }
}
