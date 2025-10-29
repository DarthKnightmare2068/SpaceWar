using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSetting : MonoBehaviour
{
    public static AudioSetting Instance;

    [Header("Weapon Audio Settings")]
    [Tooltip("Sound effect for machine gun firing")]
    public AudioClip machineGunSound;
    [Tooltip("Volume for machine gun sound effect")]
    [Range(0f, 1f)] public float machineGunSFXVolume = 0.5f;
    [Tooltip("Sound effect for missile launch")]
    public AudioClip missileSound;
    [Tooltip("Volume for missile sound effect")]
    [Range(0f, 1f)] public float missileSFXVolume = 0.7f;
    [Tooltip("Sound effect for laser firing")]
    public AudioClip laserSound;
    [Tooltip("Volume for laser sound effect")]
    [Range(0f, 1f)] public float laserSFXVolume = 0.7f;

    [Header("Plane Audio Settings")]
    [Tooltip("Sound for normal flight")]
    public AudioClip normalFlightSound;
    [Tooltip("Volume for normal flight sound")]
    [Range(0f, 1f)] public float normalFlightSoundVolume = 0.7f;
    [Tooltip("Sound for thruster boost")]
    public AudioClip thrusterSound;
    [Tooltip("Volume for thruster sound")]
    [Range(0f, 1f)] public float thrusterSoundVolume = 0.7f;
    [Tooltip("Sound for respawn")]
    public AudioClip respawnSound;
    [Tooltip("Volume for respawn sound")]
    [Range(0f, 1f)] public float respawnSoundVolume = 0.7f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayRespawnSoundForPlayer(GameObject player)
    {
        if (respawnSound != null)
        {
            // Play as 2D sound (spatialBlend = 0)
            GameObject tempAudio = new GameObject("TempRespawnAudio");
            AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
            audioSource.clip = respawnSound;
            audioSource.volume = respawnSoundVolume;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.Play();
            Destroy(tempAudio, respawnSound.length);
            StartCoroutine(SwitchToNormalFlightSoundCoroutine(player, respawnSound.length));
        }
        else if (normalFlightSound != null)
        {
            // If no respawn sound, play normal flight sound immediately
            StartCoroutine(SwitchToNormalFlightSoundCoroutine(player, 0f));
        }
    }

    private IEnumerator SwitchToNormalFlightSoundCoroutine(GameObject player, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        if (player != null && normalFlightSound != null)
        {
            GameObject audioObj = new GameObject("FlightAudio");
            audioObj.transform.SetParent(player.transform);
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = normalFlightSound;
            audioSource.loop = true;
            audioSource.volume = normalFlightSoundVolume;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Plays thruster sound for a player plane
    /// </summary>
    /// <param name="player">The player GameObject to attach the sound to</param>
    /// <returns>The AudioSource component that was created</returns>
    public AudioSource PlayThrusterSound(GameObject player)
    {
        if (thrusterSound != null && player != null)
        {
            GameObject thrusterAudio = new GameObject("ThrusterAudio");
            thrusterAudio.transform.SetParent(player.transform);
            AudioSource thrusterSource = thrusterAudio.AddComponent<AudioSource>();
            thrusterSource.clip = thrusterSound;
            thrusterSource.loop = true;
            thrusterSource.volume = thrusterSoundVolume;
            thrusterSource.spatialBlend = 0f; // 2D sound
            thrusterSource.Play();
            
            Debug.Log("[AudioSetting] Playing thruster sound for player");
            return thrusterSource;
        }
        else
        {
            Debug.LogWarning("[AudioSetting] Cannot play thruster sound - thrusterSound: " + (thrusterSound != null ? "Assigned" : "NULL") + 
                           ", player: " + (player != null ? "Valid" : "NULL"));
            return null;
        }
    }
}
