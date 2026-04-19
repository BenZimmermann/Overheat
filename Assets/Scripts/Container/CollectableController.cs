using System.Collections;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class CollectableController : MonoBehaviour, ICollectable
{
    //later as a ScriptableObject to allow for different types of collectables with different values and behaviors
    // how much this collectable is worth, can be set in inspector for different types of collectables
    [SerializeField] private CollectableData collectData;
    [SerializeField] private LineRenderer _trail;

    private Rigidbody _rb;
    private Transform _player;
    private bool _attracting = false;
    private float _spawnTime;
    public float _attractRadius => collectData.attractRadius + GameManager.Instance.Data.AttractRadiusBonus;


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        //change later to a more dynamic solution
        _player = GameObject.FindWithTag("Player").transform;
        _spawnTime = Time.time;

        if (_trail != null)
            _trail.positionCount = 0;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;
        if (Time.time - _spawnTime < collectData.pickupDelay) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= _attractRadius)
        {
            _attracting = true;
        }

        if (_attracting)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            Vector3 direction = (_player.position - transform.position).normalized;
            _rb.MovePosition(transform.position + direction * collectData.attractSpeed * Time.fixedDeltaTime);

            UpdateTrail();

            if (distance <= collectData.pickupRadius)
            {
                Collect(collectData.amount);
            }
        }
    }
    private void UpdateTrail()
    {
        if (_trail == null) return;

        _trail.positionCount = 20;
        _trail.SetPosition(0, transform.position);
        _trail.SetPosition(1, _player.position);
    }
    public void Collect(float amount)
    {
        SoundManager.Instance.Play3DSound(SoundType.CollectMoney, transform.position);
        GameManager.Instance.Data.Money += amount;
        Destroy(gameObject);
    }
}
