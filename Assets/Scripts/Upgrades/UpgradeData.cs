using UnityEngine;


[CreateAssetMenu(menuName = "Upgrade/Upgrade")]
public class UpgradeData : ScriptableObject
{

    public string upgradeName;
    public string id;
    public string upgradeDescription;
    public Sprite upgradeIcon;
    public GameObject UpgradeObj;

    public float cost;

    public RarityType Rarity;
    public UpgradeType type;

    public float value;
}

public enum UpgradeType
{
    fastFire,
    fastReload,
    lessMeeleCooldown,
    moreMeeleDistance,
    fastSlide,
    fastDash,
    fastRun,
    higherJump,
    collectMoreMoney,
    moreHealth,
    moreShield,
    lifesteal,
}
public enum RarityType
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
