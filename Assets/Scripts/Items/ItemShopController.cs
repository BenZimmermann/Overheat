using UnityEngine;
using UnityEngine.InputSystem;

public class ItemShopController : MonoBehaviour, IDamageable, IShopEntry
{
    [SerializeField] private ItemData _data;

    private float _currentHealth = 1;
    private static bool _isPurchasing;

    public ItemData Data => _data;

    // IShopEntry
    public float Cost => _data.cost;
    public RarityType Rarity => _data.Rarity;

    public void TakeDamage(float amount, string source)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0)
            TryBuyItem();
    }

    private void TryBuyItem()
    {
        if (_data == null) return;
        if (_isPurchasing) return;
        _isPurchasing = true;

        RuntimeGameData runtimeData = GameManager.Instance.Data;

        if (runtimeData.Money < _data.cost)
        {
            _isPurchasing = false;
            _currentHealth = 1;
            return;
        }

        BuyItem(runtimeData);
    }

    private void BuyItem(RuntimeGameData runtimeData)
    {
        runtimeData.Money -= _data.cost;
        runtimeData.SetItem(_data);
        Debug.Log(_data.itemName);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _isPurchasing = false;
    }
}

