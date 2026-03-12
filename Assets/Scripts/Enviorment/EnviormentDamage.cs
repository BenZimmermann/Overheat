using UnityEngine;

public class EnviormentDamage : MonoBehaviour
{
    // later SOs
    [SerializeField] private string _damageName;
    [SerializeField] private bool _damageOverTime;
    [SerializeField] private float _dotInterval = 0.5f;
    [SerializeField] private LayerMask _canDamage;
    [SerializeField] private float _damage;
    [SerializeField] private bool _useTrigger;

    private IDamageable _currentTarget;
    private float _dotTimer;

    private void OnCollisionEnter(Collision collision)
    {
        if (_useTrigger) return;

        TryApplyDamage(collision.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (_useTrigger) return;
        ClearTarget();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!_useTrigger) return;
        TryApplyDamage(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_useTrigger) return;
        ClearTarget();
    }

    private void TryApplyDamage(GameObject target)
    {
        if ((_canDamage.value & (1 << target.layer)) == 0) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return;

        if (_damageOverTime)
        {
            _currentTarget = damageable;
            _dotTimer = 0f;
        }
        else
        {
            damageable.TakeDamage(_damage, _damageName);
        }
    }

    private void ClearTarget()
    {
        _currentTarget = null;
        _dotTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (_currentTarget == null) return;

        _dotTimer += Time.fixedDeltaTime;
        if (_dotTimer >= _dotInterval)
        {
            _currentTarget.TakeDamage(_damage, _damageName);
            _dotTimer = 0f;
        }
    }
}
