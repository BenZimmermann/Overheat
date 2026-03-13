using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
public class RuntimeGameData
{
    public event System.Action OnDataChanged;
    public event System.Action<UpgradeData> OnUpgradeAdded;


    public int Level;
    public string LevelName;
    public string LevelDescription;

    public List<UpgradeData> Upgrades = new List<UpgradeData>();
    #region shop
    public void AddUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null) return;

        Upgrades.Add(upgrade);
        OnUpgradeAdded?.Invoke(upgrade);
        OnDataChanged?.Invoke();

        //Debug.Log($"[RuntimeGameData] Upgrade added: {upgrade.upgradeName} | Total upgrades: {Upgrades.Count}");
    }
    #endregion
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

    public float AttractRadiusBonus;
    public float JumpPowerBonus;
    public float SpeedBonus;
    public float DashBonus;
    public float SlideBonus;

    //later no gameobjects -> will be destroyed by scene switch
    public GameObject Item;

    public GameObject Weapon;
}
