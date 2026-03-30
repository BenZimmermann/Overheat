using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _maxShield;

    [SerializeField] private float _shieldDamageMultiplier = 2f; 
    [SerializeField] private float _bleedThroughPercent = 0.2f;

    [SerializeField] private ScriptableRendererFeature _fullScreenDamage;
    [SerializeField] private Material _material;

    void Start()
    {
        var data = GameManager.Instance.Data;

        if (data.PlayerHealth <= 0)
            data.PlayerHealth = (int)_maxHealth;

        if (data.PlayerShild <= 0)
            data.PlayerShild = (int)_maxShield;
    }


    public void TakeDamage(float amount, string source)
    {
        var data = GameManager.Instance.Data;

        if (data.MagneticFieldActive) return;

        amount = ApplyDamageReduction(amount, data);

        StartCoroutine(HurtEffect());

        if (data.PlayerShild > 0)
        {
            data.PlayerShild = (int)Mathf.Max(0, data.PlayerShild - _shieldDamageMultiplier);
            data.PlayerHealth = (int)(data.PlayerHealth - _bleedThroughPercent);
        }
        else
        {
            data.PlayerHealth = (int)(data.PlayerHealth - amount);
        }

        Lifesteal(amount);

        if (data.PlayerHealth <= 0)
            Die();
    }

    private void StartCourutine(IEnumerator enumerator)
    {
        throw new NotImplementedException();
    }

    private float ApplyDamageReduction(float amount, RuntimeGameData data)
    {
        if (data.LifestealPercent <= 0f) return amount;

        //UnityEngine.Random.value was made by AI
        bool triggered = UnityEngine.Random.value <= 0.2f;
        if (!triggered) return amount;

        float reduced = amount * (1f - data.LifestealPercent);
        return reduced;
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
    public void Lifesteal(float incomingDamage)
    {
        float percent = GameManager.Instance.Data.LifestealPercent;
        if (percent <= 0f) return;       

        float healAmount = incomingDamage * percent;
        Heal(healAmount);
    }
    public void AddLifesteal(float percent)
    {
        GameManager.Instance.Data.LifestealPercent += percent;
    }
    private IEnumerator HurtEffect()
    {
        _fullScreenDamage.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        _fullScreenDamage.SetActive(false);
    }
}
