using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verwaltet einen Raum: Türen schließen, Enemies spawnen, Türen öffnen.
/// </summary>
public class RoomController : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private List<GameObject> _doors;

    [Header("Spawn")]
    [SerializeField] private List<Transform> _spawnPoints;
    [SerializeField] private List<GameObject> _enemyPrefabs;

    private List<EnemyController> _aliveEnemies = new();
    private bool _activated;

    private void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        ActivateRoom();
    }

    private void ActivateRoom()
    {
        _activated = true;
        CloseDoors();
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        foreach (Transform spawnPoint in _spawnPoints)
        {
            if (_enemyPrefabs.Count == 0) break;

            GameObject prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Count)];
            GameObject enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);

            EnemyController ctrl = enemy.GetComponent<EnemyController>();
            if (ctrl != null)
                _aliveEnemies.Add(ctrl);
        }
    }

    public void OnEnemyDied(EnemyController enemy)
    {
        _aliveEnemies.Remove(enemy);

        if (_aliveEnemies.Count == 0)
            OpenDoors();
    }

    private void CloseDoors()
    {
        foreach (GameObject door in _doors)
            door?.SetActive(true);
    }

    private void OpenDoors()
    {
        foreach (GameObject door in _doors)
            door?.SetActive(false);
    }
}
