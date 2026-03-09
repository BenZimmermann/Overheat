using UnityEngine;
using TMPro;

public class TestDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private TMP_Text damageText;
    private float _currentDamage;
    private float damage(float amount)
    {
        return _currentDamage + amount;
    }
    public void TakeDamage(float amount, string Source)
    {
        _currentDamage = damage(amount);

        damageText.text = Mathf.RoundToInt(_currentDamage).ToString();
    }
}