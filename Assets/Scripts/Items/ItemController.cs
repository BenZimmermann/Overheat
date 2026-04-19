using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class ItemController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActionAsset;
    public string actionMapName = "Player";
    public string useItemActionName = "UseItem";

    private InputAction _useItemAction;

    [Header("References")]
    [SerializeField] private Transform _throwOrigin;
    [SerializeField] private LayerMask _teleportMask;

    // Timers
    private float _magneticFieldTimer;
    private float _goldenGunTimer;
    private float _cooldownTimer;

    // Grenade
    private GameObject _activeGrenade;
    private float _grenadeTimer;
    private float _grenadeExplosionRadius;
    private float _grenadeExplosionDamage;

    // Shield
    private GameObject _activeShield;
    [SerializeField] private ScriptableRendererFeature _fullScreenShield;
    [SerializeField] private Material _material;

    // Golden Gun
    private Material[] _originalMaterials;
    private Renderer[] _weaponRenderers;

    private GameObject _explosionPrefab;
    public bool GoldenGunActive => _goldenGunTimer > 0f;
    public bool MagneticFieldActive => _magneticFieldTimer > 0f;
    public bool OnCooldown => _cooldownTimer > 0f;

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
    private void Start()
    {
        _fullScreenShield.SetActive(false);
    }
    private void Update()
    {
        if (_useItemAction == null || !_useItemAction.WasPressedThisFrame()) return;
        if (OnCooldown) return;

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
        TickCooldown();
    }

    // -------------------------------------------------------------------------
    // Timers
    // -------------------------------------------------------------------------

    private void TickCooldown()
    {
        if (_cooldownTimer <= 0f) return;
        _cooldownTimer -= Time.fixedDeltaTime;
        if (_cooldownTimer < 0f) _cooldownTimer = 0f;
    }

    private void TickMagneticField()
    {
        if (_magneticFieldTimer <= 0f) return;
        _fullScreenShield.SetActive(true);
        _magneticFieldTimer -= Time.fixedDeltaTime;

        if (_magneticFieldTimer <= 0f)
        {
            _magneticFieldTimer = 0f;
            GameManager.Instance.Data.MagneticFieldActive = false;
            _fullScreenShield.SetActive(false);
            DestroyShield();
            Debug.Log("[ItemController] Magnetic Field abgelaufen.");
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
            RestoreWeaponMaterials();
            Debug.Log("[ItemController] Golden Gun abgelaufen.");
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

        if (_explosionPrefab != null)
        {
            SoundManager.Instance.Play3DSound(SoundType.Exposion, _activeGrenade.transform.position);
            GameObject explosion = Instantiate(_explosionPrefab, _activeGrenade.transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }
        Collider[] hits = Physics.OverlapSphere(_activeGrenade.transform.position, _grenadeExplosionRadius);
        foreach (Collider col in hits)
        {
            IDamageable target = col.GetComponentInParent<IDamageable>();
            target?.TakeDamage(_grenadeExplosionDamage, "Grenade");
        }

        Debug.Log($"[ItemController] Explosion bei {_activeGrenade.transform.position}");
        Destroy(_activeGrenade);
        _activeGrenade = null;
        _explosionPrefab = null;
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
    }

    public void UseRevive(ItemData item)
    {
        SoundManager.Instance.PlaySound(SoundType.UseItem);
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(item.healthRegen);
        playerHealth.AddShield(item.shieldRegen);
        Debug.Log($"[ItemController] Revive: +{item.healthRegen} HP");

        ConsumeItem();
    }

    // -------------------------------------------------------------------------
    // Items
    // -------------------------------------------------------------------------

    private void UseGrenade(ItemData item)
    {
        if (item.itemGrenadeModel == null)
        {
            Debug.LogWarning("[ItemController] Grenade hat kein itemGrenadeModel.");
            return;
        }
        SoundManager.Instance.PlaySound(SoundType.UseItem);
        Transform origin = _throwOrigin != null ? _throwOrigin : transform;
        _activeGrenade = Instantiate(item.itemGrenadeModel, origin.position, origin.rotation);

        Rigidbody rb = _activeGrenade.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(origin.forward * 15f + Vector3.up * 5f, ForceMode.Impulse);

        _grenadeTimer = item.effectDuration > 0f ? item.effectDuration : 2f;
        _grenadeExplosionRadius = item.effectRadius;
        _grenadeExplosionDamage = item.explosionDamage;

        _explosionPrefab = item.Explosion;

        Debug.Log("[ItemController] Granate geworfen.");
        ConsumeItem();
    }

    private void UseMagneticField(ItemData item)
    {
        SoundManager.Instance.PlaySound(SoundType.UseItem);
        _magneticFieldTimer = item.effectDuration;
        GameManager.Instance.Data.MagneticFieldActive = true;

        //if (item.shieldObject != null)
        //{
        //    Transform origin = _throwOrigin != null ? _throwOrigin : transform;
        //    _activeShield = Instantiate(item.shieldObject, origin.position, Quaternion.identity, origin);
        //}

        StartItemCooldown(item);
        Debug.Log($"[ItemController] Magnetic Field aktiv für {item.effectDuration}s");
    }

    private void UseGoldenGun(ItemData item)
    {
        SoundManager.Instance.PlaySound(SoundType.UseItem);
        _goldenGunTimer = item.effectDuration;
        GameManager.Instance.Data.GoldenGunActive = true;

        if (item.goldenGunMaterial != null)
            ApplyWeaponMaterial(item.goldenGunMaterial);

        ConsumeItem();
    }

    private void UseTeleport(ItemData item)
    {
        SoundManager.Instance.PlaySound(SoundType.UseItem);
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, item.effectRadius, _teleportMask))
            transform.position = hit.point + Vector3.up * 1f;
        else
            transform.position += ray.direction * item.effectRadius;

        StartItemCooldown(item);
        Debug.Log($"[ItemController] Teleportiert zu {transform.position}");
    }

    private void UseElexir(ItemData item)
    {
        SoundManager.Instance.PlaySound(SoundType.UseItem);
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(item.healthRegen);
        playerHealth.AddShield(item.shieldRegen);
        Debug.Log($"[ItemController] Elexier: +{item.healthRegen} HP, +{item.shieldRegen} Shield");
        ConsumeItem();
    }

    // -------------------------------------------------------------------------
    // Waffen-Material
    // -------------------------------------------------------------------------

    private void ApplyWeaponMaterial(Material mat)
    {
        Transform weaponPivot = GameObject.FindGameObjectWithTag("weaponPivot")?.transform;
        if (weaponPivot == null || weaponPivot.childCount == 0) return;

        _weaponRenderers = weaponPivot.GetChild(0).GetComponentsInChildren<Renderer>();
        _originalMaterials = new Material[_weaponRenderers.Length];

        for (int i = 0; i < _weaponRenderers.Length; i++)
        {
            _originalMaterials[i] = _weaponRenderers[i].sharedMaterial;
            _weaponRenderers[i].sharedMaterial = mat;
        }
    }

    private void RestoreWeaponMaterials()
    {
        if (_weaponRenderers == null) return;

        for (int i = 0; i < _weaponRenderers.Length; i++)
        {
            if (_weaponRenderers[i] != null && _originalMaterials[i] != null)
                _weaponRenderers[i].sharedMaterial = _originalMaterials[i];
        }

        _weaponRenderers = null;
        _originalMaterials = null;
    }

    // -------------------------------------------------------------------------
    // Shield
    // -------------------------------------------------------------------------

    private void DestroyShield()
    {
        if (_activeShield != null)
        {
            Destroy(_activeShield);
            _activeShield = null;
        }
    }

    // -------------------------------------------------------------------------
    // Item verbrauchen
    // -------------------------------------------------------------------------

    private void StartItemCooldown(ItemData item)
    {
        if (item.cooldown <= 0f) return;
        _cooldownTimer = item.cooldown;

        HUDController hud = FindAnyObjectByType<HUDController>();
        hud?.StartCooldown(item.cooldown, item);
    }

    private void ConsumeItem()
    {
        GameManager.Instance.Data.SetItem(null);
    }
}