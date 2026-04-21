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

    // damages the player, applies shield and health damage, and checks for death
    public void TakeDamage(float amount, string source)
    {
        var data = GameManager.Instance.Data;
        if (data.MagneticFieldActive) return;
        // Apply damage reduction from lifesteal before applying damage
        amount = ApplyDamageReduction(amount, data);
        SoundManager.Instance.PlaySound(SoundType.DamagePlayer);
        StartCoroutine(HurtEffect());
        //shield absorbs damage first, then health takes bleed-through damage, if shield is depleted then health takes full damage
        if (data.PlayerShild > 0)
        {
            float shieldDamage = amount * _shieldDamageMultiplier;
            data.PlayerShild = (int)Mathf.Max(0, data.PlayerShild - shieldDamage);

            float healthDamage = amount * _bleedThroughPercent;
            data.PlayerHealth = (int)Mathf.Max(0, data.PlayerHealth - healthDamage);
        }
        else
        {
            data.PlayerHealth = (int)Mathf.Max(0, data.PlayerHealth - amount);
        }

        Lifesteal(amount);

        if (data.PlayerHealth <= 0)
            Die();
    }

    // Applies damage reduction based on lifesteal percentage, with a 20% chance to trigger
    private float ApplyDamageReduction(float amount, RuntimeGameData data)
    {
        if (data.LifestealPercent <= 0f) return amount;

        bool triggered = UnityEngine.Random.value <= 0.2f;
        if (!triggered) return amount;

        float reduced = amount * (1f - data.LifestealPercent);
        return reduced;
    }
    // handles player death, plays sound, saves stats, and triggers game over
    public void Die()
    {
        SoundManager.Instance.PlaySound(SoundType.PlayerDeath);
        _fullScreenDamage.SetActive(false);
        SaveManager.Instance.SaveStats();
        GameManager.Instance.GameOver();
    }
    //heals the player
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
    //effect
    private IEnumerator HurtEffect()
    {
        _fullScreenDamage.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        _fullScreenDamage.SetActive(false);
    }
}
