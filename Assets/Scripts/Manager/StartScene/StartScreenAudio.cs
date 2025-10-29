using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenAudio : MonoBehaviour
{
    public static StartScreenAudio Instance;

    [Header("Start Screen Audio Settings")]
    [Tooltip("Background music for the start screen")]
    public AudioClip startScreenMusic;
    [Tooltip("Volume for start screen music")]
    [Range(0f, 1f)] public float startScreenMusicVolume = 0.5f;
    [Tooltip("Sound effect for button clicks")]
    public AudioClip buttonClickSound;
    [Tooltip("Volume for button click sound")]
    [Range(0f, 1f)] public float buttonClickVolume = 0.7f;

    private AudioSource musicAudioSource;
    private AudioSource sfxAudioSource;
    private bool isInitialized = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
            isInitialized = true;
            
            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from scene loading events
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure audio continues playing after scene transition
        if (isInitialized && musicAudioSource != null && startScreenMusic != null)
        {
            // If music was playing before scene change, make sure it continues
            if (!musicAudioSource.isPlaying)
            {
                musicAudioSource.clip = startScreenMusic;
                musicAudioSource.volume = startScreenMusicVolume;
                musicAudioSource.Play();
            }
        }
    }

    void Start()
    {
        if (isInitialized)
        {
            PlayStartScreenMusic();
        }
    }

    private void SetupAudioSources()
    {
        // Create music audio source
        GameObject musicObj = new GameObject("StartScreenMusic");
        musicObj.transform.SetParent(transform);
        musicAudioSource = musicObj.AddComponent<AudioSource>();
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f; // 2D sound
        musicAudioSource.playOnAwake = false; // Don't auto-play, we'll control it

        // Create SFX audio source
        GameObject sfxObj = new GameObject("StartScreenSFX");
        sfxObj.transform.SetParent(transform);
        sfxAudioSource = sfxObj.AddComponent<AudioSource>();
        sfxAudioSource.spatialBlend = 0f; // 2D sound
        sfxAudioSource.playOnAwake = false;
    }

    public void PlayStartScreenMusic()
    {
        if (startScreenMusic != null && musicAudioSource != null)
        {
            musicAudioSource.clip = startScreenMusic;
            musicAudioSource.volume = startScreenMusicVolume;
            musicAudioSource.Play();
        }
    }

    public void StopStartScreenMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    public void PlayButtonClickSound()
    {
        if (buttonClickSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
    }

    // Volume control methods
    public void SetMusicVolume(float volume)
    {
        startScreenMusicVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = startScreenMusicVolume;
        }
    }

    public void SetButtonClickVolume(float volume)
    {
        buttonClickVolume = Mathf.Clamp01(volume);
    }

    // Getter methods for UI sliders
    public float GetMusicVolume()
    {
        return startScreenMusicVolume;
    }

    public float GetButtonClickVolume()
    {
        return buttonClickVolume;
    }

    // Pause and resume functionality
    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying)
        {
            musicAudioSource.UnPause();
        }
    }

    // Fade in/out functionality
    public void FadeInMusic(float duration = 2f)
    {
        StartCoroutine(FadeMusicCoroutine(0f, startScreenMusicVolume, duration));
    }

    public void FadeOutMusic(float duration = 2f)
    {
        StartCoroutine(FadeMusicCoroutine(startScreenMusicVolume, 0f, duration));
    }

    private IEnumerator FadeMusicCoroutine(float startVolume, float endVolume, float duration)
    {
        if (musicAudioSource == null) yield break;

        float currentTime = 0f;
        musicAudioSource.volume = startVolume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, endVolume, currentTime / duration);
            musicAudioSource.volume = newVolume;
            yield return null;
        }

        musicAudioSource.volume = endVolume;
    }

    // Method to stop audio when transitioning to game scene (optional)
    public void StopAudioForGameScene()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            StartCoroutine(FadeMusicCoroutine(startScreenMusicVolume, 0f, 1f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
