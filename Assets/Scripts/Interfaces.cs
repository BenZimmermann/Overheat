using UnityEngine;


public interface IDamageable
{
    void TakeDamage(float amount, string Source);
}

public enum DamageType
{
    Player,
    Enemy,
    Environment
}