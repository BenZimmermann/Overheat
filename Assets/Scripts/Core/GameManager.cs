using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //[SerializeField] GameObject LevelManager;
    //[SerializeField] GameObject SaveManger;
    //[SerializeField] GameObject HUDController;
    //[SerializeField] GameObject PlayerPrefab;

    /// <summary>
    /// 1. gameManager instance savemanager. 
    /// 2. gameManager isntance GameStateManager.
    /// 3. gameManager sets current settings form savemanager.
    /// 4. gameManager instance levelmanager.
    /// ----
    /// HUD and Player aleady exist in the scenes, the hud manager will be filled with the player stats and upgrades, when the scene changes
    /// </summary>
    public static GameManager Instance { get; private set; }

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
        //spawn saveManager
        //spawn GameStateManager
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void QuitToMainMenu()
    {        
        SceneManager.LoadScene("StartScreen");
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        Debug.Log("Starting Game...");
        Cursor.lockState = CursorLockMode.Locked;
        SceneManager.LoadScene("TestScene");
    }
    public void PauseGame()
    {     
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
    }
    public void ResumeGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
    }
}
