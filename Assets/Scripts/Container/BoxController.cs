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
    // Checks for collisions with the floor to apply fall damage and with players or enemies to apply hit damage.
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
    //physic
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
    //takes damage from all sources and checks for death
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
    //handles the death of the box, plays particles, drops rewards and destroys the gameobject
    private void Die()
    {
        //could destroy some other particles
        DestroyParticles.transform.SetParent(null);
        DestroyParticles.transform.position = transform.position;
        DestroyParticles.Play();
        float particleDuration = DestroyParticles.main.duration + DestroyParticles.main.startLifetime.constantMax;
        Destroy(DestroyParticles.gameObject, particleDuration);
        DropMoney();
        DropHeal();
        Destroy(gameObject);
    }
    //drops money based on the CData
    private void DropMoney()
    {
        if (Random.value > CData.rewardChance) return;

        int amount = Mathf.RoundToInt(CData.rewardMoney);

        for (int i = 0; i < amount; i++)
        {
            // Instantiate the coin prefab at the box's position with a random rotation
            GameObject coin = Instantiate(
                CData.MoneyObj,
                transform.position,
                Random.rotation
            );

            if (coin.TryGetComponent(out Rigidbody rb))
            {
                // Apply a random force to the coin to make it jump up and scatter around
                Vector3 randomDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f),  // immer leicht nach oben
                    Random.Range(-1f, 1f)
                ).normalized;
                // Randomize the force magnitude for more variety
                float force = Random.Range(1f, 6f);
                rb.AddForce(randomDirection * force, ForceMode.Impulse);
                // Apply random torque to make the coin spin
                float torque = Random.Range(1f, 4f);
                rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            }
        }
    }
    //drops health based on the CData
    private void DropHeal()
    {
        if (Random.value > CData.rewardhealth) return;

        Quaternion spawnRotation = Quaternion.Euler(-90f, 0f, 0f);
        // Instantiate the heal prefab at the box's position with a fixed rotation
        GameObject heal = Instantiate(
            CData.HealObj,
            transform.position + Vector3.up * 0.1f,
            spawnRotation
        );


        if (heal.TryGetComponent(out Rigidbody rb))
        {
            // Apply a random force to the heal item to make it jump up and scatter around
            Vector3 jumpDirection = new Vector3(
                Random.Range(-0.5f, 0.5f),
                1f,
                Random.Range(-0.5f, 0.5f)
            ).normalized;

            rb.AddForce(jumpDirection * 3f, ForceMode.Impulse);
        }
    }

}
