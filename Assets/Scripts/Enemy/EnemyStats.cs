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

    [Header("Dash (Melee)")]
    public float dashForce = 14f;
    public float dashCooldown = 2f;
    public float dashDuration = 0.12f;
    public float dashTriggerMult = 1.8f;

    [Header("Ranged Strafe")]
    public float preferredDist = 9f;
    public float retreatDist = 3.5f;
    public float strafeSpeed = 1f;
    public float strafeChange = 2f;

    [Header("Sniper")]
    public float sniperRetreatDist = 5f;
    public float sniperRetreatSpeed = 1.4f;

    [Header("Crowd Separation")]
    public float separationRadius = 2f;
    public float separationForce = 1.8f;

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