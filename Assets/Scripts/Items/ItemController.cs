using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class ItemController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActionAsset;
    public string actionMapName = "Player";
    public string useItemActionName = "UseItem";

    private InputAction _useItemAction;

    [Header("Grenade")]
    [SerializeField] private Transform _throwOrigin;

    [Header("Teleport")]
    [SerializeField] private LayerMask _teleportMask;

    private float _magneticFieldTimer;
    private float _goldenGunTimer;


    private GameObject _activeGrenade;
    private float _grenadeTimer;
    private float _grenadeExplosionRadius;
    private float _grenadeExplosionDamage;

    public bool GoldenGunActive => _goldenGunTimer > 0f;
    public bool MagneticFieldActive => _magneticFieldTimer > 0f;

    private void OnEnable()
    {
        if (inputActionAsset == null) return;
        var map = inputActionAsset.FindActionMap(actionMapName, false);
        if (map == null) return;
        _useItemAction = map.FindAction(useItemActionName, false);
        map.Enable();
    }

    private void OnDisable()
    {
        inputActionAsset?.FindActionMap(actionMapName, false)?.Disable();
    }

    private void Update()
    {
        if (_useItemAction == null || !_useItemAction.WasPressedThisFrame()) return;

        ItemData item = GameManager.Instance.Data.CurrentItem;
        if (item == null) return;
        if (item.isAutomatic) return; 

        UseItem(item);
    }

    private void FixedUpdate()
    {
        TickMagneticField();
        TickGoldenGun();
        TickGrenade();
    }

    // -------------------------------------------------------------------------
    // Timers
    // -------------------------------------------------------------------------

    private void TickMagneticField()
    {
        if (_magneticFieldTimer <= 0f) return;

        _magneticFieldTimer -= Time.fixedDeltaTime;

        if (_magneticFieldTimer <= 0f)
        {
            _magneticFieldTimer = 0f;
            GameManager.Instance.Data.MagneticFieldActive = false;
            Debug.Log("[ItemUsageController] Magnetic Field abgelaufen.");
        }
    }

    private void TickGoldenGun()
    {
        if (_goldenGunTimer <= 0f) return;

        _goldenGunTimer -= Time.fixedDeltaTime;

        if (_goldenGunTimer <= 0f)
        {
            _goldenGunTimer = 0f;
            GameManager.Instance.Data.GoldenGunActive = false;
            Debug.Log("[ItemUsageController] Golden Gun abgelaufen.");
        }
    }

    private void TickGrenade()
    {
        if (_activeGrenade == null) return;

        _grenadeTimer -= Time.fixedDeltaTime;
        if (_grenadeTimer > 0f) return;

        Explode();
    }

    private void Explode()
    {
        if (_activeGrenade == null) return;

        Collider[] hits = Physics.OverlapSphere(_activeGrenade.transform.position, _grenadeExplosionRadius);
        foreach (Collider col in hits)
        {
            IDamageable target = col.GetComponentInParent<IDamageable>();
            target?.TakeDamage(_grenadeExplosionDamage, "Grenade");
        }

        Debug.Log($"[ItemUsageController] Explosion bei {_activeGrenade.transform.position}");
        Destroy(_activeGrenade);
        _activeGrenade = null;
    }

    // -------------------------------------------------------------------------
    // Item-Dispatch
    // -------------------------------------------------------------------------

    private void UseItem(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Grenade: UseGrenade(item); break;
            case ItemType.magneticField: UseMagneticField(item); break;
            case ItemType.GoldenGun: UseGoldenGun(item); break;
            case ItemType.Teleport: UseTeleport(item); break;
            case ItemType.Elexir: UseElexir(item); break;
            case ItemType.Revive: return;
        }

        //ConsumeItem();
    }

    public void UseRevive(ItemData item)
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(item.healthRegen);
        playerHealth.AddShield(item.shieldRegen);
        Debug.Log($"[ItemUsageController] Revive: +{item.healthRegen} HP");

        ConsumeItem();
    }

    // -------------------------------------------------------------------------
    // Item-Implementierungen
    // -------------------------------------------------------------------------

    private void UseGrenade(ItemData item)
    {
        if (item.itemModel == null)
        {
            Debug.LogWarning("[ItemUsageController] Grenade hat kein itemModel.");
            return;
        }

        Transform origin = _throwOrigin != null ? _throwOrigin : transform;
        _activeGrenade = Instantiate(item.itemGrenadeModel, origin.position, origin.rotation);

        Rigidbody rb = _activeGrenade.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(origin.forward * 15f + Vector3.up * 5f, ForceMode.Impulse);

        _grenadeTimer = item.effectDuration > 0f ? item.effectDuration : 2f;
        _grenadeExplosionRadius = item.effectRadius;
        _grenadeExplosionDamage = item.explosionDamage;

        Debug.Log("[ItemUsageController] Granate geworfen.");
        ConsumeItem();
    }

    private void UseMagneticField(ItemData item)
    {
        _magneticFieldTimer = item.effectDuration;
        GameManager.Instance.Data.MagneticFieldActive = true;
        Debug.Log($"[ItemUsageController] Magnetic Field aktiv für {item.effectDuration}s");
    }

    private void UseGoldenGun(ItemData item)
    {
        _goldenGunTimer = item.effectDuration;
        GameManager.Instance.Data.GoldenGunActive = true;
        Debug.Log($"[ItemUsageController] Golden Gun aktiv für {item.effectDuration}s");
        ConsumeItem();
    }

    private void UseTeleport(ItemData item)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, item.effectRadius, _teleportMask))
            transform.position = hit.point + Vector3.up * 1f;
        else
            transform.position += ray.direction * item.effectRadius;

        Debug.Log($"[ItemUsageController] Teleportiert zu {transform.position}");
    }

    private void UseElexir(ItemData item)
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(item.healthRegen);
        playerHealth.AddShield(item.shieldRegen);
        Debug.Log($"[ItemUsageController] Elexier: +{item.healthRegen} HP, +{item.shieldRegen} Shield");
        ConsumeItem();
    }

    private void ConsumeItem()
    {
        GameManager.Instance.Data.SetItem(null);
    }
}
