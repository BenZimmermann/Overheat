using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy Movement – clean, snappy, Ultrakill-inspired.
///
/// Melee/Walker:
/// - Rennt direkt auf Player zu
/// - Dash nur wenn Player SEHR nah ist (< attackRange * 1.5) UND NavMesh-Pfad frei
/// - Dash = kurzer flacher Forward-Boost (kein Bogen/Sprung)
///
/// Ranged (PostVoid Suits):
/// - Hält mittlere Distanz
/// - Bewegt sich IMMER seitwärts (strafe), niemals stehen
/// - Weicht bei zu kurzem Abstand zurück
/// - Dreht sich immer zum Player
///
/// Sniper (Ultrakill Stray):
/// - Steht fast immer still
/// - Weicht bei zu kurzem Abstand SCHNELL zurück
/// - Rotiert smooth zum Player
///
/// Bomber:
/// - Sprintet direkt drauf zu, kein Ausweichen
///
/// NavMesh Open Bounds:
/// - NavMeshAgent.autoBraking = false
/// - Kein stoppingDistance Puffer
/// - Agent folgt blindlings dem Pfad – wenn Abgrund = kein NavMesh = Agent stoppt NICHT
///   aktiv, fällt aber mit Gravity wenn Rigidbody vorhanden
/// - Für echtes Herunterfallen: Agent deaktivieren sobald kein NavMesh unter Enemy,
///   Rigidbody übernimmt (siehe CheckNavMeshEdge)
///   
/// todo:
/// fix fall of edge
/// 
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

    [Header("Melee Dash")]
    [SerializeField] private float _dashForce = 14f;
    [SerializeField] private float _dashCooldown = 2f;
    [SerializeField] private float _dashDuration = 0.12f;

    [Header("Ranged Strafe")]
    [SerializeField] private float _preferredDist = 9f;
    [SerializeField] private float _retreatDist = 3.5f;
    [SerializeField] private float _strafeSpeed = 1f;
    [SerializeField] private float _strafeChange = 2f;

    [Header("Sniper")]
    [SerializeField] private float _sniperRetreatDist = 5f;
    [SerializeField] private float _sniperRetreatSpeed = 1.4f;

    [Header("NavMesh Edge")]
    [SerializeField] private bool _canFallOffEdges = false;
    [SerializeField] private float _edgeCheckDist = 1.2f;


    private float _physicsTimer;
    private float _dashCooldownTimer;
    private float _strafeTimer;
    private float _strafeDir = 1f;

    public bool CanDash => _dashCooldownTimer <= 0f && _physicsTimer <= 0f;
    public bool ReachedDestination =>
        _agent == null || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f);


    public void Init(EnemyStats stats)
    {
        _stats = stats;
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (_agent == null) return;

        _agent.speed = stats.moveSpeed;
        _agent.angularSpeed = 0f;
        _agent.acceleration = stats.moveSpeed * 20f;
        _agent.stoppingDistance = Mathf.Max(0.3f, stats.attackRange * 0.75f);
        _agent.autoBraking = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.avoidancePriority = Random.Range(20, 80);
    }

    private void FixedUpdate()
    {
        TickPhysics();
        TickTimers();

        if (_agent == null || !_agent.enabled) return;

        if (_canFallOffEdges) CheckNavMeshEdge();
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
        if (_agent == null) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    public void SetSpeed(float s) { if (_agent != null) _agent.speed = s; }
    public void ResetSpeed() { if (_agent != null && _stats != null) _agent.speed = _stats.moveSpeed; }



    private void ChaseMelee(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);
        SetDest(playerPos);


        float dashTrigger = _stats.attackRange * 1.8f;
        if (CanDash && dist <= dashTrigger && dist > _stats.attackRange)
        {
            Vector3 dir = (playerPos - transform.position);
            dir.y = 0f;
            dir = dir.normalized;


            Vector3 checkPos = transform.position + dir * (dist * 0.8f);
            if (NavMesh.SamplePosition(checkPos, out _, 1.5f, NavMesh.AllAreas))
                DashFlat(dir);
        }
    }


    private void ChaseRanged(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);
        FacePlayer(playerPos);

        if (dist < _retreatDist)
        {

            Vector3 away = (transform.position - playerPos).normalized;
            Vector3 lateral = new Vector3(-away.z, 0f, away.x) * _strafeDir;
            SetDest(transform.position + (away + lateral * 0.6f).normalized * _preferredDist);
        }
        else if (dist > _preferredDist + 3f)
        {

            Vector3 toPlayer = (playerPos - transform.position).normalized;
            Vector3 lateral = new Vector3(-toPlayer.z, 0f, toPlayer.x) * _strafeDir * 0.4f;
            SetDest(playerPos + lateral * 2f);
        }
        else
        {

            StrafeAround(playerPos);
        }
    }


    private void ChaseSniper(Vector3 playerPos)
    {
        float dist = Vector3.Distance(transform.position, playerPos);
        FacePlayer(playerPos);

        if (dist < _sniperRetreatDist)
        {
            _agent.speed = _stats.moveSpeed * _sniperRetreatSpeed;
            Vector3 away = (transform.position - playerPos).normalized;

            Vector3 lateral = new Vector3(-away.z, 0f, away.x) * (Random.value > 0.5f ? 1f : -1f);
            SetDest(transform.position + (away * 0.7f + lateral * 0.3f).normalized * _sniperRetreatDist * 1.5f);
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
        SetDest(playerPos);
    }


    private void StrafeAround(Vector3 playerPos)
    {
        Vector3 toPlayer = (playerPos - transform.position).normalized;
        Vector3 lateral = new Vector3(-toPlayer.z, 0f, toPlayer.x) * (_strafeDir * _strafeSpeed);
        Vector3 target = transform.position + lateral * 3f;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            SetDest(hit.position);
    }


    private void DashFlat(Vector3 dir)
    {
        if (_rb == null || _agent == null) return;

        _dashCooldownTimer = _dashCooldown;
        _physicsTimer = _dashDuration;

        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;

        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z).normalized;
        _rb.AddForce(flatDir * _dashForce, ForceMode.Impulse);
    }

    public void TriggerCounterDash(Vector3 playerPos)
    {
        if (!CanDash) return;
        Vector3 dir = (playerPos - transform.position);
        dir.y = 0f;
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


    private void CheckNavMeshEdge()
    {
        if (_physicsTimer > 0f) return;


        if (!NavMesh.SamplePosition(transform.position, out _, 0.5f, NavMesh.AllAreas))
        {

            _agent.enabled = false;
            if (_rb != null) _rb.isKinematic = false;
        }
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
            _strafeTimer = _strafeChange + Random.Range(-0.5f, 0.5f);
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
        float moved = Vector3.Distance(transform.position, _lastPos);
        _lastPos = transform.position;

        if (_agent.isStopped || ReachedDestination) { _stuckTimer = 0f; return; }

        _stuckTimer = moved < 0.05f ? _stuckTimer + Time.fixedDeltaTime : 0f;

        if (_stuckTimer >= 0.8f)
        {
            _stuckTimer = 0f;
            Vector3 escape = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            _agent.SetDestination(transform.position + escape);
        }
    }



    private void SetDest(Vector3 target)
    {
        if (_agent == null || !_agent.enabled) return;
        _agent.isStopped = false;
        _agent.SetDestination(target);
    }
}