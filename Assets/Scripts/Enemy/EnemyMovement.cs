using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Bewegt den Enemy. Nutzt NavMeshAgent falls vorhanden, sonst direkte Bewegung.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private EnemyStats _stats;
    private Vector3 _repositionTarget;

    public float CurrentSpeed => _agent != null ? _agent.speed : _stats.moveSpeed;
    public bool ReachedDestination =>
        _agent == null || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);

    public void Init(EnemyStats stats)
    {
        _stats = stats;
        _agent = GetComponent<NavMeshAgent>();

        if (_agent != null)
        {
            _agent.speed = stats.moveSpeed;
            _agent.stoppingDistance = stats.attackRange * 0.9f;
        }
    }

    public void ChasePlayer(Vector3 playerPos)
    {
        MoveTo(playerPos);
    }

    public void MoveToReposition()
    {
        MoveTo(_repositionTarget);
    }

    public void Stop()
    {
        if (_agent != null)
            _agent.isStopped = true;
    }

    public void SetSpeed(float speed)
    {
        if (_agent != null)
            _agent.speed = speed;
    }

    public void SetRepositionTarget(Vector3 target)
    {
        _repositionTarget = target;
    }

    private void MoveTo(Vector3 target)
    {
        if (_agent != null)
        {
            _agent.isStopped = false;
            _agent.SetDestination(target);
        }
        else
        {
            // Fallback ohne NavMesh
            Vector3 dir = (target - transform.position).normalized;
            transform.position += dir * _stats.moveSpeed * Time.fixedDeltaTime;
        }
    }
}