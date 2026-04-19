using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;

[RequireComponent(typeof(Rigidbody))]
public class BoxController : MonoBehaviour, IDamageable
{
    [SerializeField] ContainerData CData;
    [SerializeField] Rigidbody rb;
    [SerializeField] ParticleSystem DestroyParticles;
    private float _currentHealth;

    private void Start()
    {
        _currentHealth = CData.health;
        //change later to a more dynamic solution
        Physics.IgnoreLayerCollision(8, 13);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == CData.floorMask)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed > CData.fallDamageThreshold)
            {
                float damage = (impactSpeed - CData.fallDamageThreshold) * CData.fallDamageMultiplier;
                TakeDamage(damage, "Fall");
            }
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            TakeDamage( 1f, collision.gameObject.name);
            CData.hitParticles.Play();
        }
    }
    private void FixedUpdate()
    {
        FallDamage();
    }

    private void FallDamage()
    {
      if (rb.linearVelocity.magnitude > 10f)
        {
            TakeDamage(CData.fallDamageThreshold, name);
        }
    }
    public void TakeDamage(float amount, string Source)
    {
        _currentHealth -= amount;
        CData.hitParticles.Play();
        SoundManager.Instance.Play3DSound(SoundType.DestroyBox, transform.position);
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        //could destroy some other particles
        DestroyParticles.transform.SetParent(null);
        DestroyParticles.transform.position = transform.position;
        DestroyParticles.Play();
        float particleDuration = DestroyParticles.main.duration + DestroyParticles.main.startLifetime.constantMax;
        Destroy(DestroyParticles.gameObject, particleDuration);
        DropMoney();
        Destroy(gameObject);
    }
    private void DropMoney()
    {
        if (Random.value > CData.rewardChance) return;

        int amount = Mathf.RoundToInt(CData.rewardMoney);

        for (int i = 0; i < amount; i++)
        {
            GameObject coin = Instantiate(
                CData.MoneyObj,
                transform.position,
                Random.rotation
            );

            if (coin.TryGetComponent(out Rigidbody rb))
            {
                Vector3 randomDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f),  // immer leicht nach oben
                    Random.Range(-1f, 1f)
                ).normalized;

                float force = Random.Range(1f, 6f);
                rb.AddForce(randomDirection * force, ForceMode.Impulse);

                float torque = Random.Range(1f, 4f);
                rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            }
        }
    }
    private void DropItems()
    {
        //reward Money == amount of gameObjects to spawn
        //reawrd change == the odds to spawn the money
    }
    private void DropHealth()
    {

    }
}
