using SUPERCharacter;
using UnityEditor;
using UnityEngine;

public class UpgradeController : MonoBehaviour, IDamageable
{
    [SerializeField] private UpgradeData _data;
    private float _currentHealth = 1;


    public UpgradeData Data => _data;
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
            default:
                Debug.Log("nothing to apply");
                break;
        }
    }
}