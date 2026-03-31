using UnityEngine;
[CreateAssetMenu(menuName = "Enemy/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("General")]
    public string enemyName;
    public float moneyDropChance;
    public float moneyDropAmount;
    public GameObject moneyObj;

    [Header("Health")]
    public float maxHealth;
    public float headShotMultiplier;

    [Header("Movement")]
    public float moveSpeed;
    public float attackRange;
    public float repositionRange;
    public float detectionRange;

    [Header("Hit Reaction")]
    public float stunDuration = 0.25f;
    public float slowOnHit = 0.5f;

    [Header("Attack")]
    public float damage;
    public float attackCooldown;
    public int shotsBeforeReposition;


    [Header("Enemy Type")]
    public EnemyType enemyType;
}
public enum EnemyType
{
    Meele,
    Ranged,
    Sniper,
    Bomber
}