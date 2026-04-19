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

    private float _baseMeeleCooldown;
    private float _baseMeeleDistance;

    private float CurrentCooldown =>
    Mathf.Max(0.1f,  _baseMeeleCooldown - GameManager.Instance.Data.LessMeeleCooldown);
    private float CurrentRadius =>
    Mathf.Max( _baseMeeleDistance + GameManager.Instance.Data.MoreMeeleDistance);

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
    private void Start()
    {
        _baseMeeleCooldown = Wdata.CooldownTime;
        _baseMeeleDistance = Wdata.attackRadius;
    }
    void Update()
    {
        if(!Wdata.isMelee) return; 
        if (_attackAction != null && _attackAction.WasPressedThisFrame())
            Attack();
    }
    
    private void Attack()
    {
        if (Time.time < _lastAttackTime + CurrentCooldown) return;
        _lastAttackTime = Time.time;

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 center = origin.position + origin.forward * (Wdata.range * 0.5f);

        Collider[] hits = Physics.OverlapSphere(center, CurrentRadius, Damageable);

        HashSet<IDamageable> already = new HashSet<IDamageable>();
        SoundManager.Instance.PlaySound(SoundType.Melee);
        foreach (Collider col in hits)
        {
            if ((Ignore.value & (1 << col.gameObject.layer)) != 0) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            if (!already.Add(target)) continue;

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
        Gizmos.DrawWireSphere(center, CurrentRadius);
    }
}