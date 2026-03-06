using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public string weaponID;

    public bool isMelee = false;

    public float damage = 10f;
    public float range = 100f;
    public float attackRadius = 0.5f; 

    public float fireRate = 0.2f;

    public float recoil = 0f;

    public float reloadTime = 1f;
    public int magazineSize = 30;

    public float CooldownTime = 0f;
    public bool isAutomatic = false;

    public float ShootDelay = 0.2f;
    public bool AddBulletSpread = false;
    public Vector3 BulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);

}