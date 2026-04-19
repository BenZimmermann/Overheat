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
    [SerializeField] private PortalDirection _exitDirection;

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        if (_isEndPortal)
        {
            GameManager.Instance.FinishGame();
            return; 
        }

        if (_usePortalName)
        {
            TeleportPlayer(other.transform);
        }
        else
        {
            if (!string.IsNullOrEmpty(_levelName))
            {
                SceneManager.LoadScene(_levelName);
            }
        }
    }
    private void TeleportPlayer(Transform playerTransform)
    {
        if (_portalToTeleport == null)
        {
            Debug.LogWarning("Portal: kein Ziel-Portal gesetzt!");
            return;
        }
        SoundManager.Instance.Play3DSound(SoundType.PortalWarp, playerTransform.position);
        Vector3 directionVector = Vector3.zero;
        float yRotation = 0f;


        switch (_exitDirection)
        {
            case PortalDirection.North:
                directionVector = Vector3.forward; 
                yRotation = 0f;
                break;
            case PortalDirection.South:
                directionVector = Vector3.back; 
                yRotation = 180f;
                break;
            case PortalDirection.East:
                directionVector = Vector3.right; 
                yRotation = 90f;
                break;
            case PortalDirection.West:
                directionVector = Vector3.left;   
                yRotation = 270f;
                break;
        }


        Vector3 exitPosition = _portalToTeleport.transform.position + (directionVector * _exitOffset);
        playerTransform.position = exitPosition;
        playerTransform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
    //made with the help of gemini to visualize the exit direction and the linked portal in the editor
    private void OnDrawGizmos()
    {
        Vector3 directionVector = Vector3.forward; 

        switch (_exitDirection)
        {
            case PortalDirection.North: directionVector = Vector3.forward; break;
            case PortalDirection.South: directionVector = Vector3.back; break;
            case PortalDirection.East: directionVector = Vector3.right; break;
            case PortalDirection.West: directionVector = Vector3.left; break;
        }

        Vector3 targetPortalPos = (_portalToTeleport != null) ? _portalToTeleport.transform.position : transform.position;

        Vector3 origin = targetPortalPos;
        Vector3 tip = origin + directionVector * _exitOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, tip); 

        Quaternion lookRot = Quaternion.LookRotation(directionVector, Vector3.up);
        Vector3 rightWing = lookRot * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 leftWing = lookRot * Quaternion.Euler(0, -150, 0) * Vector3.forward;

        Gizmos.DrawLine(tip, tip + rightWing * 0.3f);
        Gizmos.DrawLine(tip, tip + leftWing * 0.3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, 0.1f);


        if (_portalToTeleport != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawLine(transform.position, _portalToTeleport.transform.position);
        }
    }
}
public enum PortalDirection { North, South, East, West }