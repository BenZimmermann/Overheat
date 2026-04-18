using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalController : MonoBehaviour
{
    [SerializeField, Tooltip("Only use when portals are linked inside one scene")]
    private bool _usePortalName;
    [SerializeField, Tooltip("Only use when portals are linked inside one scene")]
    private GameObject _portalToTeleport;
    [SerializeField] private string _levelName;
    [SerializeField] private bool _isEndPortal;
    [SerializeField] private float _exitOffset = 1.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        if (_isEndPortal)
        {
            GameManager.Instance.FinishGame();
        }

        if (_usePortalName)
        {
            if (_portalToTeleport != null)
            {
                Vector3 exitPosition = _portalToTeleport.transform.position
                                     + _portalToTeleport.transform.forward * _exitOffset;
                other.transform.position = exitPosition;
            }
            else
            {
                Debug.LogWarning("Portal: kein Ziel-Portal gesetzt!");
            }
        
        }
        else
        {
            if (string.IsNullOrEmpty(_levelName))
                Debug.Log($"teleport to{_levelName}");
                SceneManager.LoadScene(_levelName);
        }

    }
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 tip = origin + forward * _exitOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, tip);

        Vector3 right = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
        Gizmos.DrawLine(tip, tip + right * 0.3f);
        Gizmos.DrawLine(tip, tip + left * 0.3f);

        Gizmos.color = Color.yellow;
        float crossSize = 0.2f;
        Gizmos.DrawLine(origin - Vector3.right * crossSize, origin + Vector3.right * crossSize);
        Gizmos.DrawLine(origin - Vector3.up * crossSize, origin + Vector3.up * crossSize);
        Gizmos.DrawLine(origin - Vector3.forward * crossSize, origin + Vector3.forward * crossSize);

        if (_portalToTeleport != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, _portalToTeleport.transform.position);
        }
    }
}
