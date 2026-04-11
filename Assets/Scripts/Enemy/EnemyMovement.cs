using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// todo:
/// dash funktoinert nicht, da agent gestoppt wird und dann nicht mehr weiterbewegt werden kann. Lösung: während Dash Agent deaktivieren, danach wieder aktivieren und Ziel neu setzen.
/// der retreat vom ranged sollte immer einen punkt finden der vor dem player ligt, sodass der ranged nicht am player vorbeiläuft. Aktuell könnte er sich hinter dem player positionieren, wenn der player sich bewegt.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private EnemyStats _stats;
    private Vector3 _repositionTarget;

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 18f;
    [SerializeField] private float _snapAngle = 50f;

    [Header("Crowd Separation")]
    [SerializeField] private LayerMask _enemyMask;

    private float _physicsTimer;
    private float _dashCooldownTimer;
    private float _strafeTimer;
    private float _strafeDir = 1f;

    public bool CanDash => _dashCooldownTimer <= 0f && _physicsTimer <= 0f;
    public bool IsMoving => _agent != null && _agent.velocity.sqrMagnitude > 0.1f;
    public bool ReachedDestination =>
        _agent == null || !_agent.isOnNavMesh || !_agent.enabled ||
        (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f);


    public void Init(EnemyStats stats)
    {
        _stats = stats;
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (_agent == null) return;

        _agent.speed = stats.moveSpeed;
        _agent.angularSpeed = 0f;
        _agent.acceleration = stats.moveSpeed * 20f;
        _agent.autoBraking = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.avoidancePriority = Random.Range(20, 80);

        _agent.stoppingDistance = (stats.enemyType == EnemyType.Ranged || stats.enemyType == EnemyType.Sniper)
            ? 0.3f
            : Mathf.Max(0.3f, stats.attackRange * 0.75f);
    }


    private void FixedUpdate()
    {
        TickPhysics();
        TickTimers();

        if (_agent == null || !_agent.enabled) return;

        HandleRotation();
        CheckStuck();
    }


    public void Wander() => Stop();

    public void ChasePlayer(Vector3 playerPos)
    {
        if (_stats == null) return;
        switch (_stats.enemyType)
        {
            case EnemyType.Meele: ChaseMelee(playerPos); break;
            case EnemyType.Ranged: ChaseRanged(playerPos); break;
            case EnemyType.Sniper: ChaseSniper(playerPos); break;
            case EnemyType.Bomber: ChaseBomber(playerPos); break;
        }
    }

    public void AttackMovement(Vector3 playerPos)
    {
        switch (_stats.enemyType)
        {
            case EnemyType.Ranged: StrafeAround(playerPos); FacePlayer(playerPos); break;
            case EnemyType.Sniper: FacePlayer(playerPos); break;
            default: Stop(); break;
        }
    }

    public void MoveToReposition() => SetDest(_repositionTarget);
    public void SetRepositionTarget(Vector3 t) => _repositionTarget = t;

    public void Stop()
    {
        if (_agent == null || !_agent.isOnNavMesh || !_agent.enabled) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    public void SetSpeed(float s) { if (_agent != null) _agent.speed = s; }
    public void ResetSpeed() { if (_agent != null && _stats != null) _agent.speed = _stats.moveSpeed; }


    private void ChaseMelee(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);
        Vector3 sep = GetSeparationOffset();
        SetDest(playerPos + sep);

        float dashTrigger = _stats.attackRange * _stats.dashTriggerMult;
        if (!CanDash || dist > dashTrigger || dist <= _stats.attackRange) return;

        Vector3 dir = (playerPos - transform.position); dir.y = 0f; dir = dir.normalized;
        Vector3 checkPos = transform.position + dir * (dist * 0.8f);

        if (NavMesh.SamplePosition(checkPos, out _, 1.5f, NavMesh.AllAreas))
            DashFlat(dir);
    }

    /// <summary>
    /// Ranged: immer in Bewegung, hält Abstand, strafe seitwärts.
    /// Weicht zurück wenn Player zu nah, kommt näher wenn zu weit.
    /// </summary>
    private void ChaseRanged(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);
        Vector3 toPlayer = (playerPos - transform.position).normalized;
        Vector3 lateral = new Vector3(-toPlayer.z, 0f, toPlayer.x) * _strafeDir;
        FacePlayer(playerPos);

        if (dist < _stats.retreatDist)
        {
            // Seitlich + rückwärts ausweichen
            Vector3 escape = (-toPlayer + lateral * 0.8f).normalized * _stats.preferredDist;
            SetDest(transform.position + escape);
        }
        else if (dist > _stats.preferredDist + 2f)
        {
            // Näherkommen – leicht seitlich versetzt
            SetDest(playerPos + lateral * 2f);
        }
        //else
        //{
        //    // Komfortzone: dauerhaft strafe
        //    StrafeAround(playerPos);
        //}
    }

    private void ChaseSniper(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);
        FacePlayer(playerPos);

        if (dist < _stats.sniperRetreatDist)
        {
            _agent.speed = _stats.moveSpeed * _stats.sniperRetreatSpeed;
            Vector3 away = (transform.position - playerPos).normalized;
            Vector3 lateral = new Vector3(-away.z, 0f, away.x) * (Random.value > 0.5f ? 1f : -1f);
            SetDest(transform.position + (away * 0.5f + lateral * 0.5f).normalized * _stats.sniperRetreatDist * 2f);
        }
        else
        {
            _agent.speed = _stats.moveSpeed;
            Stop();
        }
    }

    private void ChaseBomber(Vector3 playerPos)
    {
        _agent.speed = _stats.moveSpeed * 1.5f;
        SetDest(playerPos + GetSeparationOffset());
    }

    private void StrafeAround(Vector3 playerPos)
    {
        Vector3 toPlayer = (playerPos - transform.position).normalized;
        Vector3 lateral = new Vector3(-toPlayer.z, 0f, toPlayer.x) * (_strafeDir * _stats.strafeSpeed);
        Vector3 target = transform.position + lateral * 3f;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            SetDest(hit.position);
    }


    private void DashFlat(Vector3 dir)
    {
        if (_rb == null || _agent == null) return;

        _dashCooldownTimer = _stats.dashCooldown;
        _physicsTimer = _stats.dashDuration;

        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(new Vector3(dir.x, 0f, dir.z).normalized * _stats.dashForce, ForceMode.Impulse);
    }

    public void TriggerCounterDash(Vector3 playerPos)
    {
        if (!CanDash) return;
        Vector3 dir = (playerPos - transform.position); dir.y = 0f;
        DashFlat(dir.normalized);
        _dashCooldownTimer *= 0.5f;
    }

    public void ApplyKnockback(Vector3 dir, float force)
    {
        if (_rb == null || _agent == null) return;
        _physicsTimer = 0.15f;
        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(new Vector3(dir.x, 0f, dir.z).normalized * force, ForceMode.Impulse);
    }


    private void TickPhysics()
    {
        if (_physicsTimer <= 0f) return;
        _physicsTimer -= Time.fixedDeltaTime;

        if (_physicsTimer <= 0f)
        {
            _physicsTimer = 0f;
            if (_rb != null) _rb.isKinematic = true;
            if (_agent != null) { _agent.enabled = true; _agent.isStopped = false; }
        }
    }

    private void TickTimers()
    {
        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.fixedDeltaTime;

        _strafeTimer -= Time.fixedDeltaTime;
        if (_strafeTimer <= 0f)
        {
            _strafeTimer = _stats != null
                ? _stats.strafeChange + Random.Range(-0.5f, 0.5f)
                : 2f;
            _strafeDir = Random.value > 0.5f ? 1f : -1f;
        }
    }

    private void HandleRotation()
    {
        if (_stats != null && (_stats.enemyType == EnemyType.Ranged || _stats.enemyType == EnemyType.Sniper))
            return;

        Vector3 vel = _agent.velocity;
        if (vel.sqrMagnitude < 0.01f) return;

        Vector3 dir = new Vector3(vel.x, 0f, vel.z).normalized;
        Quaternion target = Quaternion.LookRotation(dir);
        float angle = Quaternion.Angle(transform.rotation, target);

        transform.rotation = angle >= _snapAngle
            ? target
            : Quaternion.Slerp(transform.rotation, target, _rotationSpeed * Time.fixedDeltaTime);
    }

    public void FacePlayer(Vector3 playerPos)
    {
        Vector3 dir = playerPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            _rotationSpeed * Time.fixedDeltaTime);
    }


    private Vector3 _lastPos;
    private float _stuckTimer;

    private void CheckStuck()
    {
        if (!_agent.isOnNavMesh) return;

        float moved = Vector3.Distance(transform.position, _lastPos);
        _lastPos = transform.position;

        if (_agent.isStopped || ReachedDestination) { _stuckTimer = 0f; return; }

        _stuckTimer = moved < 0.05f ? _stuckTimer + Time.fixedDeltaTime : 0f;

        if (_stuckTimer >= 0.8f)
        {
            _stuckTimer = 0f;
            _agent.SetDestination(transform.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)));
        }
    }


    private Vector3 GetSeparationOffset()
    {
        if (_stats == null) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        int count = 0;

        Collider[] nearby = Physics.OverlapSphere(transform.position, _stats.separationRadius, _enemyMask);
        foreach (Collider col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 away = transform.position - col.transform.position;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist < 0.01f) continue;
            separation += away.normalized * (1f - dist / _stats.separationRadius);
            count++;
        }

        return count == 0 ? Vector3.zero : separation.normalized * _stats.separationForce;
    }


    private void SetDest(Vector3 target)
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
        _agent.isStopped = false;
        _agent.SetDestination(target);
    }
}