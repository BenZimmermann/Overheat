using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    //[SerializeField] GameObject LevelManager;
    //[SerializeField] GameObject SaveManger;
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

    public RuntimeGameData Data = new RuntimeGameData();

    public event System.Action OnGameOver;
    public event System.Action OnFinishGame;

    public float _runTime;
    private bool _running = false;
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
    void Update()
    {
        if (_running)
        {
            _runTime += Time.deltaTime;
            Data.RunTime = _runTime;
            Highscore();
        }
        
    }
    #region Pause/Resume/Quit
    public void QuitGame()
    {
        Application.Quit();
    }
    public void QuitToMainMenu()
    {
        EndRun();
        SceneManager.LoadScene("StartScreen");
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        //reset game data so when you start a new game, you start fresh
        Data = new RuntimeGameData();
        //load the first level
        SceneManager.LoadScene("Level_1");
        ResumeGame();
        StartRun();
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
    #endregion

    #region Highscore calc (short for Calculator im just using slang!)
    public void Highscore()
    {
        var run = Data;

        float killScore = run.EnemiesKilled * 100f;
        float moneyScore = run.Money * 0.5f;

        run.OverallScore = killScore + moneyScore;

        Debug.Log($"Score: {run.OverallScore} | Kills: {killScore} | Money: {moneyScore}");
    }
    #endregion

    #region RunTime
    public void StartRun()
    {
        _runTime = 0f;
        _running = true;
    }

    public void EndRun()
    {
        _running = false;
    }

    public void FinishGame()
    {
        EndRun();
        PauseGame();
        SaveManager.Instance.SaveStatsFinishGame();
        OnFinishGame?.Invoke();
        //invoke game finish event for GameFinishMenu
    }
    #endregion

    #region gameOver
    public void GameOver()
    {
        if (TryRevive()) return;

        EndRun();
        PauseGame();
        OnGameOver?.Invoke();
        //invoke game over event for GameOverMenu
    }
    #endregion
    private bool TryRevive()
    {
        ItemData item = Data.CurrentItem;
        if (item == null || item.itemType != ItemType.Revive) return false;

        ItemController itemUsage = FindAnyObjectByType<ItemController>();
        if (itemUsage == null) return false;

        itemUsage.UseRevive(item);
        Debug.Log("[GameManager] Spieler wiederbelebt!");
        return true;
    }
}
