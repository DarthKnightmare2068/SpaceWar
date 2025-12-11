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

    [Header("Audio Pool Settings")]
    [Tooltip("Number of audio sources to pool for one-shot sounds")]
    [SerializeField] private int audioPoolSize = 10;

    private Queue<AudioSource> oneShotAudioPool = new Queue<AudioSource>();
    private List<AudioSource> activeOneShotSources = new List<AudioSource>();
    
    private Dictionary<GameObject, AudioSource> playerFlightAudio = new Dictionary<GameObject, AudioSource>();
    private Dictionary<GameObject, AudioSource> playerThrusterAudio = new Dictionary<GameObject, AudioSource>();
    private Dictionary<GameObject, AudioSource> playerLaserAudio = new Dictionary<GameObject, AudioSource>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeAudioPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioPool()
    {
        for (int i = 0; i < audioPoolSize; i++)
        {
            AudioSource source = CreatePooledAudioSource($"PooledAudio_{i}");
            oneShotAudioPool.Enqueue(source);
        }
    }

    private AudioSource CreatePooledAudioSource(string name)
    {
        GameObject audioObj = new GameObject(name);
        audioObj.transform.SetParent(transform);
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        audioObj.SetActive(false);
        return source;
    }

    void Update()
    {
        for (int i = activeOneShotSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeOneShotSources[i];
            if (source != null && !source.isPlaying)
            {
                ReturnToPool(source);
                activeOneShotSources.RemoveAt(i);
            }
        }
        
        CleanupDestroyedPlayerAudio(playerFlightAudio);
        CleanupDestroyedPlayerAudio(playerThrusterAudio);
        CleanupDestroyedPlayerAudio(playerLaserAudio);
    }

    private void CleanupDestroyedPlayerAudio(Dictionary<GameObject, AudioSource> audioDict)
    {
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in audioDict)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value != null)
                {
                    kvp.Value.Stop();
                    Destroy(kvp.Value.gameObject);
                }
            }
        }
        foreach (var key in toRemove)
        {
            audioDict.Remove(key);
        }
    }

    private AudioSource GetPooledAudioSource()
    {
        AudioSource source;
        
        if (oneShotAudioPool.Count > 0)
        {
            source = oneShotAudioPool.Dequeue();
        }
        else
        {
            source = CreatePooledAudioSource($"PooledAudio_Extra_{activeOneShotSources.Count}");
        }
        
        source.gameObject.SetActive(true);
        activeOneShotSources.Add(source);
        return source;
    }

    private void ReturnToPool(AudioSource source)
    {
        if (source == null) return;
        
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        oneShotAudioPool.Enqueue(source);
    }

    public void PlayOneShotSound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        
        AudioSource source = GetPooledAudioSource();
        source.clip = clip;
        source.volume = volume;
        source.loop = false;
        source.Play();
    }

    public void PlayMachineGunSound()
    {
        PlayOneShotSound(machineGunSound, machineGunSFXVolume);
    }

    public void PlayMissileSound()
    {
        PlayOneShotSound(missileSound, missileSFXVolume);
    }

    public void PlayRespawnSoundForPlayer(GameObject player)
    {
        if (respawnSound != null)
        {
            PlayOneShotSound(respawnSound, respawnSoundVolume);
            StartCoroutine(SwitchToNormalFlightSoundCoroutine(player, respawnSound.length));
        }
        else if (normalFlightSound != null)
        {
            StartCoroutine(SwitchToNormalFlightSoundCoroutine(player, 0f));
        }
    }

    private IEnumerator SwitchToNormalFlightSoundCoroutine(GameObject player, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
            
        if (player != null && normalFlightSound != null)
        {
            StartFlightSound(player);
        }
    }

    public AudioSource StartFlightSound(GameObject player)
    {
        if (player == null || normalFlightSound == null) return null;
        
        if (playerFlightAudio.TryGetValue(player, out AudioSource existingSource))
        {
            if (existingSource != null)
            {
                if (!existingSource.isPlaying)
                {
                    existingSource.clip = normalFlightSound;
                    existingSource.volume = normalFlightSoundVolume;
                    existingSource.Play();
                }
                return existingSource;
            }
            else
            {
                playerFlightAudio.Remove(player);
            }
        }
        
        GameObject audioObj = new GameObject("FlightAudio");
        audioObj.transform.SetParent(player.transform);
        audioObj.transform.localPosition = Vector3.zero;
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = normalFlightSound;
        audioSource.loop = true;
        audioSource.volume = normalFlightSoundVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        
        playerFlightAudio[player] = audioSource;
        return audioSource;
    }

    public void StopFlightSound(GameObject player)
    {
        if (player == null) return;
        
        if (playerFlightAudio.TryGetValue(player, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }

    public AudioSource PlayThrusterSound(GameObject player)
    {
        if (thrusterSound == null || player == null)
        {
            return null;
        }
        
        if (playerThrusterAudio.TryGetValue(player, out AudioSource existingSource))
        {
            if (existingSource != null)
            {
                existingSource.clip = thrusterSound;
                existingSource.volume = thrusterSoundVolume;
                if (!existingSource.isPlaying)
                {
                    existingSource.Play();
                }
                return existingSource;
            }
            else
            {
                playerThrusterAudio.Remove(player);
            }
        }
        
        GameObject thrusterAudio = new GameObject("ThrusterAudio");
        thrusterAudio.transform.SetParent(player.transform);
        thrusterAudio.transform.localPosition = Vector3.zero;
        AudioSource thrusterSource = thrusterAudio.AddComponent<AudioSource>();
        thrusterSource.clip = thrusterSound;
        thrusterSource.loop = true;
        thrusterSource.volume = thrusterSoundVolume;
        thrusterSource.spatialBlend = 0f;
        thrusterSource.Play();
        
        playerThrusterAudio[player] = thrusterSource;
        
        return thrusterSource;
    }

    public void StopThrusterSound(GameObject player)
    {
        if (player == null) return;
        
        if (playerThrusterAudio.TryGetValue(player, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }

    public AudioSource GetLaserAudioSource(GameObject player)
    {
        if (laserSound == null || player == null) return null;
        
        if (playerLaserAudio.TryGetValue(player, out AudioSource existingSource))
        {
            if (existingSource != null)
            {
                return existingSource;
            }
            else
            {
                playerLaserAudio.Remove(player);
            }
        }
        
        GameObject laserAudio = new GameObject("LaserAudio");
        laserAudio.transform.SetParent(player.transform);
        laserAudio.transform.localPosition = Vector3.zero;
        AudioSource laserSource = laserAudio.AddComponent<AudioSource>();
        laserSource.clip = laserSound;
        laserSource.loop = true;
        laserSource.volume = laserSFXVolume;
        laserSource.spatialBlend = 0f;
        laserSource.playOnAwake = false;
        
        playerLaserAudio[player] = laserSource;
        return laserSource;
    }

    public void CleanupPlayerAudio(GameObject player)
    {
        if (player == null) return;
        
        CleanupPlayerAudioSource(playerFlightAudio, player);
        CleanupPlayerAudioSource(playerThrusterAudio, player);
        CleanupPlayerAudioSource(playerLaserAudio, player);
    }

    private void CleanupPlayerAudioSource(Dictionary<GameObject, AudioSource> audioDict, GameObject player)
    {
        if (audioDict.TryGetValue(player, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source.gameObject);
            }
            audioDict.Remove(player);
        }
    }
}
