using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string itemID;
    public string itemDescription;

    public Sprite itemIcon;
    public GameObject itemModel;
    public GameObject itemGrenadeModel;

    public bool isAutomatic;
    public bool isInputRequired;

    public float effectDuration;
    public float effectRadius;
    public float explosionDamage;
    public float shieldRegen;
    public float healthRegen;
    
    public ItemType itemType;

    public float cost;
    public RarityType Rarity;
}

public enum ItemType
{
    Revive,
    Grenade,
    magneticField,
    GoldenGun,
    Teleport,
    Elexir
}