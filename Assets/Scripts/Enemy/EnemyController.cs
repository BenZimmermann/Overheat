using UnityEngine;

/// <summary>
/// todo:
/// states sollten sich mit animationen abpassen -> attack nur dann wenn auch animation usw
/// ranged sollte sich immer in die richtung drehen in die er geht.
/// </summary>
public enum EnemyState { Idle, Chase, Attack, Reposition, Stunned }

public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyStats _stats;
    [SerializeField] private GameObject _explosionEffect;

    private EnemyMovement _movement;
    private EnemyAttack _attack;
    private RoomController _room;
    private Animator _animator;

    private EnemyState _state = EnemyState.Idle;
    private float _currentHealth;
    private Transform _player;

    private float _attackCooldown;
    private float _repositionTimer;
    private float _stunTimer;

    [Header("Reposition")]
    [SerializeField] private float _repositionInterval = 4f;


    private static readonly int AnimIdle = Animator.StringToHash("Idle");
    private static readonly int AnimRun = Animator.StringToHash("Run");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _attack = GetComponent<EnemyAttack>();
        _animator = GetComponent<Animator>();
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

    //cases for state changes:
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
                if (dist <= _stats.attackRange) SetState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                if (dist > _stats.attackRange * 1.2f) SetState(EnemyState.Chase);
                break;

            case EnemyState.Reposition:
                if (_movement.ReachedDestination) SetState(EnemyState.Chase);
                break;
        }
    }
    //execute behavior based on state (movement, attack, animations)
    private void ExecuteState()
    {
        if (_state == EnemyState.Stunned) return;

        switch (_state)
        {
            case EnemyState.Idle:
                _movement.Wander();
                // ANIMATION: Idle
                PlayAnim(AnimIdle);
                break;

            case EnemyState.Chase:
                _movement.ChasePlayer(_player.position);
                // ANIMATION: Run, Idle
                PlayAnim(_movement.IsMoving ? AnimRun : AnimIdle);
                break;

            case EnemyState.Attack:
                _movement.AttackMovement(_player.position);
                FacePlayer();
                TryAttack();
                // ANIMATION: Attack getriggert TryAttack 
                if (_stats.enemyType == EnemyType.Ranged && _movement.IsMoving)
                    PlayAnim(AnimRun);
                break;

            case EnemyState.Reposition:
                _movement.MoveToReposition();
                FacePlayer();
                // ANIMATION: Run
                PlayAnim(AnimRun);
                break;
        }
    }

    #region timers
    private void TickTimers()
    {
        if (_attackCooldown > 0f) _attackCooldown -= Time.fixedDeltaTime;

        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.fixedDeltaTime;
            if (_stunTimer <= 0f)
            {
                _stunTimer = 0f;
                _movement.ResetSpeed();
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
    #endregion
    //try to attack player if in range and cooldown is over, behavior depends on enemy type (meele, ranged, bomber, sniper)
    private void TryAttack()
    {
        if (_attackCooldown > 0f) return;
        _attackCooldown = _stats.attackCooldown;

        switch (_stats.enemyType)
        {
            case EnemyType.Meele:
                // ANIMATION: Attack trigger
                PlayAnim(AnimAttack);
                IDamageable meleeTarget = _player.GetComponentInParent<IDamageable>();
                meleeTarget?.TakeDamage(_stats.damage, gameObject.name);
                break;

            case EnemyType.Bomber:
                // ANIMATION: Attack trigger
                PlayAnim(AnimAttack);
                Explode();
                break;

            case EnemyType.Ranged:
                // ANIMATION: Attack trigger
                PlayAnim(AnimAttack);
                _attack?.Fire();
                break;

            case EnemyType.Sniper:
                //not finished yet
                _attack?.Fire();
                break;
        }
    }

    // take damage, apply stun and slow effect, if health drops to 0 or below, die and drop money
    public void TakeDamage(float amount, string source)
    {
        SoundManager.Instance.Play3DSound(SoundType.DamageEnemy, transform.position);
        _currentHealth -= amount;


        _stunTimer = _stats.stunDuration;
        _movement.SetSpeed(_stats.moveSpeed * _stats.slowOnHit);
        SetState(EnemyState.Stunned);

        if (_currentHealth <= 0f)
            Die();
    }
    // headshot takes more damage and triggers reposition
    public void TakeHeadshotDamage(float amount, string source)
        => TakeDamage(amount * _stats.headShotMultiplier, source);

    public void ApplyKnockback(Vector3 dir, float force)
        => _movement.ApplyKnockback(dir, force);


    public void TriggerReposition() => TriggerReposition(GetRepositionPoint());
    // set a new random reposition target and switch to reposition state, used for ranged enemies to keep distance and avoid being predictable, also triggered when they shoot
    public void TriggerReposition(Vector3 target)
    {
        _movement.SetRepositionTarget(target);
        SetState(EnemyState.Reposition);
    }

    public void NotifyShotFired() => TriggerReposition();
    //calculate a random point around the enemy to reposition
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
    // play animation based on hash, checks if animator is assigned to avoid errors
    private void PlayAnim(int hash)
    {
        if (_animator == null) return;
        _animator.SetTrigger(hash);
    }
    //only for bomber enemy: play explosion effect, deal damage in an area around it, then die
    private void Explode()
    {
        if (_explosionEffect != null)
        {
            //instanciate explosion effect and play all particle systems, then destroy the effect after 3 seconds to clean up
            GameObject fx = Instantiate(_explosionEffect, transform.position, Quaternion.identity);
            foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
            Destroy(fx, 3f);
        }
        //damages all damageable objects in a radius around the enemy, using OverlapSphere to find colliders and applying damage to any IDamageable components found in their parents
        Collider[] hits = Physics.OverlapSphere(transform.position, _stats.attackRange * 1.5f);
        foreach (Collider col in hits)
            col.GetComponentInParent<IDamageable>()?.TakeDamage(_stats.damage, gameObject.name);

        Die();
    }
    //kills the enemy, plays death sound, notifies the room, increments kill count, drops money and destroys the game object
    private void Die()
    {
        SoundManager.Instance.Play3DSound(SoundType.EnemyDeath, transform.position);
        _room?.OnEnemyDied(this);
        GameManager.Instance.Data.EnemiesKilled++;
        DropMoney();
        Destroy(gameObject);
    }
    //handles dropping money on death, chance and amount based on stats, instantiates money objects and applies random force and torque to them for a natural scatter effect
    private void DropMoney()
    {
        if (Random.value > _stats.moneyDropChance) return;
        for (int i = 0; i < Mathf.RoundToInt(_stats.moneyDropAmount); i++)
        {
            GameObject coin = Instantiate(_stats.moneyObj, transform.position, Random.rotation);
            if (coin.TryGetComponent(out Rigidbody rb))
            {
                Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f)).normalized;
                rb.AddForce(dir * Random.Range(1f, 6f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * Random.Range(1f, 4f), ForceMode.Impulse);
            }
        }
    }

    public void SetRoom(RoomController room) => _room = room;

    public EnemyStats Stats => _stats;
    public Transform Player => _player;
    public EnemyState State => _state;
}