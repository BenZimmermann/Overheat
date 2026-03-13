using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private List<Transform> _spawnPoints;

    [SerializeField] private List<UpgradeController> _upgradePool;

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
            Debug.LogWarning("[ShopController] No spawn points assigned.");
            return;
        }

        if (_upgradePool == null || _upgradePool.Count == 0)
        {
            Debug.LogWarning("[ShopController] Upgrade pool is empty.");
            return;
        }

        List<UpgradeData> alreadyOwned = GameManager.Instance.Data.Upgrades;

        List<UpgradeController> available = new();
        foreach (UpgradeController upgradeController in _upgradePool)
        {
            if (upgradeController != null && upgradeController.Data != null && !alreadyOwned.Contains(upgradeController.Data))
                available.Add(upgradeController);
        }

        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            if (available.Count == 0)
            {
                Debug.LogWarning("[ShopController] Not enough upgrades left to fill all spawn points.");
                break;
            }

            Transform spawnPoint = _spawnPoints[i];
            if (spawnPoint == null) continue;

            UpgradeController selectedController = PickByRarity(available);
            available.Remove(selectedController);

            GameObject spawned = Instantiate(
                selectedController.gameObject,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint
            );

            _spawnedObjects.Add(spawned);
        }
    }

    private UpgradeController PickByRarity(List<UpgradeController> pool)
    {
        float totalWeight = 0f;
        List<(UpgradeController ctrl, float weight)> weighted = new();

        foreach (UpgradeController ctrl in pool)
        {
            float w = GetRarityWeight(ctrl.Data.Rarity);
            weighted.Add((ctrl, w));
            totalWeight += w;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach ((UpgradeController ctrl, float weight) in weighted)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return ctrl;
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