using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeController : MonoBehaviour
{
    [SerializeField] private WeaponData Wdata;

    [SerializeField] private Transform attackOrigin;
    [SerializeField] private ParticleSystem ImpactParticleSystem;

    [SerializeField] private LayerMask Ignore;
    [SerializeField] private LayerMask Damageable;

    [Header("Input Actions")]
    public InputActionAsset inputActionAsset;
    public string actionMapName = "Player";
    public string attackActionName = "Attack";

    InputAction _attackAction;

    private Animator animator;
    private float _lastAttackTime;
    private bool _gizmoActive;

    void BindInputActions()
    {
        if (inputActionAsset == null) return;
        var map = inputActionAsset.FindActionMap(actionMapName, false);

        if (map == null) return;
        _attackAction = map.FindAction(attackActionName, false); 
        
        map.Enable();
    }

    private void OnEnable() { BindInputActions(); }
    private void OnDisable() { inputActionAsset?.FindActionMap(actionMapName, false)?.Disable(); }

    void Update()
    {
        if(!Wdata.isMelee) return; 
        if (_attackAction != null && _attackAction.WasPressedThisFrame())
            Attack();
    }
    
    private void Attack()
    {
        if (Time.time < _lastAttackTime + Wdata.ShootDelay) return;
        _lastAttackTime = Time.time;

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 center = origin.position + origin.forward * (Wdata.range * 0.5f);

        Collider[] hits = Physics.OverlapSphere(center, Wdata.attackRadius, Damageable);

        HashSet<GameObject> already = new HashSet<GameObject>();

        foreach (Collider col in hits)
        {
            if ((Ignore.value & (1 << col.gameObject.layer)) != 0) continue;

            GameObject root = col.transform.root.gameObject;
            if (!already.Add(root)) continue;

            Damage(col, center);
        }

        _gizmoActive = true;
        CancelInvoke(nameof(ResetGizmo));
        Invoke(nameof(ResetGizmo), 0.5f);
    }

    private void Damage(Collider col, Vector3 origin)
    {
        IDamageable target = col.GetComponentInParent<IDamageable>();
        target?.TakeDamage(Wdata.damage, Wdata.name);

        if (ImpactParticleSystem != null)
        {
            Vector3 closest = col.ClosestPoint(origin);
            Instantiate(ImpactParticleSystem, closest,
                Quaternion.LookRotation(closest - origin));
        }
    }

    void ResetGizmo() => _gizmoActive = false;
    void OnDrawGizmos()
    {
        if (Wdata == null) return;
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 center = origin.position + origin.forward * (Wdata.range * 0.5f);

        Gizmos.color = _gizmoActive ? Color.red : new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(center, Wdata.attackRadius);
    }
}