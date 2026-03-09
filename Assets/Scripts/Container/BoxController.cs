using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;

[RequireComponent(typeof(Rigidbody))]
public class BoxController : MonoBehaviour, IDamageable
{
    [SerializeField] ContainerData CData;
    [SerializeField] Rigidbody rb;

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed > CData.fallDamageThreshold)
        {
            float damage = (impactSpeed - CData.fallDamageThreshold) * CData.fallDamageMultiplier;
            TakeDamage(damage, "Fall");
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
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
        if (amount >= CData.health)
        {
            Die();
        }
        else
        {
            CData.health -= amount;
            CData.destroyParticles.Play();
        }
    }
    private void Die()
    {         

        Destroy(gameObject);
    }
}
