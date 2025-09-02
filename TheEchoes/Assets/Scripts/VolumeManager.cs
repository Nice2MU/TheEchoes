using UnityEngine;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager instance;

    [Header("Default Volume Levels")]
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
        }

        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        ApplyVolumeSettings();
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void ApplyVolumeSettings()
    {
        AudioListener.volume = masterVolume;

        if (SoundManager.instance != null)
        {
            if (SoundManager.instance.effectSource != null)
                SoundManager.instance.effectSource.volume = sfxVolume;

            if (SoundManager.instance.distanceSource != null)
                SoundManager.instance.distanceSource.volume = sfxVolume;

            if (SoundManager.instance.uiSource != null)
                SoundManager.instance.uiSource.volume = sfxVolume;

            if (SoundManager.instance.musicSource != null)
                SoundManager.instance.musicSource.volume = musicVolume;
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }
}