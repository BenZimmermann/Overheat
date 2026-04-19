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
        ApplyLevel("MasterVolume", master);
        ApplyLevel("MusicVolume", music);
        ApplyLevel("SFXVolume", sfx);
    }
    private void ApplyLevel(string parameterName, float sliderValue)
    {
        if (sliderValue <= 0.001f)
        {
            _audioMixer.SetFloat(parameterName, -80f);
        }
        else
        {
            float dB = Mathf.Log10(sliderValue) * 20;
            _audioMixer.SetFloat(parameterName, dB);
        }
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
