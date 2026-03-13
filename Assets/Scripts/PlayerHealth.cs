using Mono.Cecil.Cil;
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _maxShield;

    [SerializeField] private float _shieldDamageMultiplier = 2f; 
    [SerializeField] private float _bleedThroughPercent = 0.2f;


    void Start()
    {
        //GameManager.Instance.Data.PlayerHealth = (int)_maxHealth;
        //GameManager.Instance.Data.PlayerShild = (int)_maxShield;
        var data = GameManager.Instance.Data;

        if (data.PlayerHealth <= 0)
            data.PlayerHealth = (int)_maxHealth;

        if (data.PlayerShild <= 0)
            data.PlayerShild = (int)_maxShield;
    }


    public void TakeDamage(float amount, string source)
    {
        var data = GameManager.Instance.Data;

        if (data.PlayerShild > 0)
        {
            data.PlayerShild = (int)Mathf.Max(0, data.PlayerShild - _shieldDamageMultiplier);
            data.PlayerHealth = (int)(data.PlayerHealth - _bleedThroughPercent);
        }
        else
        {
            data.PlayerHealth = (int)(data.PlayerHealth - amount);
        }

        if (data.PlayerHealth <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        SaveManager.Instance.SaveStats();
        GameManager.Instance.GameOver();
    }

    public void Heal(float amount)
    {
        var data = GameManager.Instance.Data;
        data.PlayerHealth = (int)Mathf.Min(_maxHealth, data.PlayerHealth + amount);
    }

    public void AddShield(float amount)
    {
        var data = GameManager.Instance.Data;
        data.PlayerShild = (int)Mathf.Min(_maxShield, data.PlayerShild + amount);
    }

    //upgrades
    public void AddMaxHealth(float amount)
    {
        _maxHealth += amount;
       var data = GameManager.Instance.Data;
        data.PlayerHealth = (int)Mathf.Min(_maxHealth, data.PlayerHealth + amount);
    }
    public void AddMaxShield(float amount)
    {
        _maxShield += amount;
        var data = GameManager.Instance.Data;
        data.PlayerShild = (int)Mathf.Min(_maxShield, data.PlayerShild + amount);
    }
}
