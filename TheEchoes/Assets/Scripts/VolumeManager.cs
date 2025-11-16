using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VolumeManager : MonoBehaviour
{
    public static VolumeManager instance;

    [Header("Default Volume Levels")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;

    [Header("Optional UI Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
            SyncSlidersFromValues();
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        ApplyVolumeSettings();
        AddSliderListeners();
    }

    private void OnDisable()
    {
        RemoveSliderListeners();
        SaveVolumeSettings();
    }

    private void OnApplicationQuit()
    {
        SaveVolumeSettings();
    }

    public void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        ApplyVolumeSettings();
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void ApplyVolumeSettings()
    {
        AudioListener.volume = masterVolume;

        var sm = SoundManager.instance;
        if (sm != null)
        {
            if (sm.effectSource != null) sm.effectSource.volume = sfxVolume;
            if (sm.distanceSource != null) sm.distanceSource.volume = sfxVolume;
            if (sm.uiSource != null) sm.uiSource.volume = sfxVolume;
            if (sm.musicSource != null) sm.musicSource.volume = musicVolume;
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
        SyncSlidersFromValues();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
        SyncSlidersFromValues();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
        SyncSlidersFromValues();
    }

    public void Refresh() => LoadVolumeSettings();

    private void AddSliderListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
    }

    private void RemoveSliderListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
    }

    private void SyncSlidersFromValues()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);
    }
}