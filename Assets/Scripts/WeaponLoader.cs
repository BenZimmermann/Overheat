using UnityEngine;

public class WeaponLoader : MonoBehaviour
{
    private void Start()
    {
        WeaponData currentWeapon = GameManager.Instance.Data.CurrentWeapon;
        if (currentWeapon == null)
        { 
            return;
        }

        Transform pivot = GameObject.FindGameObjectWithTag("weaponPivot")?.transform;
        if (pivot == null)
        {
            return;
        }

        if (pivot.childCount > 0)
            Destroy(pivot.GetChild(0).gameObject);

        GameObject newWeapon = Instantiate(currentWeapon.WeaponObj, pivot);
        newWeapon.transform.localPosition = currentWeapon.WeaponObj.transform.localPosition;
        newWeapon.transform.localRotation = currentWeapon.WeaponObj.transform.localRotation;
    }
}
