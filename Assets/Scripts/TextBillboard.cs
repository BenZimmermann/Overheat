using UnityEngine;
using TMPro;

/// <summary>
/// updates the rotation of the text to always face the camera, making it easier to read from any angle.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Transform _mainCameraTransform;

    [SerializeField] private bool _invertRotation = false;

    void Start()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (_mainCameraTransform == null) return;
        // Set the rotation of the text to match the camera's rotation
        transform.rotation = _mainCameraTransform.rotation;

        // Optionally invert the rotation to make the text face away from the camera
        if (_invertRotation)
        {
            transform.Rotate(0, 180, 0);
        }
    }
}