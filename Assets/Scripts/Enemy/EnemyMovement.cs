using UnityEngine;
using UnityEngine.AI;
using System.Collections;
/// <summary>
///todo:
///-ranged/sniper always look at player
///-ranged/sniper sould reposition after a short time
///-when taken damage -> reposition
/// -fix buggy movement
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private EnemyStats _stats;
    private Vector3 _repositionTarget;

    //later in Enemy Stats

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 20f;
    [SerializeField] private float _snapRotationAngle = 45f;

    [Header("Acceleration")]
    [SerializeField] private float _accelerationBoost = 2f;

    [Header("Melee Dash")]
    [SerializeField] private float _dashRange = 5f;
    [SerializeField] private float _dashForce = 18f;
    [SerializeField] private float _dashCooldown = 3f;

    [Header("Ranged / Sniper")]
    [SerializeField] private float _preferredDistance = 10f; 
    [SerializeField] private float _retreatDistance = 4f;

    [Header("Randomness")]
    [SerializeField] private float _wanderStrength = 0.8f; 
    [SerializeField] private float _wanderChangeRate = 1.5f;

    // Knockback
    private float _knockbackTimer;
    private const float KnockbackDuration = 0.15f;

    // Dash
    private float _dashCooldownTimer;
    private bool _isDashing;

    // Wander
    private Vector3 _wanderOffset;
    private float _wanderTimer;

    // Stuck detection
    private Vector3 _lastPosition;
    private float _stuckTimer;
    private const float StuckThreshold = 0.05f;
    private const float StuckTime = 0.8f;

    public float CurrentSpeed => _agent != null ? _agent.speed : _stats.moveSpeed;
    public bool ReachedDestination => _agent == null || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);


    public void Init(EnemyStats stats)
    {
        _stats = stats;
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (_agent != null)
        {
            _agent.speed = stats.moveSpeed;     
            _agent.acceleration = stats.moveSpeed * _accelerationBoost * 10f;
            _agent.stoppingDistance = Mathf.Max(0.3f, stats.attackRange * 0.85f);
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(30, 70);
        }
    }
    private void FixedUpdate()
    {
        TickKnockback();
        TickDashCooldown();
        TickWander();

        if (_agent == null || !_agent.enabled) return;

        HandleRotation();
        CheckStuck();
    }

    public void ChasePlayer(Vector3 playerPos)
    {
        if (_stats == null) return;

        switch (_stats.enemyType)
        {
            case EnemyType.Meele:
                ChaseMelee(playerPos);
                break;
            case EnemyType.Ranged:
                ChaseRanged(playerPos);
                break;
            case EnemyType.Sniper:
                ChaseSniper(playerPos);
                break;
            case EnemyType.Bomber:
                ChaseBomber(playerPos);
                break;
        }
    }
    public void MoveToReposition() => SetDestination(_repositionTarget);
    public void SetRepositionTarget(Vector3 target) => _repositionTarget = target;

    public void Resume()
    {
        if (_agent != null)
            _agent.isStopped = false;
    }

    public void Stop()
    {
        if (_agent == null) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    public void SetSpeed(float speed)
    {
        if (_agent != null)
            _agent.speed = speed;
    }

    public void ResetSpeed()
    {
        if (_agent != null && _stats != null)
            _agent.speed = _stats.moveSpeed;
    }
    #region Movement types
    private void ChaseMelee(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);

        if (!_isDashing && _dashCooldownTimer <= 0f && dist <= _dashRange && dist > _stats.attackRange)
        {
            TriggerDash(playerPos);
            return;
        }

        SetDestination(playerPos + _wanderOffset);
    }
    private void ChaseRanged(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);

        if (dist < _retreatDistance)
        {
            Vector3 awayDir = (transform.position - playerPos).normalized;
            SetDestination(transform.position + awayDir * _preferredDistance);
        }
        else if (dist > _preferredDistance + 2f)
        {
            SetDestination(playerPos + _wanderOffset);
        }
    }
    private void ChaseSniper(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);

        if (dist < _retreatDistance)
        {
            Vector3 awayDir = (transform.position - playerPos).normalized;
            SetDestination(transform.position + awayDir * (_retreatDistance * 2f));
        }
    }
    private void ChaseBomber(Vector3 playerPos)
    {
        _agent.speed = _stats.moveSpeed * 1.4f;
        SetDestination(playerPos);
    }
    #endregion
    #region Dash
    private void TriggerDash(Vector3 target)
    {
        if (_rb == null) return;

        _isDashing = true;
        _dashCooldownTimer = _dashCooldown;

        Vector3 dir = (target - transform.position).normalized;

        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.AddForce(dir * _dashForce, ForceMode.Impulse);
        _knockbackTimer = 0.12f;
    }

    private void TickDashCooldown()
    {
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= Time.fixedDeltaTime;

        if (_isDashing && _knockbackTimer <= 0f)
            _isDashing = false;
    }
    #endregion

    #region Knockback
    public void ApplyKnockback(Vector3 dir, float force)
    {
        if (_rb == null) return;

        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.AddForce(dir.normalized * force, ForceMode.Impulse);
        _knockbackTimer = KnockbackDuration;
    }

    private void TickKnockback()
    {
        if (_knockbackTimer <= 0f) return;

        _knockbackTimer -= Time.fixedDeltaTime;

        if (_knockbackTimer <= 0f)
        {
            _knockbackTimer = 0f;
            if (_rb != null) _rb.isKinematic = true;
            if (_agent != null)
            {
                _agent.enabled = true;
                _agent.isStopped = false;
            }
        }
    }


    #endregion

    #region wander
    private void TickWander()
    {
        _wanderTimer -= Time.fixedDeltaTime;
        if (_wanderTimer > 0f) return;

        _wanderTimer = _wanderChangeRate + Random.Range(-0.3f, 0.3f);
        _wanderOffset = new Vector3(
            Random.Range(-_wanderStrength, _wanderStrength),
            0f,
            Random.Range(-_wanderStrength, _wanderStrength));
    }
    #endregion

    private void HandleRotation()
    {
        Vector3 velocity = _agent.velocity;
        if (velocity.sqrMagnitude < 0.01f) return;

        Vector3 dir = new Vector3(velocity.x, 0f, velocity.z).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        float angle = Quaternion.Angle(transform.rotation, targetRot);

        transform.rotation = angle >= _snapRotationAngle
            ? targetRot
            : Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.fixedDeltaTime);
    }

    private void CheckStuck()
    {
        float moved = Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        if (_agent.isStopped || ReachedDestination) { _stuckTimer = 0f; return; }

        if (moved < StuckThreshold)
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer >= StuckTime)
            {
                _stuckTimer = 0f;
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                _agent.SetDestination(transform.position + offset);
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
    }
    private void SetDestination(Vector3 target)
    {
        if (_agent == null) return;
        _agent.isStopped = false;
        _agent.SetDestination(target);
    }
}