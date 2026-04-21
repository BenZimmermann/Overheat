using TMPro;
using UnityEngine;
// This script is for testing purposes only, it represents a simple enemy that can take damage
//this will be not used in the final game
public class TestEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] private TMP_Text HealthText;
    [SerializeField] private float MaxHealth = 50f;
    private float _currentHealth;
    private void Start()
    {
        _currentHealth = MaxHealth;
        HealthText.text = Mathf.RoundToInt(_currentHealth).ToString();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        damageable?.TakeDamage(10f, "Enemy");
    }
    public void TakeDamage(float amount, string Source)
    {
        _currentHealth -= amount;
        HealthText.text = Mathf.RoundToInt(_currentHealth).ToString();
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Destroy(gameObject);
        GameManager.Instance.Data.EnemiesKilled++;
    }
}
