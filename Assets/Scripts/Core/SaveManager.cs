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
        LoadStats();
    }
    public void SaveStats()
    { 
        string json = JsonUtility.ToJson(_data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Gespeichert nach: " + SavePath);
    }
    public void LoadStats()
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
        SaveStats();
        Debug.Log("Settings saved.");
    }

}
