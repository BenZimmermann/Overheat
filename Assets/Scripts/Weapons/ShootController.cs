using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(Animator))]
public class ShootController : MonoBehaviour
{
    /// <summary>
    /// später als Scriptable Object umsetzen, damit man die Werte im Editor anpassen kann, ohne den Code zu ändern
    /// </summary>
    [SerializeField] bool AddBulletSpread = false;
    [SerializeField] private Vector3 BulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private float ShootDelay = 0.2f;

    [SerializeField] private ParticleSystem ShootingSystem;
    [SerializeField] private Transform BulletSpawnPoint;
    [SerializeField] private ParticleSystem ImpactParticleSystem;
    [SerializeField] private TrailRenderer BulletTrail;
    [SerializeField] private LayerMask Mask;
    private Animator animator;
    private float _lastShootTime;

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
        if (_shootAction != null && _shootAction.IsPressed())
            Shoot();
    }

        public void Shoot()
    {
        if(_lastShootTime + ShootDelay < Time.time)
        {
            
            //animator.SetBool("IsShooting", true);
            ShootingSystem.Play();
            Vector3 direction = GetDirection();

            if(Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit Hit, float.MaxValue, Mask))
            {
                TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);

                StartCoroutine(SpawnTrail(trail, Hit));

                _lastShootTime = Time.time;
            }
            Debug.Log("Shoot");
        }
    }

    private Vector3 GetDirection()
    {
        Vector3 direction = -transform.right;
        if (AddBulletSpread)
        {
            direction += new Vector3(
                Random.Range(-BulletSpreadVariance.x, BulletSpreadVariance.x),
                Random.Range(-BulletSpreadVariance.y, BulletSpreadVariance.y),
                Random.Range(-BulletSpreadVariance.z, BulletSpreadVariance.z)
            );
            direction.Normalize();
        }
        return direction;
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
    }
}
