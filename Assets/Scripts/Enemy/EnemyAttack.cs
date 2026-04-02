using System.Collections;
using UnityEngine;
/// <summary>
/// todo:
/// bullet indicator genauer
/// sniper repositioneren sich nicht
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private TrailRenderer _bulletTrail;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private ParticleSystem _impactParticle;
    [SerializeField] private LayerMask _hitMask;
    [SerializeField] private float _range = 40f;
    [SerializeField] private float _trailSpeed = 8f;

    [Header("Bullet Indicator (Sniper)")]
    [SerializeField] private LineRenderer _bulletIndicator; 
    [SerializeField] private float _indicatorWidth = 0.02f;

    [Header("Sniper Windup")]
    [SerializeField] private float _sniperWindupTime = 0.7f;

    private EnemyController _controller;
    private EnemyStats _stats;
    private Transform _player;
    private Rigidbody _playerRb;

    private float _windupTimer;
    private bool _isWindingUp;



    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
        if (_bulletIndicator != null)
        {
            _bulletIndicator.positionCount = 2;
            _bulletIndicator.startWidth = _indicatorWidth;
            _bulletIndicator.endWidth = _indicatorWidth;
            _bulletIndicator.enabled = false;
        }
    }

    public void Init(EnemyStats stats, Transform player)
    {
        _stats = stats;
        _player = player;
    }

    private void FixedUpdate()
    {
        TickWindup();
        UpdateIndicator();
    }

    public void Fire()
    {
        if (_stats == null || _player == null) return;

        switch (_stats.enemyType)
        {
            case EnemyType.Ranged:
                ShootRaycast(_player.position);
                _controller.NotifyShotFired();
                break;

            case EnemyType.Sniper:
                if (!_isWindingUp)
                    StartWindup();
                break;
        }
    }

    private void StartWindup()
    {
        _isWindingUp = true;
        _windupTimer = _sniperWindupTime;

        if (_bulletIndicator != null)
            _bulletIndicator.enabled = true;
    }

    private void TickWindup()
    {
        if (!_isWindingUp) return;

        _windupTimer -= Time.fixedDeltaTime;
        if (_windupTimer > 0f) return;

        _isWindingUp = false;

        if (_bulletIndicator != null)
            _bulletIndicator.enabled = false;

        if (_player != null)
        {
            ShootRaycast(_player.position);
     
            _controller.NotifyShotFired();
        }
    }
    private void UpdateIndicator()
    {
        if (_bulletIndicator == null || !_bulletIndicator.enabled || _firePoint == null || _player == null) return;

        Vector3 dir = (_player.position - _firePoint.position).normalized;
        Vector3 endPoint = _firePoint.position + dir * _range;


        if (Physics.Raycast(_firePoint.position, dir, out RaycastHit hit, _range, _hitMask))
            endPoint = hit.point;

        _bulletIndicator.SetPosition(0, _firePoint.position);
        _bulletIndicator.SetPosition(1, endPoint);
    }

    private void ShootRaycast(Vector3 targetPos)
    {
        if (_firePoint == null)
        {
            Debug.LogWarning($"[EnemyAttack] {name} hat keinen FirePoint.");
            return;
        }

        if (_muzzleFlash != null)
            _muzzleFlash.Play();

        Vector3 dir = (targetPos - _firePoint.position).normalized;

        bool didHit = Physics.Raycast(_firePoint.position, dir, out RaycastHit hit, _range, _hitMask);
        Vector3 endPoint = didHit ? hit.point : _firePoint.position + dir * _range;

        if (_bulletTrail != null)
        {
            TrailRenderer trail = Instantiate(_bulletTrail, _firePoint.position, Quaternion.identity);
            StartCoroutine(MoveTrail(trail, endPoint));
        }

        Debug.DrawRay(_firePoint.position, dir * _range, Color.red, 0.5f);

        if (!didHit) return;

        if (_impactParticle != null)
            Instantiate(_impactParticle, hit.point, Quaternion.LookRotation(hit.normal));

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        target?.TakeDamage(_stats.damage, gameObject.name);
    }


    private IEnumerator MoveTrail(TrailRenderer trail, Vector3 endPoint)
    {
        float time = 0f;
        Vector3 start = trail.transform.position;
        float dist = Vector3.Distance(start, endPoint);

        while (time < 1f)
        {
            trail.transform.position = Vector3.Lerp(start, endPoint, time);
            time += Time.deltaTime / Mathf.Max(trail.time, 0.01f);
            yield return null;
        }

        trail.transform.position = endPoint;
        Destroy(trail.gameObject, trail.time);
    }
}