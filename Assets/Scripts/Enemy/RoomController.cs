using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// -beinhaltet navmesh surface
/// -beinhaltet nur die logik für roomcontroller
/// -roomcontroller bekommt bescheid von RoomTrigger GetComponentInParent
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

    private void StartWave()
    {
        if(_currentWave >=  _waves.Count)
        {
            OpenExitDoors();
            return;
        }
        SpawnWave(_waves[_currentWave]);
    }
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
    public void OnEnemyDied(EnemyController enemy)
    {
        _aliveEnemies.Remove(enemy);

        if (_aliveEnemies.Count == 0)
        {
            _currentWave++;
            StartWave();
        }
    }

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
}
