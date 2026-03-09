using UnityEngine;
using System.Collections.Generic; 

public class SettingsController : MonoBehaviour
{
    /// <summary>
    /// TODO
    /// -save and load settings
    /// -control scheme
    /// -setting for disable specific UI elements
    /// </summary>
    [Header("Menu References")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject soundMenu;
    [SerializeField] private GameObject mouseMenu;

    private Stack<GameObject> _menuStack = new Stack<GameObject>();
    private GameObject _currentMenu;

    private void Start()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        soundMenu.SetActive(false);
        mouseMenu.SetActive(false);
        ShowMenu(mainMenu, false);
    }

    public void OpenMenu(GameObject targetMenu)
    {
        Debug.Log("Öffne: " + targetMenu.name + " | Stack size: " + _menuStack.Count);
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
}

