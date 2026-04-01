using UnityEngine;

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
