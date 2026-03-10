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

    InputAction _pauseAction;

    void BindInputActions()
    {
        if (inputActionAsset == null) return;
        var map = inputActionAsset.FindActionMap(actionMapName, false);

        if (map == null) return;
        _pauseAction = map.FindAction(pauseActionName, false);

        map.Enable();
    }

    private void OnEnable() { BindInputActions(); }
    private void OnDisable() { inputActionAsset?.FindActionMap(actionMapName, false)?.Disable(); }


    void Update()
    {

        if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
        //if(GameStateManager.Instance.CurrentState == GameState.paused)
        //{ ResumeGame(); }
            PauseGame();
    }
    public void PauseGame()
    {
        PauseMenu.SetActive(true);
        GameManager.Instance.PauseGame();
        //gamestateManager.SetState(GameState.Paused);
    }
    public void QuitGame()
    {
        GameManager.Instance.QuitToMainMenu();
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
