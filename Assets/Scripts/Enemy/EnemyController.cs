using System.Runtime.CompilerServices;
using UnityEngine;

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
    [SerializeField] private GameObject _explosionEffect;
    [SerializeField] private Transform _headHitbox;      

    private EnemyMovement _movement;
    private RoomController _room;
    private EnemyAttack _attack;

    private EnemyState _state = EnemyState.Idle;
    private float _currentHealth;
    private Transform _player;

    private float _attackCooldown;
    private float _repositionTimer;
    private float _stunTimer;

    [Header("Reposition")]
    [SerializeField] private float _repositionInterval = 4f;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _attack = GetComponent<EnemyAttack>();
    }

    private void Start()
    {
        _currentHealth = _stats.maxHealth;
        _player = GameObject.FindWithTag("Player")?.transform;
        _repositionTimer = _repositionInterval;
        _movement.Init(_stats);
        _attack?.Init(_stats, _player);
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        TickTimers();
        UpdateState();
        ExecuteState();
    }

    private void UpdateState()
    {
        if (_state == EnemyState.Stunned) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        switch (_state)
        {
            case EnemyState.Idle:
                if (dist <= _stats.detectionRange) SetState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                bool inAttackRange = (_stats.enemyType == EnemyType.Ranged || _stats.enemyType == EnemyType.Sniper)
                    ? dist <= _stats.attackRange
                    : dist <= _stats.attackRange;  
                if (inAttackRange) SetState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                if (dist > _stats.attackRange * 1.2f) SetState(EnemyState.Chase);
                break;

            case EnemyState.Reposition:
                if (_movement.ReachedDestination) SetState(EnemyState.Chase);
                break;
        }
    }

    private void ExecuteState()
    {
        if (_state == EnemyState.Stunned) return;

        switch (_state)
        {
            case EnemyState.Idle:
                _movement.Wander();
                break;

            case EnemyState.Chase:
                _movement.ChasePlayer(_player.position);
                break;

            case EnemyState.Attack:
                _movement.AttackMovement(_player.position);
                FacePlayer();
                TryAttack();
                break;

            case EnemyState.Reposition:
                _movement.MoveToReposition();
                FacePlayer();
                break;
        }
    }


    private void TickTimers()
    {
        if (_attackCooldown > 0f) _attackCooldown -= Time.fixedDeltaTime;


        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.fixedDeltaTime;
            if (_stunTimer <= 0f)
            {
                _stunTimer = 0f;
                SetState(EnemyState.Chase);
            }
        }


        if (_state == EnemyState.Stunned || _state == EnemyState.Reposition) return;
        if (_stats.enemyType != EnemyType.Ranged && _stats.enemyType != EnemyType.Sniper) return;

        _repositionTimer -= Time.fixedDeltaTime;
        if (_repositionTimer <= 0f)
        {
            _repositionTimer = _repositionInterval + Random.Range(-1f, 1f);
            TriggerReposition();
        }
    }




    private void TryAttack()
    {
        if (_attackCooldown > 0f) return;
        _attackCooldown = _stats.attackCooldown;

        switch (_stats.enemyType)
        {
            case EnemyType.Meele:
                IDamageable meleeTarget = _player.GetComponentInParent<IDamageable>();
                meleeTarget?.TakeDamage(_stats.damage, gameObject.name);
                break;

            case EnemyType.Bomber:
                Explode();
                break;

            case EnemyType.Ranged:
            case EnemyType.Sniper:
                _attack?.Fire();
                break;
        }
    }

    public void TakeDamage(float amount, string source)
    {
        _currentHealth -= amount;


        _stunTimer = _stats.stunDuration;
        SetState(EnemyState.Stunned);
        _movement.SetSpeed(_stats.moveSpeed * _stats.slowOnHit);
        SetState(EnemyState.Reposition);
        if (_currentHealth <= 0f)
            Die();
    }

    public void TakeHeadshotDamage(float amount, string source)
        => TakeDamage(amount * _stats.headShotMultiplier, source);

    public void ApplyKnockback(Vector3 dir, float force)
        => _movement.ApplyKnockback(dir, force);

    public void TriggerReposition()
        => TriggerReposition(GetRepositionPoint());

    public void TriggerReposition(Vector3 target)
    {
        _movement.SetRepositionTarget(target);
        SetState(EnemyState.Reposition);
    }

    public void NotifyShotFired()
    {

        TriggerReposition();
    }

    private Vector3 GetRepositionPoint()
    {
        Vector3 toPlayer = (_player.position - transform.position).normalized;
        Vector3 lateral = new Vector3(-toPlayer.z, 0f, toPlayer.x);
        float side = Random.value > 0.5f ? 1f : -1f;
        float range = _stats.repositionRange;

        return transform.position
             + lateral * side * range
             + toPlayer * Random.Range(-range * 0.4f, range * 0.4f);
    }


    public void SetState(EnemyState state) => _state = state;

    private void FacePlayer()
    {
        if (_player == null) return;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            20f * Time.fixedDeltaTime);
    }

    private void Explode()
    {
        if (_explosionEffect != null)
        {
            GameObject fx = Instantiate(_explosionEffect, transform.position, Quaternion.identity);
            foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
            Destroy(fx, 3f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, _stats.attackRange * 1.5f);
        foreach (Collider col in hits)
        {
            IDamageable t = col.GetComponentInParent<IDamageable>();
            t?.TakeDamage(_stats.damage, gameObject.name);
        }

        Die();
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
                    Random.Range(0.5f, 1f),  // immer leicht nach oben
                    Random.Range(-1f, 1f)
                ).normalized;

                float force = Random.Range(1f, 6f);
                rb.AddForce(randomDirection * force, ForceMode.Impulse);

                float torque = Random.Range(1f, 4f);
                rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            }
        }
    }

    public void SetRoom(RoomController room) => _room = room;

    public EnemyStats Stats => _stats;
    public Transform Player => _player;
    public EnemyState State => _state;
}