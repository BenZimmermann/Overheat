using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemShopController : MonoBehaviour, IDamageable, IShopEntry
{
    [SerializeField] private ItemData _data;
    [SerializeField] private TMP_Text _itemName;

    private float _currentHealth = 1;
    private static bool _isPurchasing;

    public ItemData Data => _data;

    // IShopEntry
    public float Cost => _data.cost;
    public RarityType Rarity => _data.Rarity;
    private void Start()
    {
        _itemName.text = _data.itemName.ToString();
    }
    public void TakeDamage(float amount, string source)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0)
            TryBuyItem();
    }
    // Tries to buy the item, if player has enough money it applies the item effects to player stats and destroys the gameobject, if not it resets health to 1 so player can try again
    private void TryBuyItem()
    {
        if (_data == null) return;
        if (_isPurchasing) return;
        _isPurchasing = true;
        SoundManager.Instance.Play3DSound(SoundType.BuyItem, transform.position);
        RuntimeGameData runtimeData = GameManager.Instance.Data;

        if (runtimeData.Money < _data.cost)
        {
            _isPurchasing = false;
            _currentHealth = 1;
            return;
        }

        BuyItem(runtimeData);
    }
    // Buys the item, applies the item effects to player stats and destroys the gameobject
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

