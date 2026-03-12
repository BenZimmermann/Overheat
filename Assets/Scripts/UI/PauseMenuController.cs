using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] GameObject PauseMenu;
    [SerializeField] SettingsController settingsController;
    [SerializeField] GameObject SettingsMenu;

    [Header("Input Settings")]
    public InputActionAsset inputActionAsset;
    public string actionMapName = "Player";
    public string pauseActionName = "Pause";

    private bool _gameOver;
    InputAction _pauseAction;

    void BindInputActions()
    {
        if (inputActionAsset == null) return;
        var map = inputActionAsset.FindActionMap(actionMapName, false);

        if (map == null) return;
        _pauseAction = map.FindAction(pauseActionName, false);

        map.Enable();
    }

    private void OnEnable() 
    { 
        BindInputActions();
        GameManager.Instance.OnGameOver += DisablePauseGame;
    }
    private void OnDisable() 
    { 

        inputActionAsset?.FindActionMap(actionMapName, false)?.Disable();
        GameManager.Instance.OnGameOver -= DisablePauseGame;
    }


    void Update()
    {

        if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
        //if(GameStateManager.Instance.CurrentState == GameState.paused)
        //{ ResumeGame(); }
            PauseGame();
    }
    public void PauseGame()
    {
        if (_gameOver) return;
        PauseMenu.SetActive(true);
        GameManager.Instance.PauseGame();
        //gamestateManager.SetState(GameState.Paused);
    }
    private void DisablePauseGame()
    {
        _gameOver = true;
    }
    public void QuitGame()
    {
        _gameOver = false;
        GameManager.Instance.QuitToMainMenu();
        SaveManager.Instance.SaveStats();
    }

    public void ResumeGame()
    {
        PauseMenu.SetActive(false);
        GameManager.Instance.ResumeGame();
        //gamestateManager.SetState(GameState.Playing);
    }
    public void SettingsPressed()
    {
        settingsController.OpenMenu(SettingsMenu);
    }
}
