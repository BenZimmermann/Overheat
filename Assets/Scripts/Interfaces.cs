using UnityEngine;


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
public enum DamageType
{
    Player,
    Enemy,
    Environment
}