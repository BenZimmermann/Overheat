using UnityEngine;
using TMPro;

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

        transform.rotation = _mainCameraTransform.rotation;

        if (_invertRotation)
        {
            transform.Rotate(0, 180, 0);
        }
    }
}