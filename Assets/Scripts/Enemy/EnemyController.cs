using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Reposition,
    Stunned
}
public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyStats _stats;

    private EnemyMovement _movement;
    private RoomController _room;

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
                FacePlayer();
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

    public void SetRoom(RoomController room)
    {
        _room = room;
    }
    private void Die()
    {
        _room?.OnEnemyDied(this);
        GameManager.Instance.Data.EnemiesKilled++;
        DropMoney();
        Destroy(gameObject);
    }

    private void DropMoney()
    {
        if (Random.value > _stats.moneyDropChance) return;

        int amount = Mathf.RoundToInt(_stats.moneyDropAmount);

        for (int i = 0; i < amount; i++)
        {
            GameObject coin = Instantiate(
                _stats.moneyObj,
                transform.position,
                Random.rotation
            );

            if (coin.TryGetComponent(out Rigidbody rb))
            {
                Vector3 randomDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized;

                float force = Random.Range(1f, 6f);
                rb.AddForce(randomDirection * force, ForceMode.Impulse);

                float torque = Random.Range(1f, 4f);
                rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            }
        }
    }
    private void FacePlayer()
    {
        if (_player == null) return;
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            20f * Time.fixedDeltaTime
        );
    }

    public EnemyStats Stats => _stats;
    public Transform Player => _player;
}