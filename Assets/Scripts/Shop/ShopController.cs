using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private List<Transform> _spawnPoints;

    [SerializeField] private List<GameObject> _shopPool;

    [Header("Rarity Weights")]
    [SerializeField] private float _weightCommon = 50f;
    [SerializeField] private float _weightUncommon = 25f;
    [SerializeField] private float _weightRare = 15f;
    [SerializeField] private float _weightEpic = 8f;
    [SerializeField] private float _weightLegendary = 2f;

    private readonly List<GameObject> _spawnedObjects = new();

    private void Start()
    {
        SpawnShopItems();
    }

    public void RefreshShop()
    {
        ClearShop();
        SpawnShopItems();
    }

    public void ClearShop()
    {
        foreach (GameObject obj in _spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        _spawnedObjects.Clear();
    }

    private void SpawnShopItems()
    {
        if (_spawnPoints == null || _spawnPoints.Count == 0)
        {
            return;
        }

        if (_shopPool == null || _shopPool.Count == 0)
        {
            return;
        }

        List<UpgradeData> alreadyOwned = GameManager.Instance.Data.Upgrades;

        List<GameObject> available = new();
        foreach (GameObject obj in _shopPool)
        {
            if (obj == null) continue;

            IShopEntry entry = obj.GetComponent<IShopEntry>();
            if (entry == null) continue;

            if (obj.TryGetComponent(out UpgradeController upgradeCtrl))
            {
                if (upgradeCtrl.Data != null && alreadyOwned.Contains(upgradeCtrl.Data))
                    continue;
            }

            available.Add(obj);
        }

        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            if (available.Count == 0)
            {
                Debug.LogWarning("[ShopController] Not enough items left to fill all spawn points.");
                break;
            }

            Transform spawnPoint = _spawnPoints[i];
            if (spawnPoint == null) continue;

            GameObject selected = PickByRarity(available);
            available.Remove(selected);

            GameObject spawned = Instantiate(
                selected,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint
            );

            _spawnedObjects.Add(spawned);
        }
    }

    private GameObject PickByRarity(List<GameObject> pool)
    {
        float totalWeight = 0f;
        List<(GameObject obj, float weight)> weighted = new();

        foreach (GameObject obj in pool)
        {
            IShopEntry entry = obj.GetComponent<IShopEntry>();
            float w = entry != null ? GetRarityWeight(entry.Rarity) : _weightCommon;
            weighted.Add((obj, w));
            totalWeight += w;
        }
        //not my idea
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach ((GameObject obj, float weight) in weighted)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return obj;
        }

        return pool[pool.Count - 1];
    }

    private float GetRarityWeight(RarityType rarity)
    {
        switch (rarity)
        {
            case RarityType.Common: return _weightCommon;
            case RarityType.Uncommon: return _weightUncommon;
            case RarityType.Rare: return _weightRare;
            case RarityType.Epic: return _weightEpic;
            case RarityType.Legendary: return _weightLegendary;
            default: return _weightCommon;
        }
    }
}