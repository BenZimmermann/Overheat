using SUPERCharacter;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
public class UpgradeController : MonoBehaviour, IDamageable, IShopEntry
{
    [SerializeField] private UpgradeData _data;
    private float _currentHealth = 1;

    public float Cost => _data.cost;
    public RarityType Rarity =>  _data.Rarity;

    [SerializeField] private TMP_Text _UpgradeName;
    [SerializeField] private TMP_Text _UpgradeDescription;
    [SerializeField] private GameObject _Icon;



    public UpgradeData Data => _data;

    private void Start()
    {
        _UpgradeName.text = _data.upgradeName.ToString();
        _UpgradeDescription.text = _data.upgradeDescription.ToString();

        SpriteRenderer iconRenderer = _Icon.GetComponent<SpriteRenderer>();
        if (iconRenderer != null)
            iconRenderer.sprite = _data.upgradeIcon;
    }
    public void TakeDamage(float amount, string source)
    {

        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            TryBuyUpgrade();
        }
    }

    private void TryBuyUpgrade()
    {
        if (_data == null) return;
        SoundManager.Instance.Play3DSound(SoundType.BuyItem, transform.position);
        RuntimeGameData Data = GameManager.Instance.Data;

        if (Data == null)
        {
            Debug.LogWarning("[UpgradeController] RuntimeGameData not found.");
            return;
        }

        if (Data.Money < _data.cost)
        {
            Debug.Log($"[UpgradeController] Not enough money to buy '{_data.upgradeName}'. " +
                      $"Cost: {_data.cost} | Money: {Data.Money}");

            _currentHealth = 1;
            return;
        }

        BuyUpgrade(Data);
    }

    private void BuyUpgrade(RuntimeGameData runtimeData)
    {
        runtimeData.Money -= _data.cost;
        runtimeData.AddUpgrade(_data);

        ApplyUpgrade(runtimeData);

        Debug.Log($"[UpgradeController] Bought: {_data.upgradeName} for {_data.cost}. " +
                  $"Remaining money: {runtimeData.Money}");

        Destroy(gameObject);
    }
    private void ApplyUpgrade(RuntimeGameData runtimeData)
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        SUPERCharacterAIO playerController = FindAnyObjectByType<SUPERCharacterAIO>();
        //CollectableData collectableData = new CollectableData();

        switch (_data.type)
        {
            case UpgradeType.moreHealth:
                if (playerHealth != null)
                    playerHealth.AddMaxHealth(_data.value);
                break;
            case UpgradeType.moreShield:
                if (playerHealth != null)
                    playerHealth.AddMaxShield(_data.value);
                break;
            case UpgradeType.collectMoreMoney:
                if(runtimeData != null)
                runtimeData.AttractRadiusBonus += _data.value;
                break;
            case UpgradeType.higherJump:
                runtimeData.JumpPowerBonus += _data.value;
                if (playerController != null)
                    playerController.jumpPower += _data.value;
                break;
            case UpgradeType.fastRun:
                runtimeData.SpeedBonus += _data.value;
                if(playerController != null)
                    playerController.walkingSpeed += _data.value;
                break;
            case UpgradeType.fastDash:
                runtimeData.DashBonus += _data.value;
                if(playerController != null)
                    playerController.dashDuration += _data.value;
                break;
            case UpgradeType.fastSlide:
                runtimeData.SlideBonus += _data.value;
                if(playerController != null)
                    playerController.slidingDeceleration += _data.value;
                break;
            case UpgradeType.fastReload:
                if (runtimeData != null)
                    runtimeData.FastReload += _data.value;
                break;
            case UpgradeType.fastFire:
                if(runtimeData != null)
                    runtimeData.FastFire += _data.value;
                break;
            case UpgradeType.lessMeeleCooldown:
                if(runtimeData != null)
                    runtimeData.LessMeeleCooldown += _data.value;
                break;
            case UpgradeType.moreMeeleDistance:
                if(runtimeData != null)
                    runtimeData.MoreMeeleDistance += _data.value;
                break;
            case UpgradeType.lifesteal:
                if (playerHealth != null)
                    playerHealth.AddLifesteal(_data.value); ;
                break;
            default:
                Debug.Log("nothing to apply");
                break;
        }
    }
}