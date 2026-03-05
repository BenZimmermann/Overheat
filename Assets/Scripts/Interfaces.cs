using UnityEngine;


public interface IDamageable
{
    void TakeDamage(float amount, float Source);
}

public enum DamageType
{
    Player,
    Enemy,
    Environment
}