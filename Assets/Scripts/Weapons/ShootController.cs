using UnityEngine;
using System.Collections;
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
        aimCamera = GameObject.FindWithTag("Camera")?.GetComponent<Camera>();
    }
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
    if(Wdata.isMelee) return; // Prevent shooting if it's a melee weapon
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

    public void Shoot()
    {
        if (_isReloading || _currentAmmo <= 0) return;

        if (_currentAmmo <= 0 && _reloadAction != null && _reloadAction.WasPressedThisFrame())
        {
            StartReload();
            return;
        }
        if (_lastShootTime + Wdata.ShootDelay < Time.time)
        {
            //animator.SetBool("IsShooting", true);
            ShootingSystem.Play();
            _lastShootTime = Time.time;
            _currentAmmo--;


            Camera cam = aimCamera != null ? aimCamera : Camera.main;
            Ray screenRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            bool screenHitFound = Physics.Raycast(screenRay, out RaycastHit screenHit, Wdata.range, Mask);
            Vector3 gunDir = screenRay.direction;

            if (screenHitFound)
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

            RaycastHit finalHit;
            bool didHit = Physics.Raycast(BulletSpawnPoint.position, gunDir, out finalHit, Wdata.range, Mask)
                       || Physics.Raycast(screenRay, out finalHit, Wdata.range, Mask);

            if (didHit)
            {
                TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, finalHit));
            }
            // Debugging rays
            Debug.DrawRay(screenRay.origin, screenRay.direction * Wdata.range, Color.green, 1f);
            Debug.DrawRay(BulletSpawnPoint.position, gunDir * Wdata.range, Color.red, 1f);
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer Trail, RaycastHit Hit)
    {
        float time = 0f;
        Vector3 startPosition = Trail.transform.position;
        while (time < 1f)
        {
            Trail.transform.position = Vector3.Lerp(startPosition, Hit.point, time);
            time += Time.deltaTime / Trail.time;
            yield return null;
        }
        //animator.SetBool("IsShooting", false);
        Trail.transform.position = Hit.point;
        Instantiate(ImpactParticleSystem, Hit.point, Quaternion.LookRotation(Hit.normal));

        Destroy(Trail.gameObject, Trail.time);

        if ((Mask.value & (1 << Hit.collider.gameObject.layer)) > 0)
        {
            Damage(Hit);
        }
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
        yield return new WaitForSeconds(Wdata.reloadTime);
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
