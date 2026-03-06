using UnityEngine;
using Unity.Cinemachine;

public class WeaponBobController : MonoBehaviour
{

    public CinemachineCamera virtualCamera;
    public Vector3 cameraOffset = new Vector3(0.25f, -0.25f, 0.5f);

    [Header("Tilt")]
    public float tiltAmount = 4f;
    public float tiltSmooth = 10f;

    Quaternion _lastCamRot;
    Vector3 _tiltOffset;

    void Start()
    {
        if (virtualCamera == null)
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        _lastCamRot = CamRot();
    }

    void LateUpdate()
    {
        if (virtualCamera == null) return;

        Quaternion camRot = CamRot();

        transform.position = virtualCamera.transform.position
            + virtualCamera.transform.right * cameraOffset.x
            + virtualCamera.transform.up * cameraOffset.y
            + virtualCamera.transform.forward * cameraOffset.z;

        Quaternion delta = camRot * Quaternion.Inverse(_lastCamRot);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        float lookX = Vector3.Dot(axis, virtualCamera.transform.right) * angle;
        float lookY = Vector3.Dot(axis, virtualCamera.transform.up) * angle;
        _lastCamRot = camRot;

        Vector3 targetTilt = new Vector3(lookY * tiltAmount, 0f, -lookX * tiltAmount);
        _tiltOffset = Vector3.Lerp(_tiltOffset, targetTilt, tiltSmooth * Time.deltaTime);
        transform.rotation = camRot * Quaternion.Euler(_tiltOffset);

    }
    Quaternion CamRot() =>
        virtualCamera != null ? virtualCamera.transform.rotation : transform.rotation;

}