using UnityEngine;


[System.Serializable]
public class SaveData
{
    // RunData

    public float Time;
    public int EnemiesKilled;
    public int Money;
    public int FinalScore;

    // Settings/Sound
    public float MasterVolume;
    public float MusicVolume;
    public float SFXVolume;

    // Settings/Mouse
    public float MouseSensitivity;
    public bool InvertX;
    public bool InvertY;

    
    public bool showUpgrades;
    public bool showStats;
    public bool showHealth;

    public SaveData()
    {
        Time = 0;
        EnemiesKilled = 0;
        Money = 0;
        FinalScore = 0;
        MasterVolume = 1;
        MusicVolume = 1;
        SFXVolume = 1;
        MouseSensitivity = 1;
        InvertX = false;
        InvertY = false;
        showUpgrades = true;
        showStats = true;
        showHealth = true;
    }
}
