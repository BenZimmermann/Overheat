using UnityEngine;

public enum EnemyState
{
    Chase,
    Attack,
    Reposition,
    Stunned
}
public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyStats _stats;

    private EnemyMovement _movement;

    private EnemyState _currentState = EnemyState.Chase;
    private float _currentHealth;
    private Transform _player;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        _currentHealth = _stats.maxHealth;
        _player = GameObject.FindWithTag("Player")?.transform;

        _movement.Init(_stats);
    }

    private void FixedUpdate()
    {
        if (_player == null) return;
        if (_currentState == EnemyState.Stunned) return;

        UpdateState();
        ExecuteState();
    }

    private void UpdateState()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        switch (_currentState)
        {
            case EnemyState.Chase:
                if (dist <= _stats.attackRange)
                    SetState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                if (dist > _stats.attackRange)
                    SetState(EnemyState.Chase);
                break;

            case EnemyState.Reposition:
                if (_movement.ReachedDestination)
                    SetState(EnemyState.Chase);
                break;
        }
    }

    private void ExecuteState()
    {
        switch (_currentState)
        {
            case EnemyState.Chase:
                _movement.ChasePlayer(_player.position);
                break;

            case EnemyState.Attack:
                _movement.Stop();
                
                break;

            case EnemyState.Reposition:
                _movement.MoveToReposition();
                break;
        }
    }

    public void SetState(EnemyState state)
    {
        _currentState = state;
    }

    public void TriggerReposition(Vector3 target)
    {
        _movement.SetRepositionTarget(target);
        SetState(EnemyState.Reposition);
    }

    public void TakeDamage(float amount, string source)
    {
        _currentHealth -= amount;
        //_effects.ApplyEffect(CreateStunEffect());
        if (_currentHealth <= 0)
            Die();
    }
    public void TakeHeadshotDamage(float amount, string source)
    {
        TakeDamage(amount * _stats.headShotMultiplier, source);
    }

    private void Die()
    {
        RoomController room = GetComponentInParent<RoomController>();
        room?.OnEnemyDied(this);
        GameManager.Instance.Data.EnemiesKilled++;
        Destroy(gameObject);
    }

    public EnemyStats Stats => _stats;
    public Transform Player => _player;
}