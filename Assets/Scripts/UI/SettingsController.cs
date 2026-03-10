using System.Collections.Generic; 
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
public class SettingsController : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject soundMenu;
    [SerializeField] private GameObject mouseMenu;
    [SerializeField] private GameObject keybindsMenu;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Mouse Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Toggle invertX;
    [SerializeField] private Toggle invertY;

    [Header("Keybinds Settings")]
    [SerializeField] private Toggle hideHealth;
    [SerializeField] private Toggle hideUpgrades;
    [SerializeField] private Toggle hideStats;

    private Stack<GameObject> _menuStack = new Stack<GameObject>();
    private GameObject _currentMenu;

    private void Start()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        soundMenu.SetActive(false);
        mouseMenu.SetActive(false);
        keybindsMenu.SetActive(false);
        ShowMenu(mainMenu, false);
    }

    public void OpenMenu(GameObject targetMenu)
    {
        UpdateMenuSettings();
        ShowMenu(targetMenu, true);
    }

    private void ShowMenu(GameObject targetMenu, bool addToStack)
    {
        if (targetMenu == null || targetMenu == _currentMenu) return;

        if (_currentMenu != null)
        {
            if (addToStack)
            {
                _menuStack.Push(_currentMenu);
            }
            _currentMenu.SetActive(false);
        }

        _currentMenu = targetMenu;
        _currentMenu.SetActive(true);
    }

    public void Back()
    {
        if (_menuStack.Count > 0)
        {
            if (_currentMenu != null)
            {
                _currentMenu.SetActive(false);
            }
            _currentMenu = _menuStack.Pop();
            _currentMenu.SetActive(true);
        }
    }
    private void UpdateMenuSettings()
    {
        var data = SaveManager.Instance.Data;

        masterVolumeSlider.value = data.MasterVolume;
        musicVolumeSlider.value = data.MusicVolume;
        sfxVolumeSlider.value = data.SFXVolume;

        mouseSensitivitySlider.value = data.MouseSensitivity;
        invertX.isOn = data.InvertX;
        invertY.isOn = data.InvertY;

        hideHealth.isOn = data.showHealth;
        hideUpgrades.isOn = data.showUpgrades;
        hideStats.isOn = data.showStats;
    }
    public void SetMasterVolume(Slider caller)
    {
        SettingsManager.Instance.SetMasterVolume(caller);
    }

    public void SetMusicVolume(Slider caller)
    {
        SettingsManager.Instance.SetMusicVolume(caller);
    }

    public void SetSFXVolume(Slider caller)
    {
        SettingsManager.Instance.SetSFXVolume(caller);
    }

    public void SetSensitivity(Slider caller)
    {
        SettingsManager.Instance.SetMouseSensitivity(caller);
    }

    public void ToggleInvertX(Toggle value)
    {
        SettingsManager.Instance.ToggleInvertX(value);
    }

    public void ToggleInvertY(Toggle value)
    {
        SettingsManager.Instance.ToggleInvertY(value);
    }

    public void ToggleShowUpgrades(Toggle value)
    {
        SettingsManager.Instance.ToggleShowUpgrades(value);
    }
    public void ToggleShowStats(Toggle value)
    {
        SettingsManager.Instance.ToggleShowStats(value);
    }
    public void ToggleShowHealth(Toggle value)
    {
        SettingsManager.Instance.ToggleShowHealth(value);
    }
}

