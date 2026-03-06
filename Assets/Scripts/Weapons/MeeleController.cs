using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeController : MonoBehaviour
{
    [SerializeField] private WeaponData Wdata;

    [SerializeField] private Transform attackOrigin;
    [SerializeField] private ParticleSystem ImpactParticleSystem;

    [SerializeField] private LayerMask Mask;
    [SerializeField] private LayerMask Damageable;

    [Header("Input Actions")]
    public InputActionAsset inputActionAsset;
    public string actionMapName = "Player";
    public string attackActionName = "Attack";

    InputAction _attackAction;

    private Animator animator;
    private float _lastAttackTime;

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
        if(!Wdata.isMelee) return; // Prevent melee logic if it's not a melee weapon
        if (_attackAction != null && _attackAction.WasPressedThisFrame())
            Attack();
    }

    private void Attack()
    {
        if (Time.time < _lastAttackTime + Wdata.ShootDelay) return;
        _lastAttackTime = Time.time;

        Transform origin = attackOrigin != null ? attackOrigin : transform;

        if (Physics.SphereCast(origin.position, Wdata.attackRadius, origin.forward,
            out RaycastHit hit, Wdata.range, Damageable))
        {
            hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(Wdata.damage, Wdata.name);
        }

        Debug.DrawRay(origin.position, origin.forward * Wdata.attackRadius, Color.red, 0.5f);
    }
        void OnDrawGizmos()
    {
        if (Wdata == null) return;
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position, Wdata.attackRadius);
        Gizmos.DrawWireSphere(origin.position + origin.forward * Wdata.range, Wdata.attackRadius);

        Gizmos.DrawLine(origin.position + origin.up    * Wdata.attackRadius, origin.position + origin.forward * Wdata.range + origin.up    * Wdata.attackRadius);
        Gizmos.DrawLine(origin.position - origin.up    * Wdata.attackRadius, origin.position + origin.forward * Wdata.range - origin.up    * Wdata.attackRadius);
        Gizmos.DrawLine(origin.position + origin.right * Wdata.attackRadius, origin.position + origin.forward * Wdata.range + origin.right * Wdata.attackRadius);
        Gizmos.DrawLine(origin.position - origin.right * Wdata.attackRadius, origin.position + origin.forward * Wdata.range - origin.right * Wdata.attackRadius);
    }
}