using UnityEngine;

/// <summary>
/// collection of all interfaces
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount, string Source);
}
public interface ICollectable
{
    void Collect(float amount);
}
public interface IHealable
{
    void GiveHeal(float amount);
}
public interface IShopEntry
{
    float Cost { get; }
    RarityType Rarity { get; }
    }
public enum DamageType
{
    Player,
    Enemy,
    Environment
}