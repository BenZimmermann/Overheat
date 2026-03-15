using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(Animator))]
public class ShootController : MonoBehaviour
{
    [SerializeField] private WeaponData Wdata;

    [SerializeField] private ParticleSystem ShootingSystem;
    [SerializeField] private Transform BulletSpawnPoint;
    [SerializeField] private ParticleSystem ImpactParticleSystem;
    [SerializeField] private TrailRenderer BulletTrail;

    [SerializeField] private LayerMask Mask;
    [SerializeField] private LayerMask Damageable;


    private Camera aimCamera;

    private Animator animator;
    private float _lastShootTime;
    private int _currentAmmo;
    private bool _isReloading;
    private Coroutine _reloadCoroutine;

    private float _baseReloadTime;
    private float _baseShootDelay;

    private bool _isSalveActive;
    private int _salveRemaining;
    private float _salveTimer;

    [Header("Input Actions")]
    public InputActionAsset inputActionAsset;

    [Tooltip("Name of the Action Map inside the asset.")]

    public string actionMapName = "Player";
    public string reloadActionName = "Reload";
    public string shootActionName = "Attack";

    InputAction _shootAction;
    InputAction _reloadAction;

    //private void Awake()
    //{
    //    animator = GetComponent<Animator>();
    //}
    private void Start()
    {
        _baseReloadTime = Wdata.reloadTime;
        _baseShootDelay = Wdata.fireRate;
        _currentAmmo = Wdata.magazineSize;
        aimCamera = GameObject.FindWithTag("Camera")?.GetComponent<Camera>();

    }
    private float CurrentReloadTime =>
        Mathf.Max(0.1f, _baseReloadTime - GameManager.Instance.Data.FastReload);
    private float CurrentShootDelay =>
        Mathf.Max(_baseShootDelay - GameManager.Instance.Data.FastFire);

    void BindInputActions()
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("[SUPERCharacter] No InputActionAsset assigned!");
            return;
        }
        var map = inputActionAsset.FindActionMap(actionMapName, throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogWarning($"[SUPERCharacter] Action Map '{actionMapName}' not found.");
            return;
        }
        _shootAction = map.FindAction(shootActionName, false);
        _reloadAction = map.FindAction(reloadActionName, false);

        map.Enable();
    }
    private void OnEnable() { BindInputActions(); }
    private void OnDisable() { inputActionAsset?.FindActionMap(actionMapName, false)?.Disable(); }

    public void Update()
    {
        if (Wdata.isMelee) return;
        if (Wdata.isAutomatic)
        {
            if (_shootAction != null && _shootAction.IsPressed())
                Shoot();
        }
        else
        {
            if (_shootAction != null && _shootAction.WasPressedThisFrame())
                Shoot();
        }
        if (_reloadAction != null && _reloadAction.WasPressedThisFrame())
            Reload();
    }
    private void FixedUpdate()
    {
        TickSalve();
    }
    public void Shoot()
    {
        if (_isReloading || _currentAmmo <= 0) return;

        if (_currentAmmo <= 0 && _reloadAction != null && _reloadAction.WasPressedThisFrame())
        {
            StartReload();
            return;
        }
        if (_lastShootTime + CurrentShootDelay < Time.time)
        {
            _lastShootTime = Time.time;

            if (Wdata.isSalve && !_isSalveActive)
                StartSalve();
            else if (Wdata.isShotgun)
                ShootShotgun();
            else
                FireShot();
        }
    }

    private void StartSalve()
    {
        _salveRemaining = Wdata.salveCount;
        _isSalveActive = true;
        _salveTimer = 0f;
    }

    private void TickSalve()
    {
        if (!_isSalveActive) return;

        _salveTimer -= Time.fixedDeltaTime;
        if (_salveTimer > 0f) return;

        if (_currentAmmo <= 0) { _isSalveActive = false; return; }

        FireShot();
        _salveRemaining--;
        _salveTimer = Wdata.salveDelay;

        if (_salveRemaining <= 0)
            _isSalveActive = false;
    }

    private void ShootShotgun()
    {
        if (_currentAmmo <= 0) return;
        _currentAmmo--;
        ShootingSystem.Play();
        _lastShootTime = Time.time;

        // Schaden pro IDamageable akkumulieren – nah treffen mehr Pellets = mehr Schaden
        Dictionary<IDamageable, float> damageAccum = new Dictionary<IDamageable, float>();
        Dictionary<IDamageable, RaycastHit> hitInfo = new Dictionary<IDamageable, RaycastHit>();

        Vector3 baseDir = GetGunDirection();

        for (int i = 0; i < Wdata.shotgunPellets; i++)
        {
            Vector3 pelletDir = (baseDir + new Vector3(
                Random.Range(-Wdata.shotgunSpread, Wdata.shotgunSpread),
                Random.Range(-Wdata.shotgunSpread, Wdata.shotgunSpread),
                Random.Range(-Wdata.shotgunSpread, Wdata.shotgunSpread))).normalized;

            RaycastHit pelletHit;
            Camera cam = aimCamera != null ? aimCamera : Camera.main;
            Ray screenRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            bool didHit = Physics.Raycast(BulletSpawnPoint.position, pelletDir, out pelletHit, Wdata.range, Mask)
                       || Physics.Raycast(screenRay, out pelletHit, Wdata.range, Mask);

            if (!didHit) continue;

            // Trail spawnen
            TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, pelletHit, skipDamage: true));

            IDamageable target = pelletHit.collider.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            if (damageAccum.ContainsKey(target))
                damageAccum[target] += Wdata.damage;
            else
            {
                damageAccum[target] = Wdata.damage;
                hitInfo[target] = pelletHit;
            }
        }

        // Gesamtschaden einmal pro Ziel anwenden
        foreach (var kvp in damageAccum)
            kvp.Key.TakeDamage(kvp.Value, Wdata.name);
    }

    private void FireShot()
    {
        if (_currentAmmo <= 0) return;
        _currentAmmo--;
        ShootingSystem.Play();
        FireRay(GetGunDirection());
    }

    private Vector3 GetGunDirection()
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        Ray screenRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 gunDir = screenRay.direction;

        if (Physics.Raycast(screenRay, out RaycastHit screenHit, Wdata.range, Mask))
        {
            Vector3 toHit = screenHit.point - BulletSpawnPoint.position;
            if (Vector3.Dot(toHit, screenRay.direction) > 0f)
                gunDir = toHit.normalized;
        }

        if (Wdata.AddBulletSpread)
        {
            gunDir += new Vector3(
                Random.Range(-Wdata.BulletSpreadVariance.x, Wdata.BulletSpreadVariance.x),
                Random.Range(-Wdata.BulletSpreadVariance.y, Wdata.BulletSpreadVariance.y),
                Random.Range(-Wdata.BulletSpreadVariance.z, Wdata.BulletSpreadVariance.z));
            gunDir.Normalize();
        }

        Debug.DrawRay(BulletSpawnPoint.position, gunDir * Wdata.range, Color.red, 1f);
        return gunDir;
    }

    private void FireRay(Vector3 direction)
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        Ray screenRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        RaycastHit finalHit;
        bool didHit = Physics.Raycast(BulletSpawnPoint.position, direction, out finalHit, Wdata.range, Mask)
                   || Physics.Raycast(screenRay, out finalHit, Wdata.range, Mask);

        if (!didHit) return;

        TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, finalHit));
    }

    private IEnumerator SpawnTrail(TrailRenderer Trail, RaycastHit Hit, bool skipDamage = false)
    {
        float time = 0f;
        Vector3 startPosition = Trail.transform.position;
        while (time < 1f)
        {
            Trail.transform.position = Vector3.Lerp(startPosition, Hit.point, time);
            time += Time.deltaTime / Trail.time;
            yield return null;
        }
        Trail.transform.position = Hit.point;
        Instantiate(ImpactParticleSystem, Hit.point, Quaternion.LookRotation(Hit.normal));
        Destroy(Trail.gameObject, Trail.time);

        if (!skipDamage && (Mask.value & (1 << Hit.collider.gameObject.layer)) > 0)
            Damage(Hit);
    }
    private void Reload()
    {
        if (_isReloading || _currentAmmo == Wdata.magazineSize) return;
        StartReload();
    }

    private void StartReload()
    {
        if (_isReloading) return;
        if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        yield return new WaitForSeconds(CurrentReloadTime);
        _currentAmmo = Wdata.magazineSize;
        _isReloading = false;
    }
    private void Damage(RaycastHit hit)
    {
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        target?.TakeDamage(Wdata.damage, Wdata.name);
    }
    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => Wdata.magazineSize;
    public bool IsReloading => _isReloading;
}