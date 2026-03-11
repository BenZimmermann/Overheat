using UnityEngine;
using System.Collections;

public class CollectableController : MonoBehaviour, ICollectable
{
    //later as a ScriptableObject to allow for different types of collectables with different values and behaviors
    // how much this collectable is worth, can be set in inspector for different types of collectables
    [SerializeField] private CollectableData collectData;

    private Rigidbody _rb;
    private Transform _player;
    private bool _attracting = false;
    private float _spawnTime;
    

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        //change later to a more dynamic solution
        _player = GameObject.FindWithTag("Player").transform;
        _spawnTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;
        if (Time.time - _spawnTime < collectData.pickupDelay) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= collectData.attractRadius)
        {
            _attracting = true;
        }

        if (_attracting)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            Vector3 direction = (_player.position - transform.position).normalized;
            _rb.MovePosition(transform.position + direction * collectData.attractSpeed * Time.fixedDeltaTime);

            if (distance <= collectData.pickupRadius)
            {
                Collect(collectData.amount);
            }
        }
    }
    public void Collect(float amount)
    {
        GameManager.Instance.Data.Money += amount;
        Destroy(gameObject);
    }
}
