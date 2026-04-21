using UnityEngine;

public class HealthItemController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _healAmount = 20f;
    [SerializeField] private float _pickupDelay = 0.5f;
    [SerializeField] private float _pickupRadius = 0.5f;

    private Rigidbody _rb;
    private Transform _player;
    private bool _attracting = false;
    private float _spawnTime;

    private float AttractRadius => 5f + GameManager.Instance.Data.AttractRadiusBonus;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        _spawnTime = Time.time;
    }
    // This method is called at fixed intervals and handles the attraction of the health item towards the player and its collection when close enough.
    private void FixedUpdate()
    {
        if (_player == null) return;

        if (Time.time - _spawnTime < _pickupDelay) return;
        // Check distance to player
        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= AttractRadius)
        {
            _attracting = true;
        }

        if (_attracting)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            // Move towards the player
            Vector3 direction = (_player.position - transform.position).normalized;
            float moveSpeed = 10f; 
            _rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (distance <= _pickupRadius)
            {
                Collect();
            }
        }
    }
    // This method handles the collection of the health item by the player.
    private void Collect()
    {
        PlayerHealth health = _player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Heal the player
            health.Heal(_healAmount); 
            SoundManager.Instance.Play3DSound(SoundType.UseItem, transform.position);
            Destroy(gameObject);
        }
    }
}