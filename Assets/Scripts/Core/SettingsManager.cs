using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class SettingsManager : MonoBehaviour
{

    public static SettingsManager Instance { get; private set; }

    [SerializeField] private AudioMixer _audioMixer;

    public event System.Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    public void SetMouseSensitivity(Slider caller)
    {
        SaveManager.Instance.Data.MouseSensitivity = caller.value;

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ToggleInvertX(Toggle value)
    {
        SaveManager.Instance.Data.InvertX = value.isOn;

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ToggleInvertY(Toggle value)
    {
        SaveManager.Instance.Data.InvertY = value.isOn;

        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ToggleShowUpgrades(Toggle value)
    {
        SaveManager.Instance.Data.showUpgrades = value.isOn;
        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    public void ToggleShowStats(Toggle value)
    {
        SaveManager.Instance.Data.showStats = value.isOn;
        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    public void ToggleShowHealth(Toggle value)
    {
        SaveManager.Instance.Data.showHealth = value.isOn;
        SaveManager.Instance.SaveSettings();
        OnSettingsChanged?.Invoke();
    }
    //settings change event
}
