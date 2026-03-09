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
        //spawn GameManager
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void StartGame()
    {
        SceneManager.LoadScene("TestScene");
    }
}
