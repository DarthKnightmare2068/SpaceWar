using System.Collections;
using UnityEngine;

public partial class PlaneControl
{
    private void InitializeAudioSources()
    {
        if (AudioSetting.Instance == null) return;

        flightAudioSource = AudioSetting.Instance.GetOrCreateLoopedSource(
            gameObject, "FlightAudio", AudioSetting.Instance.normalFlightSound, AudioSetting.Instance.normalFlightSoundVolume);
        thrusterAudioSource = AudioSetting.Instance.GetOrCreateLoopedSource(
            gameObject, "ThrusterAudio", AudioSetting.Instance.thrusterSound, AudioSetting.Instance.thrusterSoundVolume);

        PlayFlightSound();
    }

    private void PlayFlightSound()
    {
        if (AudioSetting.Instance == null || AudioSetting.Instance.normalFlightSound == null) return;
        if (flightAudioSource == null) return;

        flightAudioSource.clip = AudioSetting.Instance.normalFlightSound;
        flightAudioSource.volume = AudioSetting.Instance.normalFlightSoundVolume;
        if (!flightAudioSource.isPlaying)
        {
            flightAudioSource.Play();
        }
    }

    private void PlayThrusterSound()
    {
        if (AudioSetting.Instance == null || AudioSetting.Instance.thrusterSound == null) return;
        if (thrusterAudioSource == null) return;

        thrusterAudioSource.clip = AudioSetting.Instance.thrusterSound;
        thrusterAudioSource.volume = AudioSetting.Instance.thrusterSoundVolume;
        if (!thrusterAudioSource.isPlaying)
        {
            thrusterAudioSource.Play();
        }
    }

    private void StopThrusterSound()
    {
        if (thrusterAudioSource != null && thrusterAudioSource.isPlaying)
        {
            thrusterAudioSource.Stop();
        }
    }

    private void OnDestroy()
    {
        if (AudioSetting.Instance != null)
        {
            AudioSetting.Instance.CleanupPlayerAudio(gameObject);
        }
    }

    private void ManageThrusterEnergy()
    {
        if (!isBoosting && currentThrusterThreshold < maxThrusterThreshold)
        {
            thrusterConsumptionAccumulator += Time.deltaTime;
            if (thrusterConsumptionAccumulator >= 1f)
            {
                currentThrusterThreshold = Mathf.Min(currentThrusterThreshold + 1, maxThrusterThreshold);
                thrusterConsumptionAccumulator = 0f;
            }
        }

        if (mustRechargeThrusterFull && currentThrusterThreshold == maxThrusterThreshold)
        {
            mustRechargeThrusterFull = false;
        }
    }

    private void HandleThruster()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !mustRechargeThrusterFull && currentThrusterThreshold > 0)
            StartCoroutine(ThrusterBoost());
    }

    private IEnumerator ThrusterBoost()
    {
        if (isBoosting)
            yield break;

        isBoosting = true;
        float originalMax = maxSpeedAir;
        maxSpeedAir = boostTargetSpeed;

        if (flightAudioSource != null)
        {
            flightAudioSource.Stop();
        }
        PlayThrusterSound();

        if (planeEffects != null)
            foreach (var fx in planeEffects)
                if (fx != null && !fx.isPlaying)
                    fx.Play();

        while (currentThrusterThreshold > 0 && !mustRechargeThrusterFull && Input.GetKey(KeyCode.Space))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeedAir, boostAcceleration * Time.deltaTime);

            thrusterConsumptionAccumulator += Time.deltaTime;
            if (thrusterConsumptionAccumulator >= 1f)
            {
                currentThrusterThreshold = Mathf.Max(currentThrusterThreshold - 1, 0);
                thrusterConsumptionAccumulator = 0f;
                if (currentThrusterThreshold == 0)
                {
                    mustRechargeThrusterFull = true;
                    break;
                }
            }
            yield return null;
        }

        maxSpeedAir = originalMax;
        isBoosting = false;
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeedAir);

        StopThrusterSound();
        PlayFlightSound();

        if (planeEffects != null)
            foreach (var fx in planeEffects)
                if (fx != null && fx.isPlaying)
                    fx.Stop();

        thrusterConsumptionAccumulator = 0f;
    }

    private void ControlPlaneEffects()
    {
        if (planeEffects == null)
            return;

        if (isBoosting)
        {
            foreach (var fx in planeEffects)
                if (fx != null && !fx.isPlaying)
                    fx.Play();
        }
        else
        {
            foreach (var fx in planeEffects)
                if (fx != null && fx.isPlaying)
                    fx.Stop();
        }
    }
}
