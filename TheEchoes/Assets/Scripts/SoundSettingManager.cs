using UnityEngine;
using UnityEngine.UI;

public class SoundSettingManager : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    void Start()
    {
        masterVolumeSlider.onValueChanged.AddListener(VolumeManager.instance.SetMasterVolume);
        sfxVolumeSlider.onValueChanged.AddListener(VolumeManager.instance.SetSFXVolume);
        musicVolumeSlider.onValueChanged.AddListener(VolumeManager.instance.SetMusicVolume);

        masterVolumeSlider.value = VolumeManager.instance.masterVolume;
        sfxVolumeSlider.value = VolumeManager.instance.sfxVolume;
        musicVolumeSlider.value = VolumeManager.instance.musicVolume; 
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (SoundManager.instance != null)
        {
            if (SoundManager.instance.effectSource != null)
                SoundManager.instance.effectSource.volume = volume;

            if (SoundManager.instance.distanceSource != null)
                SoundManager.instance.distanceSource.volume = volume;
        }
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (SoundManager.instance != null)
            SoundManager.instance.musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
}