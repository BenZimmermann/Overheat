using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// use navmesh surface
/// gets activated by RoomTrigger, spawns waves of enemies, and opens exit doors when all waves are cleared. It also handles replayability if enabled.
/// </summary>
#region Serializable
[System.Serializable]
public class SpawnPointData
{
    public Transform spawnPoint;
    public GameObject enemyPrefab;
}

[System.Serializable]
public class Wave
{
    public List<SpawnPointData> spawns;
}
#endregion
public class RoomController : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private List<GameObject> _entryDoors;
    [SerializeField] private List<GameObject> _exitDoors;

    [Header("Waves")]
    [SerializeField] private List<Wave> _waves;

    [Header("Settings")]
    [SerializeField] private bool _canReplay = false;

    private List<EnemyController> _aliveEnemies = new();
    private int _currentWave = 0;
    public bool _activated;


    public void ActivateRoom()
    {
        if (_activated && !_canReplay) return;

        _activated = true;
        _currentWave = 0;

        CloseEntryDoors();
        CloseExitDoors();

        StartWave();
    }
    //starts the current wave of enemies. If all waves have been completed, it opens the exit doors.
    private void StartWave()
    {
        if(_currentWave >=  _waves.Count)
        {
            OpenExitDoors();
            return;
        }
        SpawnWave(_waves[_currentWave]);
    }
    //spawns enemies based on the provided wave data, and adds them to the list of alive enemies.
    private void SpawnWave(Wave wave)
    {
        foreach (var spawn in wave.spawns)
        {
            if (spawn.enemyPrefab == null || spawn.spawnPoint == null) continue;

            GameObject enemy = Instantiate(
                spawn.enemyPrefab,
                spawn.spawnPoint.position,
                spawn.spawnPoint.rotation,
                transform
            );

            EnemyController ctrl = enemy.GetComponent<EnemyController>();

            if (ctrl != null)
            {
                ctrl.SetRoom(this);
                _aliveEnemies.Add(ctrl);
            }
        }
    }
    //called by enemies when they die, removes the enemy from the list of alive enemies and checks if the wave is cleared.
    public void OnEnemyDied(EnemyController enemy)
    {
        _aliveEnemies.Remove(enemy);

        if (_aliveEnemies.Count == 0)
        {
            _currentWave++;
            StartWave();
        }
    }
    #region door control
    private void CloseEntryDoors()
    {
        foreach (GameObject door in _entryDoors)
            door?.SetActive(true);
    }
    private void CloseExitDoors()
    {
        foreach (GameObject door in _exitDoors)
            door?.SetActive(true);
    }
    private void OpenExitDoors()
    {
        foreach (GameObject door in _exitDoors)
            door?.SetActive(false);
    }
    #endregion
}
