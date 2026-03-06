using TMPro;
using UnityEngine;

public class TestWeaponSwitch : MonoBehaviour, IDamageable
{
    //toDo:
    // - dont use Start() to find the weapon pivot
    [SerializeField] GameObject WeaponToSwitch;
    private Transform WeaponPivot;

    private void Start()
    {
        WeaponPivot = GameObject.FindGameObjectWithTag("weaponPivot").transform;
    }

    public void TakeDamage(float amount, string name)
    {
        SwitchWeapon();
    }

    private void SwitchWeapon()
    {
        if (WeaponPivot.childCount > 0)
            Destroy(WeaponPivot.GetChild(0).gameObject);

        GameObject newWeapon = Instantiate(WeaponToSwitch, WeaponPivot);
        newWeapon.transform.localPosition = WeaponToSwitch.transform.localPosition;
        newWeapon.transform.localRotation = WeaponToSwitch.transform.localRotation;
    }
}
