using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    /// <summary>
    /// singleton instance of the SaveManager, accessible from anywhere in the code
    /// loads and saves the SaveData to a json file in the persistent data path, which is a platform-specific location for storing data that persists between sessions
    /// </summary>
    
    public static SaveManager Instance { get; private set; }
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public SaveData _data = new SaveData();
    public SaveData Data => _data;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }
    // This method saves the player's stats at the end of a run, comparing current run data with saved data and updating if the current run has better stats. It then writes the updated data to a JSON file.
    public void SaveStats()
    {
        var run = GameManager.Instance.Data;

        if (run.Money > _data.Money) _data.Money = run.Money;
        if (run.OverallScore > _data.FinalScore) _data.FinalScore = run.OverallScore;
        _data.Time = 0;
        if (run.EnemiesKilled > _data.EnemiesKilled) _data.EnemiesKilled = (int)run.EnemiesKilled;
       
        string json = JsonUtility.ToJson(_data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Stats saved.");
    }
    // This method is similar to SaveStats but is specifically called at the end of a game, ensuring that the final stats are saved. It updates the saved data with the current run's stats if they are better and writes it to a JSON file.
    public void SaveStatsFinishGame()
        {
        var run = GameManager.Instance.Data;
        if (run.Money > _data.Money) _data.Money = run.Money;
        if (run.OverallScore > _data.FinalScore) _data.FinalScore = run.OverallScore;
        if (run.RunTime > _data.Time) _data.Time = run.RunTime;  
        if (run.EnemiesKilled > _data.EnemiesKilled) _data.EnemiesKilled = (int)run.EnemiesKilled;
        string json = JsonUtility.ToJson(_data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Stats saved.");
    }
    public void LoadStats()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            _data = JsonUtility.FromJson<SaveData>(json);

            var run = GameManager.Instance.Data;
            run.Money = _data.Money;
            run.OverallScore = _data.FinalScore;
            run.RunTime = _data.Time;
            run.EnemiesKilled = _data.EnemiesKilled;
        }
        else
        {
            _data = new SaveData();
        }
    }

    public void LoadSettings() 
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            _data = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            _data = new SaveData();
        }
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(_data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
    }

}
