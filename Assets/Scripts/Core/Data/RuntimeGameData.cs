using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
public class RuntimeGameData
{
    public event System.Action OnDataChanged;

    public int Level;
    public string LevelName;
    public string LevelDescription;

    public List<GameObject> Upgrades;

    #region Run Stats
    //will be saved
    private float _money;
    public float Money
    {
        get => _money;
        set { _money = value; OnDataChanged?.Invoke(); }
    }

    //will be saved
    public float _overallScore;
    public float OverallScore
    {
        get => _overallScore;
        set { _overallScore = value; OnDataChanged?.Invoke(); }
    }

    //will be saved
    public float _runTime;
    public float RunTime
        {
        get => _runTime;
        set { _runTime = value; OnDataChanged?.Invoke(); }
    }

    //will be saved
    public float _enemiesKilled;
    public float EnemiesKilled
    {
        get => _enemiesKilled;
        set { _enemiesKilled = value; OnDataChanged?.Invoke(); }
    }
#endregion

    #region Player Stats
    private int _playerHealth;
    public int PlayerHealth
    {
        get => _playerHealth;
        set { _playerHealth = value; OnDataChanged?.Invoke(); }
    }

    private int _playerShield;
    public int PlayerShild
    {
        get => _playerShield;
        set { _playerShield = value; OnDataChanged?.Invoke(); }
    }
    #endregion

    public GameObject Item;
}
