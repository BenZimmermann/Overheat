using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// modular damage script for environmental hazards like spikes, fire, acid, etc. It can be set to apply damage on collision or trigger, 
/// and can optionally apply damage over time. The damage type can be specified by name for use in damage calculations or effects.
/// </summary>
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

    private HashSet<IDamageable> _currentTargets = new HashSet<IDamageable>();
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
    // Checks if the target can be damaged based on layer and if it has an IDamageable component, then applies damage immediately or starts damage over time.
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