using UnityEngine;

/// <summary>
/// this script is attached to the enemy's head and allows it to take damage separately from the rest of the body.
/// </summary>
public class EnemyHead : MonoBehaviour, IDamageable
{
    private EnemyController _controller;

    private void Awake()
    {
        _controller = GetComponentInParent<EnemyController>();
    }

    public void TakeDamage(float amount, string source)
    {
        _controller?.TakeHeadshotDamage(amount, source);
    }
}
