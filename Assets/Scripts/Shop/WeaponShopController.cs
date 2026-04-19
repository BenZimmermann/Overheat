using TMPro;
using UnityEngine;

public class WeaponShopController : MonoBehaviour, IDamageable, IShopEntry
{
    [SerializeField] private WeaponData _data;
    [SerializeField] private TMP_Text _itemName;

    private float _currentHealth = 1;
    private Transform _weaponPivot;
    private static bool _isPurchasing;

    public float Cost => _data.cost;
    public RarityType Rarity => _data.Rarity ;

    private void Start()
    {
        _weaponPivot = GameObject.FindGameObjectWithTag("weaponPivot")?.transform;
        _itemName.text = _data.weaponName.ToString();
    }

    public void TakeDamage(float amount, string source)
    {
        _currentHealth -= amount;

        if (_currentHealth <= 0)
            TryBuyWeapon();
    }

    private void TryBuyWeapon()
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

        BuyWeapon(runtimeData);
    }

    private void BuyWeapon(RuntimeGameData runtimeData)
    {
        runtimeData.Money -= _data.cost;
        runtimeData.CurrentWeapon = _data;

        SwitchWeapon(_data);
        Destroy(gameObject);
    }

    private void SwitchWeapon(WeaponData currentData)
    {
        if (_data?.WeaponObj == null) return;

        Transform pivot = GameObject.FindGameObjectWithTag("weaponPivot")?.transform;
        if (pivot == null)
        {
            Debug.LogWarning("[WeaponShopController] WeaponPivot not found.");
            return;
        }

        if (_weaponPivot.childCount > 0)
            Destroy(_weaponPivot.GetChild(0).gameObject);

        GameObject newWeapon = Instantiate(_data.WeaponObj, _weaponPivot);
        newWeapon.transform.localPosition = _data.WeaponObj.transform.localPosition;
        newWeapon.transform.localRotation = _data.WeaponObj.transform.localRotation;
    }
    private void OnDestroy()
    {
        _isPurchasing = false;
    }
}