using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class SettingsManager : MonoBehaviour
{

    public static SettingsManager Instance { get; private set; }

    [SerializeField] private AudioMixer _audioMixer;
    //[SerializeField] private SaveManager saveManager; //later via singleton

    public event System.Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        ApplySettings();
    }
    public void ApplySettings()
    { 
        var data = SaveManager.Instance.Data;
        SetVolume(data.MasterVolume, data.MusicVolume, data.SFXVolume);
        OnSettingsChanged?.Invoke();
    }
    public void SetVolume(float master, float music, float sfx)
    {
        _audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(master, 0.0001f)) * 20);
        _audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(music, 0.0001f)) * 20);
        _audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(sfx, 0.0001f)) * 20);
    }
    public void SetMasterVolume(Slider caller)
    {
        var data = SaveManager.Instance.Data;

        data.MasterVolume = caller.value;

        SetVolume(data.MasterVolume, data.MusicVolume, data.SFXVolume);

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    public void SetMusicVolume(Slider caller)
    {
        var data = SaveManager.Instance.Data;

        data.MusicVolume = caller.value;

        SetVolume(data.MasterVolume, data.MusicVolume, data.SFXVolume);

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    public void SetSFXVolume(Slider caller)
    {
        var data = SaveManager.Instance.Data;

        data.SFXVolume = caller.value;

        SetVolume(data.MasterVolume, data.MusicVolume, data.SFXVolume);

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void SetMouseSensitivity(float value)
    {
        SaveManager.Instance.Data.MouseSensitivity = value;

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ToggleInvertX(bool value)
    {
        SaveManager.Instance.Data.InvertX = value;

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ToggleInvertY(bool value)
    {
        SaveManager.Instance.Data.InvertY = value;

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ToggleShowUpgrades(bool value)
    {
        SaveManager.Instance.Data.showUpgrades = value;
        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    public void ToggleShowStats(bool value)
    {
        SaveManager.Instance.Data.showStats = value;
        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    public void ToggleShowHealth(bool value)
    {
        SaveManager.Instance.Data.showHealth = value;
        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    //settings change event
}
